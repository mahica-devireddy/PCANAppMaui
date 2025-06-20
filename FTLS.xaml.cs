using LocalizationResourceManager.Maui;
using System.Globalization;
using PCANAppM.Resources.Languages;
using Peak.Can.Basic;
using System.Collections.ObjectModel;
using System.Diagnostics;

#if WINDOWS
using PCANAppM.Platforms.Windows;
#endif

namespace PCANAppM;

public partial class FTLS : ContentPage
{
    private string? _currentCanId1 = null;
    private string? _pendingNewCanId1 = null;
    private readonly ILocalizationResourceManager _localizationResourceManager;
#if WINDOWS
    private PCAN_USB? _pcanUsb;
    private ushort _currentHandle;
    private bool _isStarted = false;
#endif

    public FTLS(ILocalizationResourceManager localizationResourceManager)
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
        _currentCanId1 = lastTwo;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateLatestCanIdLabel1(lastTwo);
        });
    }

    private void OnPcanFeedback(string message)
    {
        // Optional: Display or log feedback
    }

    private async void OnSetCanIdClicked(object sender, EventArgs e)
    {
        SetCanIdView2.IsVisible = true;
        InitialFtlsView.IsVisible = false;
    }

    private async void OnSetClicked(object sender, EventArgs e)
    {
        var newCanId = NewCanIdEntry2.Text?.Trim();
        if (string.IsNullOrEmpty(newCanId) || newCanId.Length > 2 || !int.TryParse(newCanId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            await DisplayAlert("Invalid Input", "Please enter a valid 2-digit hex CAN ID.", "OK");
            return;
        }

        ConfirmText2.Text = $"Set The CAN ID to {newCanId.ToUpper()}";
        SetCanIdView2.IsVisible = false;
        ConfirmCanIdView2.IsVisible = true;
        _pendingNewCanId1 = newCanId.ToUpper();
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_pendingNewCanId1)) return;

        string currentId = _currentCanId1 ?? "00";
        string newId = _pendingNewCanId1.PadLeft(2, '0');

        // Send 8-byte and 1-byte messages back-to-back
        SendCanMessage($"0CEF{currentId}02", new byte[] { 0x0C, 0xEF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }); // 8 bytes
        SendCanMessage($"0CEF{currentId}02", new byte[] { Convert.ToByte(newId, 16) }); // 1 byte

        _currentCanId1 = newId;
        UpdateLatestCanIdLabel1(newId);
        ConfirmCanIdView2.IsVisible = false;
        InitialFtlsView.IsVisible = true;
    }

    // ✅ This wrapper auto-detects data length and sends the message
    private void SendCanMessage(string canIdHex, byte[] data)
    {
        if (_pcanUsb == null || !_isStarted)
            return;

        try
        {
            uint canId = uint.Parse(canIdHex, NumberStyles.HexNumber);
            int dataLen = data.Length;

            var status = _pcanUsb.WriteFrame(canId, dataLen, data);

            if (status != TPCANStatus.PCAN_ERROR_OK)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("Send Failed",
                        $"CAN message failed to send.\nStatus: {_pcanUsb!.PeakCANStatusErrorString(status)}", "OK");
                });
            }
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Application.Current.MainPage.DisplayAlert("Exception",
                    $"Error while sending CAN message: {ex.Message}", "OK");
            });
        }
    }

    private void OnCancelConfirmClicked1(object sender, EventArgs e)
    {
        ConfirmCanIdView2.IsVisible = false;
        InitialFtlsView.IsVisible = true;
    }

    private void OnExitClicked(object sender, EventArgs e)
    {
        SetCanIdView2.IsVisible = false;
        InitialFtlsView.IsVisible = true;
        ConfirmCanIdView2.IsVisible = false;
    }
#endif

    private void UpdateLatestCanIdLabel1(string id)
    {
        LatestCanIdLabel2.Text = $"Current CAN ID of the Fluid Tank Level Sensor: {id}";
    }

    private void NewCanIdEntry_Focused1(object sender, FocusEventArgs e)
    {
        // Placeholder for focus handling
    }

    private async void OnLanguageButtonClicked(object sender, EventArgs e)
    {
        LanguageState.CurrentLanguage = LanguageState.CurrentLanguage == "en" ? "es" : "en";
        _localizationResourceManager.CurrentCulture = new CultureInfo(LanguageState.CurrentLanguage);
    }
}

public class CanMessageViewModel2
{
    public string Direction { get; set; } = "";
    public string Id { get; set; } = "";
    public string Data { get; set; } = "";
}
