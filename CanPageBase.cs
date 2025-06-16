using System.Collections.ObjectModel;
using System.Globalization;
using Peak.Can.Basic;

#if WINDOWS
using PCANAppM.Platforms.Windows;
#endif

namespace PCANAppM;

public abstract partial class CanPageBase : ContentPage
{
    protected abstract Picker DevicePickerControl { get; }
    protected abstract Picker BaudRatePickerControl { get; }
    protected abstract Entry CanIdHexEntry { get; }
    protected abstract Button StartStopButtonControl { get; }
    protected abstract Button SendButtonControl { get; }
    protected abstract Entry CanIdEntryControl { get; }
    protected abstract Entry DataEntryControl { get; }
    protected abstract Entry LengthEntryControl { get; }
    protected abstract Entry NewCanIdEntryControl { get; }
    protected abstract Button ConfirmCanIdButtonControl { get; }
    protected abstract Label LatestCanIdLabelControl { get; }
    protected abstract CollectionView CanMessagesViewControl { get; }

    protected ObservableCollection<CanMessageViewModel> _canMessages = new();

#if WINDOWS
    private PCAN_USB? _pcanUsb;
    private ushort _currentHandle;
    private bool _isStarted = false;
    private bool _bChanging = false;
#endif

    private string? _pendingNewCanId = null;
    private string? _currentCanId = null; 

    public CanPageBase()
    {
        InitializeComponent();
        CanMessagesViewControl.ItemsSource = _canMessages;

#if WINDOWS
        // Populate device list
        var devices = PCAN_USB.GetUSBDevices();
        foreach (var dev in devices)
            DevicePickerControl.Items.Add(dev);

        // Populate baud rate list
        foreach (var rate in PCAN_USB.CANBaudRates)
            BaudRatePickerControl.Items.Add(rate);

        if (BaudRatePickerControl.Items.Contains("250 kbit/s"))
            BaudRatePickerControl.SelectedIndex = BaudRatePickerControl.Items.IndexOf("250 kbit/s");

        StartStopButtonControl.IsEnabled = DevicePickerControl.Items.Count > 0 && BaudRatePickerControl.SelectedIndex >= 0;
        SendButtonControl.IsEnabled = false;
#endif
    }

    protected abstract void InitializeComponent();

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
            if (DevicePickerControl.SelectedIndex < 0 || BaudRatePickerControl.SelectedIndex < 0)
            {
                _canMessages.Insert(0, new CanMessageViewModel { Direction = "", Id = "", Data = "Select device and baud rate." });
                return;
            }
            var handle = PCAN_USB.DecodePEAKHandle(DevicePickerControl.SelectedItem.ToString());
            var baud = BaudRatePickerControl.SelectedItem.ToString();

            _pcanUsb = new PCAN_USB();
            SubscribeToPcanUsbEvents();

            var status = _pcanUsb.InitializeCAN(handle, baud, true);
            if (status == TPCANStatus.PCAN_ERROR_OK)
            {
                _currentHandle = handle;
                _isStarted = true;
                StartStopButtonControl.Text = "Stop";
                SendButtonControl.IsEnabled = true;
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
            StartStopButtonControl.Text = "Start";
            SendButtonControl.IsEnabled = false;
            _canMessages.Insert(0, new CanMessageViewModel { Direction = "", Id = "", Data = "CAN stopped." });
        }
    }

    private void OnSendClicked(object sender, EventArgs e)
    {
        if (_pcanUsb == null || !_isStarted)
            return;

        // Parse CAN ID
        if (!uint.TryParse(CanIdEntryControl.Text, out var canId))
        {
            _canMessages.Insert(0, new CanMessageViewModel { Direction = "", Id = "", Data = "Invalid CAN ID." });
            return;
        }

        // Parse data
        var dataParts = DataEntryControl.Text?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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
        if (!int.TryParse(LengthEntryControl.Text, out var dataLen) || dataLen < 0 || dataLen > 8)
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
        NewCanIdEntryControl.Text = string.Empty;
        NewCanIdEntryControl.IsVisible = true;
        ConfirmCanIdButtonControl.IsVisible = true;
        await DisplayAlert("Update CAN ID", "Enter the new CAN ID (last two hex digits) and press Confirm.", "OK");
    }
#if WINDOWS
    protected void SendCanMessage(string canIdHex, byte[] data, int dataLen)
    {

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

    }
#endif
    private void UpdateLatestCanIdLabel(string id)
    {
        if (LatestCanIdLabelControl != null)
            LatestCanIdLabelControl.Text = $"Latest CAN ID: {id}";
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
        if (uint.TryParse(CanIdEntryControl.Text, out var dec))
            CanIdHexEntry.Text = dec.ToString("X");
        else
            CanIdHexEntry.Text = string.Empty;
        _bChanging = false;
    }

    private void OnCanIdHexEntryChanged(object sender, TextChangedEventArgs e)
    {
        if (_bChanging) return;
        _bChanging = true;
        if (uint.TryParse(CanIdHexEntry.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
            CanIdEntryControl.Text = hex.ToString();
        else
            CanIdEntryControl.Text = string.Empty;
        _bChanging = false;
    }

    protected void SendCanIdChange(uint canId, byte[] data, int dataLen)
    {
#if WINDOWS
        if (_pcanUsb == null || !_isStarted)
            return;

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

    public virtual void SendCanMessageId(string currentId, string newId)
    {
        // Default implementation does nothing or throws
        throw new NotImplementedException("SendCanMessageId must be overridden in derived class.");
    }
#endif
}
public class CanMessageViewModel
{
    public string Direction { get; set; } = ""; // "Rx" or "Tx"
    public string Id { get; set; } = "";
    public string Data { get; set; } = "";
}
