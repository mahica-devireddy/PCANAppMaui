using LocalizationResourceManager.Maui;
using System.Globalization;
using PCANAppM.Resources.Languages;
using Peak.Can.Basic;
using System.Collections.ObjectModel;
using System.Globalization;
using PCANAppM;

#if WINDOWS
using PCANAppM.Platforms.Windows;
#endif

namespace PCANAppM;

public partial class FTLS : ContentPage
{
    private string? _currentCanId = null;
    private string? _pendingNewCanId1 = null;
    private readonly ILocalizationResourceManager _localizationResourceManager;
#if WINDOWS
    private PCAN_USB? _pcanUsb;
    private ushort _currentHandle;
    private bool _isStarted = false;
    private bool _sideMenuFirstOpen = true;
    private bool _isFTLSConnected = false;
    private System.Timers.Timer? _connectionTimeoutTimer;
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
        uint canId = packet.Id;
        uint pgn = (canId >> 8) & 0xFFFF;

        if (pgn == 0xFF52)
        {
            FTLSConnectionState.IsConnected = true;
            ResetConnectionTimeout();
        }

        var idHex = $"0x{packet.Id:X}";
        string lastTwo = idHex.Length >= 2 ? idHex.Substring(idHex.Length - 2) : idHex;
        if (int.TryParse(lastTwo, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int canIdInt))
        {
            _currentCanId = canIdInt.ToString();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateLatestCanIdLabel1(_currentCanId);
            });
        }
    }

    private void ResetConnectionTimeout()
    {
#if WINDOWS
        _connectionTimeoutTimer?.Stop();
        _connectionTimeoutTimer = new System.Timers.Timer(2000); // 2 seconds
        _connectionTimeoutTimer.Elapsed += (s, e) =>
        {
            FTLSConnectionState.IsConnected = false;
            _connectionTimeoutTimer?.Stop();
        };
        _connectionTimeoutTimer.AutoReset = false;
        _connectionTimeoutTimer.Start();
#endif
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
        var newCanIdText = NewCanIdEntry2.Text?.Trim();
        if (string.IsNullOrEmpty(newCanIdText) || !int.TryParse(newCanIdText, out int newCanIdInt) || newCanIdInt < 0 || newCanIdInt > 255)
        {
            await DisplayAlert("Invalid Input", "Please enter a valid CAN ID (0-255).", "OK");
            return;
        }

        ConfirmText2.Text = $"Set The CAN ID to {newCanIdInt}";
        SetCanIdView2.IsVisible = false;
        ConfirmCanIdView2.IsVisible = true;
        _pendingNewCanId1 = newCanIdInt.ToString();

        NewCanIdEntry2.Text = string.Empty;
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
#if WINDOWS
        if (string.IsNullOrEmpty(_pendingNewCanId1)) return;

        // Use the current CAN ID as integer, fallback to 0
        int currentIdInt = 0;
        int.TryParse(_currentCanId, out currentIdInt);

        int newIdInt = int.Parse(_pendingNewCanId1);

        // Convert to hex string for protocol
        string currentIdHex = currentIdInt.ToString("X2");
        string newIdHex = newIdInt.ToString("X2");

        // Change FTLS CAN ID using the new protocol
        await ChangeFtlsCanIdAsync(currentIdHex, newIdHex);

        ConfirmCanIdView2.IsVisible = false;
        InitialFtlsView.IsVisible = true;
#endif
    }

#if WINDOWS
    private async Task ChangeFtlsCanIdAsync(string currentId, string newId)
    {
        if (_pcanUsb == null || !_isStarted)
            return;

        // Compose CAN ID
        string canIdHex = $"0CEF{currentId}02";
        uint canId = uint.Parse(canIdHex, NumberStyles.HexNumber);

        // First message: data = [0x72, 0x6F, 0x74, 0x61, 0x2D, 0x65, 0x6E, 0x6A]
        byte[] firstData = new byte[] { 0x72, 0x6F, 0x74, 0x61, 0x2D, 0x65, 0x6E, 0x6A };
        _pcanUsb.WriteFrame(canId, 8, firstData, canId > 0x7FF);

        // Wait a short time to ensure the message is sent (optional, but recommended)
        await Task.Delay(100);

        // Second message: data = [newId] (data length = 1)
        byte[] secondData = new byte[] { Convert.ToByte(newId, 16) };
        _pcanUsb.WriteFrame(canId, 1, secondData, canId > 0x7FF);
    }
#endif

    // This wrapper auto-detects data length and sends the message
    private void SendCanMessage(string canIdHex, byte[] data, int dataLen)
    {
        if (_pcanUsb == null || !_isStarted)
            return;

        uint canId = uint.Parse(canIdHex, NumberStyles.HexNumber);

        var buffer = new byte[8];
        Array.Copy(data, buffer, Math.Min(data.Length, 8));

        _pcanUsb.WriteFrame(
            canId,
            8,
            buffer,
            canId > 0x7FF
        );
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
        LatestCanIdLabel2.Text = _localizationResourceManager["CurrentFTLS"] + " " + id;
    }
#if WINDOWS
    private async void OnFTLSStatusClicked(object sender, EventArgs e)
    {
        await ShowFTLSConnectionStatusAsync();
    }

    private async void OnFTLSButtonClicked(object sender, EventArgs e)
    {
        // Determine connection state (replace with your actual check)
        bool isFTLSConnected = _isFTLSConnected;
        await Navigation.PushAsync(new FTLSConnectionStatusPage(isFTLSConnected));
    }

    private async Task ShowFTLSConnectionStatusAsync()
    {
        string message = _isFTLSConnected
            ? "Fluid Tank Level Sensor is CONNECTED."
            : "Fluid Tank Level Sensor is NOT CONNECTED.";
        await DisplayAlert("Fluid Tank Level Sensor Connection", message, "OK");
    }
#endif

    private async void OnCheckConnectionClicked(object sender, EventArgs e)
    {
#if WINDOWS
        bool isConnected = FTLSConnectionState.IsConnected;
        string message = isConnected
            ? "Fluid Tank Level Sensor is CONNECTED."
            : "Fluid Tank Level Sensor is NOT CONNECTED.";
        await DisplayAlert("Connection Status", message, "OK");
#endif
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
    private void OnOshkoshLogoClicked(object sender, EventArgs e)
    {
        SideMenu.IsVisible = true;
        SideMenuDim.IsVisible = true;

        if (SideMenu.Width == 0)
        {
            // Wait for the menu to be measured, then animate
            SideMenu.SizeChanged += SideMenu_SizeChangedAnimateIn;
        }
        else
        {
            AnimateSideMenuIn();
        }
    }

    private async void SideMenu_SizeChangedAnimateIn(object? sender, EventArgs e)
    {
        if (SideMenu.Width > 0)
        {
            SideMenu.SizeChanged -= SideMenu_SizeChangedAnimateIn;
            await AnimateSideMenuIn();
        }
    }

    private async Task AnimateSideMenuIn()
    {
        SideMenu.TranslationX = -SideMenu.Width;
        await SideMenu.TranslateTo(0, 0, 250, Easing.SinOut);
    }

#if WINDOWS
    private async void SideMenuOnFirstSizeChanged(object? sender, EventArgs e)
    {
        SideMenu.SizeChanged -= SideMenuOnFirstSizeChanged;
        _sideMenuFirstOpen = false;
        SideMenu.TranslationX = -SideMenu.Width;
        await SideMenu.TranslateTo(0, 0, 250, Easing.SinOut);
    }
#endif

    private async void OnCloseSideMenuClicked(object sender, EventArgs e)
    {
        await SideMenu.TranslateTo(-SideMenu.Width, 0, 250, Easing.SinIn); // Slide out
        SideMenu.IsVisible = false;
        SideMenuDim.IsVisible = false;
    }

    private async void OnMenuClicked(object sender, EventArgs e)
    {
        await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
        SideMenu.IsVisible = false;
        SideMenuDim.IsVisible = false;
        await Navigation.PushAsync(new Menu(_localizationResourceManager));
    }

    private async void OnAngleSensorMenuClicked(object sender, EventArgs e)
    {
        await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
        SideMenu.IsVisible = false;
        SideMenuDim.IsVisible = false;
        await Navigation.PushAsync(new BAS(_localizationResourceManager));
    }

    private async void OnKzValveMenuClicked(object sender, EventArgs e)
    {
        await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
        SideMenu.IsVisible = false;
        SideMenuDim.IsVisible = false;
        await Navigation.PushAsync(new KZV(_localizationResourceManager));
    }

    private async void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
    {
        await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
        SideMenu.IsVisible = false;
        SideMenuDim.IsVisible = false;
        await Navigation.PushAsync(new FTLS(_localizationResourceManager));
    }
}

public class CanMessageViewModel2
{
    public string Direction { get; set; } = "";
    public string Id { get; set; } = "";
    public string Data { get; set; } = "";
}
