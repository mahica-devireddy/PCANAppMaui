using LocalizationResourceManager.Maui;
using Peak.Can.Basic;
using System.Collections.ObjectModel;
using System.Globalization;
using PCANAppM;


#if WINDOWS
using PCANAppM.Platforms.Windows;
#endif

namespace PCANAppM;

public partial class KZV : ContentPage
{

    private string? _currentCanId = null;
    private string? _pendingNewCanId = null;
    private readonly ILocalizationResourceManager _localizationResourceManager;
#if WINDOWS
    private PCAN_USB? _pcanUsb;
    private ushort _currentHandle;
    private bool _isStarted = false;
    private bool _sideMenuFirstOpen = true;
    private bool _isKZVConnected = false; // Add this field to resolve the error
    private System.Timers.Timer? _connectionTimeoutTimer;
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
        uint canId = packet.Id;
        uint pgn = (canId >> 8) & 0xFFFF;

        if (pgn == 0xFECA)
        {
            KZVConnectionState.IsConnected = true;
            ResetConnectionTimeout();
        }
        var idHex = $"0x{packet.Id:X}";
        string lastTwo = idHex.Length >= 2 ? idHex.Substring(idHex.Length - 2) : idHex;

        // Convert lastTwo from hex to decimal for display
        if (int.TryParse(lastTwo, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int canIdInt))
        {
            _currentCanId = canIdInt.ToString();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateLatestCanIdLabel(_currentCanId);
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
            KZVConnectionState.IsConnected = false;
            _connectionTimeoutTimer?.Stop();
        };
        _connectionTimeoutTimer.AutoReset = false;
        _connectionTimeoutTimer.Start();
#endif
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
        if (string.IsNullOrEmpty(newCanId) || !int.TryParse(newCanId, out int newCanIdInt) || newCanIdInt < 0 || newCanIdInt > 255)
        {
            await DisplayAlert("Invalid Input", "Please enter a valid CAN ID value between 0-255.", "OK");
            return;
        }

        ConfirmText.Text = $"Set The CAN ID to {newCanIdInt}";
        SetCanIdView.IsVisible = false;
        ConfirmCanIdView.IsVisible = true;
        _pendingNewCanId = newCanIdInt.ToString();

        NewCanIdEntry.Text = string.Empty;
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_pendingNewCanId)) return;

        // Use the current CAN ID as integer, fallback to 0
        int currentIdInt = 0;
        int.TryParse(_currentCanId, out currentIdInt);

        int newIdInt = int.Parse(_pendingNewCanId);

        byte currentIdByte = (byte)currentIdInt;
        byte newIdByte = (byte)newIdInt;

        uint canId = (0x18EF0000u) | ((uint)currentIdByte << 8) | 0x01u;

        byte[] data = new byte[8];
        data[3] = 0x04;
        data[4] = newIdByte;

        var status = _pcanUsb?.WriteFrame(canId, 8, data, canId > 0x7FF);

        //_currentCanId = newId;
        //UpdateLatestCanIdLabel(newId);
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
        LatestCanIdLabel.Text = _localizationResourceManager["CurrentKZV"] + " " + id;
    }

#if WINDOWS
    private async void OnKZVClicked(object sender, EventArgs e)
    {
        // Determine connection state (replace with your actual check)
        bool isKZVConnected = _isKZVConnected;
        await Navigation.PushAsync(new KZVConnectionStatusPage(isKZVConnected));
    }

    private async Task ShowKZVConnectionStatusAsync()
    {
        string message = _isKZVConnected
            ? "KZ Valve is CONNECTED."
            : "KZ Valve is NOT CONNECTED.";
        await DisplayAlert("KZ Valve Connection", message, "OK");
    }
#endif

    private async void OnCheckConnectionClicked(object sender, EventArgs e)
    {
#if WINDOWS
        bool isConnected = KZVConnectionState.IsConnected;
        string message = isConnected
            ? "KZ Valve is CONNECTED."
            : "KZ Valve is NOT CONNECTED.";
        await DisplayAlert("Connection Status", message, "OK");
#endif
    }

    private void NewCanIdEntry_Focused(object sender, FocusEventArgs e)
    {

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

public class CanMessageViewModel
{
    public string Direction { get; set; } = "";
    public string Id { get; set; } = "";
    public string Data { get; set; } = "";
}
