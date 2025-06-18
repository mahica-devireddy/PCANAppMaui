using System.Collections.ObjectModel;
using System.Globalization;
using Peak.Can.Basic;

#if WINDOWS
using PCANAppM.Platforms.Windows;
#endif

namespace PCANAppM;

public partial class BAS : ContentPage
{
    private string? _currentCanId1 = null;
    private string? _pendingNewCanId1 = null;
#if WINDOWS
    private PCAN_USB? _pcanUsb;
    private ushort _currentHandle;
    private bool _isStarted = false;
#endif

    public BAS()
    {
        InitializeComponent();
#if WINDOWS
        var devices = PCAN_USB.GetUSBDevices();
        if (devices.Count == 0)
        {
            DisplayAlert("Error", "No PCAN devices found.", "OK");
            return;
        }

        string selectedDevice = devices[0];
        string baudRate = "250 kbit/s";

        _pcanUsb = new PCAN_USB();
        SubscribeToPcanUsbEvents();

        var handle = PCAN_USB.DecodePEAKHandle(selectedDevice);
        var status = _pcanUsb.InitializeCAN(handle, baudRate, true);
        if (status == TPCANStatus.PCAN_ERROR_OK)
        {
            _currentHandle = handle;
            _isStarted = true;
        }
        else
        {
            DisplayAlert("Init Failed", _pcanUsb.PeakCANStatusErrorString(status), "OK");
        }
#endif
    }

#if WINDOWS
    private void SubscribeToPcanUsbEvents()
    {
        if (_pcanUsb == null) return;
        _pcanUsb.MessageReceived += OnCanMessageReceived;
        _pcanUsb.Feedback += OnPcanFeedback;
    }

    private void OnCanMessageReceived(PCAN_USB.Packet packet)
    {
        var idHex = $"0x{packet.Id:X}";
        string lastTwo = idHex.Length >= 2 ? idHex.Substring(idHex.Length - 2) : idHex;
        _currentCanId1 = lastTwo;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateLatestCanIdLabel1(lastTwo);
        });
    }

    private void OnPcanFeedback(string message)
    {
        // Not shown in UI
    }

    private async void OnSetCanIdClicked(object sender, EventArgs e)
    {
        SetCanIdView1.IsVisible = true;
        InitialBasView.IsVisible = false;
    }

    private async void OnSetClicked(object sender, EventArgs e)
    {
        var newCanId = NewCanIdEntry1.Text?.Trim();
        if (string.IsNullOrEmpty(newCanId) || newCanId.Length > 2 || !int.TryParse(newCanId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            await DisplayAlert("Invalid Input", "Please enter a valid 2-digit hex CAN ID.", "OK");
            return;
        }

        ConfirmText1.Text = $"Set The CAN ID to {newCanId.ToUpper()}";
        SetCanIdView1.IsVisible = false;
        ConfirmCanIdView1.IsVisible = true;
        _pendingNewCanId1 = newCanId.ToUpper();
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_pendingNewCanId1)) return;

        string currentId = _currentCanId1 ?? "00";
        string newId = _pendingNewCanId1.PadLeft(2, '0');

        byte currentIdByte = byte.Parse(currentId, NumberStyles.HexNumber);
        byte newIdByte = byte.Parse(newId, NumberStyles.HexNumber);

        uint canId = (0x18EF0000u) | ((uint)currentIdByte << 8) | 0x01u;

        byte[] data = new byte[8];
        data[3] = 0x04;
        data[4] = newIdByte;

        var status = _pcanUsb?.WriteFrame(canId, 8, data, canId > 0x7FF);

        _currentCanId1 = newId;
        UpdateLatestCanIdLabel1(newId);
        ConfirmCanIdView1.IsVisible = false;
        InitialBasView.IsVisible = true;
    }

    private void OnCancelConfirmClicked1(object sender, EventArgs e)
    {
        ConfirmCanIdView1.IsVisible = false;
        InitialBasView.IsVisible = true;
    }

    private void OnExitClicked(object sender, EventArgs e)
    {
        SetCanIdView1.IsVisible = false;
        InitialBasView.IsVisible = true;
        ConfirmCanIdView1.IsVisible = false;
    }
#endif

    private void UpdateLatestCanIdLabel1(string id)
    {
        LatestCanIdLabel1.Text = $"Current CAN ID of the Angle Sensor: {id}";
    }

    private void NewCanIdEntry_Focused1(object sender, FocusEventArgs e)
    {

    }
}

public class CanMessageViewModel1
{
    public string Direction { get; set; } = "";
    public string Id { get; set; } = "";
    public string Data { get; set; } = "";
}
