using System.Collections.ObjectModel;
using System.Globalization;
using Peak.Can.Basic;

#if WINDOWS
using PCANAppM.Platforms.Windows;
#endif

namespace PCANAppM;

public partial class KZV : ContentPage
{
    private ObservableCollection<CanMessageViewModel> _canMessages = new();
#if WINDOWS
    private PCAN_USB? _pcanUsb;
    private ushort _currentHandle;
    private bool _isStarted = false;
    private bool _bChanging = false;
#endif

    private string? _pendingNewCanId = null;
    private string? _currentCanId = null;
    public KZV()
    {
        InitializeComponent();
        CanMessagesView.ItemsSource = _canMessages;

#if WINDOWS
        // Populate device list
        var devices = PCAN_USB.GetUSBDevices();
        foreach (var dev in devices)
            DevicePicker1.Items.Add(dev);

        // Populate baud rate list
        foreach (var rate in PCAN_USB.CANBaudRates)
            BaudRatePicker1.Items.Add(rate);

        if (BaudRatePicker1.Items.Contains("250 kbit/s"))
            BaudRatePicker1.SelectedIndex = BaudRatePicker1.Items.IndexOf("250 kbit/s");

        StartStopButton1.IsEnabled = DevicePicker1.Items.Count > 0 && BaudRatePicker1.SelectedIndex >= 0;
        SendButton1.IsEnabled = false;
#endif
    }

#if WINDOWS
    private void SubscribeToPcanUsbEvents()
    {
        if (_pcanUsb == null)
            return;

        // Subscribe to CAN message reception
        _pcanUsb.MessageReceived += OnCanMessageReceived;

        // Subscribe to feedback (status/error) messages
        _pcanUsb.Feedback += OnPcanFeedback;
    }

    private void OnStartStopClicked(object sender, EventArgs e)
    {
        if (!_isStarted)
        {
            if (DevicePicker1.SelectedIndex < 0 || BaudRatePicker1.SelectedIndex < 0)
            {
                _canMessages.Insert(0, new CanMessageViewModel { Direction = "", Id = "", Data = "Select device and baud rate." });
                return;
            }
            var handle = PCAN_USB.DecodePEAKHandle(DevicePicker1.SelectedItem.ToString());
            var baud = BaudRatePicker1.SelectedItem.ToString();

            _pcanUsb = new PCAN_USB();
            SubscribeToPcanUsbEvents();

            var status = _pcanUsb.InitializeCAN(handle, baud, true);
            if (status == TPCANStatus.PCAN_ERROR_OK)
            {
                _currentHandle = handle;
                _isStarted = true;
                StartStopButton1.Text = "Stop";
                SendButton1.IsEnabled = true;
                _canMessages.Insert(0, new CanMessageViewModel { Direction = "", Id = "", Data = "CAN started." });
            }
            else
            {
                _canMessages.Insert(0, new CanMessageViewModel { Direction = "", Id = "", Data = $"Init failed: {_pcanUsb.PeakCANStatusErrorString(status)}" });
            }
        }
        else
        {
            _pcanUsb?.Uninitialize();
            _isStarted = false;
            StartStopButton1.Text = "Start";
            SendButton1.IsEnabled = false;
            _canMessages.Insert(0, new CanMessageViewModel { Direction = "", Id = "", Data = "CAN stopped." });
        }
    }

    private void OnSendClicked(object sender, EventArgs e)
    {
        if (_pcanUsb == null || !_isStarted)
            return;

        // Parse CAN ID
        if (!uint.TryParse(CanIdEntry1.Text, out var canId))
        {
            _canMessages.Insert(0, new CanMessageViewModel { Direction = "", Id = "", Data = "Invalid CAN ID." });
            return;
        }

        // Parse data
        var dataParts = DataEntry1.Text?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (dataParts == null)
        {
            _canMessages.Insert(0, new CanMessageViewModel { Direction = "", Id = "", Data = "No data." });
            return;
        }
        var data = new byte[8];
        int len = 0;
        foreach (var part in dataParts)
        {
            if (len >= 8) break;
            if (byte.TryParse(part, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
                data[len++] = b;
            else
            {
                _canMessages.Insert(0, new CanMessageViewModel { Direction = "", Id = "", Data = $"Invalid data byte: {part}" });
                return;
            }
        }
        if (!int.TryParse(LengthEntry.Text, out var dataLen) || dataLen < 0 || dataLen > 8)
            dataLen = len;

        var status = _pcanUsb.WriteFrame(canId, dataLen, data, canId > 0x7FF); // Extended if > 11 bits
        if (status == TPCANStatus.PCAN_ERROR_OK)
        {
            _canMessages.Insert(0, new CanMessageViewModel
            {
                Direction = "Tx",
                Id = $"0x{canId:X}",
                Data = $"Len={dataLen}"
            });
            _canMessages.Insert(0, new CanMessageViewModel
            {
                Direction = "Tx",
                Id = $"0x{canId:X}",
                Data = string.Join(" ", data.Take(dataLen).Select(b => b.ToString()))
            });
        }
        else
        {
            _canMessages.Insert(0, new CanMessageViewModel { Direction = "", Id = "", Data = $"Send failed: {_pcanUsb.PeakCANStatusErrorString(status)}" });
        }
    }

    private async void OnUpdateCanIdButtonClicked(object sender, EventArgs e)
    {
        NewCanIdEntry1.Text = string.Empty;
        NewCanIdEntry1.IsVisible = true;
        ConfirmCanIdButton1.IsVisible = true;
        await DisplayAlert("Update CAN ID", "Enter the new CAN ID (last two hex digits) and press Confirm.", "OK");
    }

    private async void OnConfirmCanIdButtonClicked(object sender, EventArgs e)
    {
        var newCanId = NewCanIdEntry1.Text?.Trim();
        if (string.IsNullOrEmpty(newCanId) || newCanId.Length > 2 || !int.TryParse(newCanId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            await DisplayAlert("Invalid Input", "Please enter a valid 2-digit hex CAN ID.", "OK");
            return;
        }

        bool sure = await DisplayAlert("Confirm", $"Are you sure you want to change the CAN ID to {newCanId.ToUpper()}?", "Yes", "No");
        if (!sure)
            return;

        // Hide entry and button
        NewCanIdEntry1.IsVisible = false;
        ConfirmCanIdButton1.IsVisible = false;

        // Use the last two digits of the current CAN ID (if not set, default to "00")
        string currentId = _currentCanId ?? "00";
        string newId = newCanId.ToUpper().PadLeft(2, '0');

#if WINDOWS
        // Parse IDs
        byte currentIdByte = byte.Parse(currentId, NumberStyles.HexNumber);
        byte newIdByte = byte.Parse(newId, NumberStyles.HexNumber);

        // Build CAN ID: 0x18EFxx01
        uint canId = (0x18EF0000u) | ((uint)currentIdByte << 8) | 0x01u;

        // Data: 00 00 00 04 yy 00 00 00
        byte[] data = new byte[8];
        data[0] = 0x00;
        data[1] = 0x00;
        data[2] = 0x00;
        data[3] = 0x04;
        data[4] = newIdByte;
        data[5] = 0x00;
        data[6] = 0x00;
        data[7] = 0x00;

        var status = _pcanUsb?.WriteFrame(canId, 8, data, canId > 0x7FF);

        _canMessages.Insert(0, new CanMessageViewModel
        {
            Direction = "Tx",
            Id = $"0x{canId:X}",
            Data = string.Join(" ", data.Select(b => b.ToString("X2")))
        });
#endif

        // Update the current CAN ID
        _currentCanId = newId;
        UpdateLatestCanIdLabel(newId);
    }

    private void SendCanMessage(string canIdHex, byte[] data, int dataLen)
    {
#if WINDOWS
        if (_pcanUsb == null || !_isStarted)
            return;

        uint canId = uint.Parse(canIdHex, NumberStyles.HexNumber);
        // Ensure data is at least dataLen bytes (pad with zeros if needed)
        byte[] paddedData = data.Length < dataLen
            ? data.Concat(Enumerable.Repeat((byte)0x00, dataLen - data.Length)).ToArray()
            : data;

        var status = _pcanUsb.WriteFrame(canId, dataLen, paddedData, canId > 0x7FF);

        _canMessages.Insert(0, new CanMessageViewModel
        {
            Direction = "Tx",
            Id = $"0x{canId:X}",
            Data = string.Join(" ", paddedData.Take(dataLen).Select(b => b.ToString("X2")))
        });
#endif
    }

    private void UpdateLatestCanIdLabel(string id)
    {
        if (LatestCanIdLabel1 != null)
            LatestCanIdLabel1.Text = $"Latest CAN ID: {id}";
    }

    private void OnCanMessageReceived(PCAN_USB.Packet packet)
    {
        var idHex = $"0x{packet.Id:X}";
        string lastTwo = idHex.Length >= 2 ? idHex.Substring(idHex.Length - 2) : idHex;
        _currentCanId = lastTwo;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _canMessages.Insert(0, new CanMessageViewModel
            {
                Direction = "Rx",
                Id = idHex,
                Data = string.Join(" ", packet.Data.Take(packet.Length).Select(b => b.ToString("X2")))
            });
            UpdateLatestCanIdLabel(lastTwo);
            if (_canMessages.Count > 100)
                _canMessages.RemoveAt(_canMessages.Count - 1);
        });
    }

    private void OnPcanFeedback(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _canMessages.Insert(0, new CanMessageViewModel
            {
                Direction = "",
                Id = "",
                Data = message
            });
            if (_canMessages.Count > 100)
                _canMessages.RemoveAt(_canMessages.Count - 1);
        });
    }

    private void OnCanIdEntryChanged(object sender, TextChangedEventArgs e)
    {
        if (_bChanging) return;
        _bChanging = true;
        if (uint.TryParse(CanIdEntry1.Text, out var dec))
            CanIdHexEntry1.Text = dec.ToString("X");
        else
            CanIdHexEntry1.Text = string.Empty;
        _bChanging = false;
    }

    private void OnCanIdHexEntryChanged(object sender, TextChangedEventArgs e)
    {
        if (_bChanging) return;
        _bChanging = true;
        if (uint.TryParse(CanIdHexEntry1.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
            CanIdEntry1.Text = hex.ToString();
        else
            CanIdEntry1.Text = string.Empty;
        _bChanging = false;
    }

    private void ChangeID(string currentIdHex, string newIdHex)
    {
#if WINDOWS
        if (_pcanUsb == null || !_isStarted)
            return;

        // Parse IDs
        byte currentId = byte.Parse(currentIdHex, NumberStyles.HexNumber);
        byte newId = byte.Parse(newIdHex, NumberStyles.HexNumber);

        // Build CAN ID: 0x18EFxx01
        uint canId = (0x18EF0000u) | ((uint)currentId << 8) | 0x01u;

        // Data: 00 00 00 04 yy 00 00 00
        byte[] data = new byte[8];
        data[0] = 0x00;
        data[1] = 0x00;
        data[2] = 0x00;
        data[3] = 0x04;
        data[4] = newId;
        data[5] = 0x00;
        data[6] = 0x00;
        data[7] = 0x00;

        int dataLen = 8;

        var status = _pcanUsb.WriteFrame(canId, dataLen, data, canId > 0x7FF);
        if (status == TPCANStatus.PCAN_ERROR_OK)
        {
            _canMessages.Insert(0, new CanMessageViewModel
            {
                Direction = "Tx",
                Id = $"0x{canId:X}",
                Data = string.Join(" ", data.Select(b => b.ToString("X2")))
            });
        }
        else
        {
            _canMessages.Insert(0, new CanMessageViewModel
            {
                Direction = "",
                Id = "",
                Data = $"Send failed: {_pcanUsb.PeakCANStatusErrorString(status)}"
            });
        }
#endif
    }
#endif
}

public class CanMessageViewModel1
{
    public string Direction { get; set; } = ""; // "Rx" or "Tx"
    public string Id { get; set; } = "";
    public string Data { get; set; } = "";
}
