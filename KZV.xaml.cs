using LocalizationResourceManager.Maui;
using Peak.Can.Basic;
using System.Collections.ObjectModel;
using System.Globalization;

#if WINDOWS
using PCANAppM.Platforms.Windows;
#endif

namespace PCANAppM;

public partial class KZV : ContentPage
{
    private readonly ILocalizationResourceManager _localizationResourceManager;
    private string? _currentCanId = null;
    private string? _pendingNewCanId = null;
#if WINDOWS
    private PCAN_USB? _pcanUsb;
    private ushort _currentHandle;
    private bool _isStarted = false;
#endif

    public KZV(ILocalizationResourceManager localizationResourceManager)
    {
        _localizationResourceManager = localizationResourceManager;
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
        _currentCanId = lastTwo;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateLatestCanIdLabel(lastTwo);
        });
    }

    private void OnPcanFeedback(string message)
    {
        // Not shown in UI
    }

    private async void OnSetCanIdClicked(object sender, EventArgs e)
    {
        SetCanIdView.IsVisible = true;
        InitialKzvView.IsVisible = false;
    }

    private async void OnSetClicked(object sender, EventArgs e)
    {
        var newCanId = NewCanIdEntry.Text?.Trim();
        if (string.IsNullOrEmpty(newCanId) || newCanId.Length > 2 || !int.TryParse(newCanId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            await DisplayAlert("Invalid Input", "Please enter a valid 2-digit hex CAN ID.", "OK");
            return;
        }

        ConfirmText.Text = $"Set The CAN ID to {newCanId.ToUpper()}";
        SetCanIdView.IsVisible = false;
        ConfirmCanIdView.IsVisible = true;
        _pendingNewCanId = newCanId.ToUpper();
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_pendingNewCanId)) return;

        string currentId = _currentCanId ?? "00";
        string newId = _pendingNewCanId.PadLeft(2, '0');

        byte currentIdByte = byte.Parse(currentId, NumberStyles.HexNumber);
        byte newIdByte = byte.Parse(newId, NumberStyles.HexNumber);

        uint canId = (0x18EF0000u) | ((uint)currentIdByte << 8) | 0x01u;

        byte[] data = new byte[8];
        data[3] = 0x04;
        data[4] = newIdByte;

        var status = _pcanUsb?.WriteFrame(canId, 8, data, canId > 0x7FF);

        _currentCanId = newId;
        UpdateLatestCanIdLabel(newId);
        ConfirmCanIdView.IsVisible = false;
        InitialKzvView.IsVisible = true;
    }

    private void OnCancelConfirmClicked(object sender, EventArgs e)
    {
        ConfirmCanIdView.IsVisible = false;
        InitialKzvView.IsVisible = true;
    }

    private void OnExitClicked(object sender, EventArgs e)
    {
        SetCanIdView.IsVisible = false;
        InitialKzvView.IsVisible = true;
        ConfirmCanIdView.IsVisible = false;
    }
#endif

    private void UpdateLatestCanIdLabel(string id)
    {
        LatestCanIdLabel.Text = _localizationResourceManager["CurrentFTLS"] + " " + id;
    }

    private void NewCanIdEntry_Focused(object sender, FocusEventArgs e)
    {

    }

    private async void OnLanguageButtonClicked(object sender, EventArgs e)
    {
        LanguageState.CurrentLanguage = LanguageState.CurrentLanguage == "en" ? "es" : "en";
        _localizationResourceManager.CurrentCulture = new CultureInfo(LanguageState.CurrentLanguage);
    }
}

public class CanMessageViewModel
{
    public string Direction { get; set; } = "";
    public string Id { get; set; } = "";
    public string Data { get; set; } = "";
}
