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

public partial class BAS : ContentPage
{
    private string? _currentCanId1 = null;
    private string? _pendingNewCanId1 = null;
    private readonly ILocalizationResourceManager _localizationResourceManager;
#if WINDOWS
    private PCAN_USB? _pcanUsb;
    private ushort _currentHandle;
    private bool _isStarted = false;
    private bool _sideMenuFirstOpen = true;
    private bool _isAngleSensorConnected = false;
    private System.Timers.Timer? _connectionTimeoutTimer;
#endif

    public BAS(ILocalizationResourceManager localizationResourceManager)
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

        if (pgn == 0xFFBB)
        {
            ASConnectionState.IsConnected = true;
            ResetConnectionTimeout();
        }

        // Existing CAN ID display logic
        var idHex = $"0x{packet.Id:X}";
        string lastTwo = idHex.Length >= 2 ? idHex.Substring(idHex.Length - 2) : idHex;
        if (int.TryParse(lastTwo, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int canIdInt))
        {
            _currentCanId1 = canIdInt.ToString();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateLatestCanIdLabel1(_currentCanId1);
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
            ASConnectionState.IsConnected = false;
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
        SetCanIdView1.IsVisible = true;
        InitialBasView.IsVisible = false;
    }

    private async void OnSetClicked(object sender, EventArgs e)
    {
        var newCanId = NewCanIdEntry1.Text?.Trim();
        if (string.IsNullOrEmpty(newCanId) || !int.TryParse(newCanId, out int newCanIdInt) || newCanIdInt < 0 || newCanIdInt > 255)
        {
            await DisplayAlert("Invalid Input", "Please enter a valid CAN ID value between 0-255.", "OK");
            return;
        }

        ConfirmText1.Text = $"Set The CAN ID to {newCanIdInt}";
        SetCanIdView1.IsVisible = false;
        ConfirmCanIdView1.IsVisible = true;
        _pendingNewCanId1 = newCanIdInt.ToString();

        NewCanIdEntry1.Text = string.Empty;
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_pendingNewCanId1)) return;

        // Use the current CAN ID as integer, fallback to 0
        int currentIdInt = 0;
        int.TryParse(_currentCanId1, out currentIdInt);

        int newIdInt = int.Parse(_pendingNewCanId1);

        // Convert to hex string for protocol
        string currentIdHex = currentIdInt.ToString("X2");
        string newIdHex = newIdInt.ToString("X2");

        // Send CAN messages according to protocol
        SendCanMessage($"18EA{currentIdHex}00", new byte[] { 0x00, 0xEF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 8);
        SendCanMessage($"18EF{currentIdHex}00", new byte[] { 0x06, Convert.ToByte(newIdHex, 16), 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, 8);
        SendCanMessage($"18EA{currentIdHex}00", new byte[] { 0x00, 0xEF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 8);
        SendCanMessage($"18EF{currentIdHex}00", new byte[] { 0xFA, 0x73, 0x61, 0x76, 0x65, 0x00, 0x00, 0x00 }, 8);

        //_currentCanId1 = newIdHex;
        //UpdateLatestCanIdLabel1(newIdHex);
        ConfirmCanIdView1.IsVisible = false;
        InitialBasView.IsVisible = true;
    }

    private void SendCanMessage(string canIdHex, byte[] data, int dataLen)
    {
        if (_pcanUsb == null || !_isStarted)
            return;

        uint canId = uint.Parse(canIdHex, NumberStyles.HexNumber);
        byte[] paddedData = data.Length < dataLen
            ? data.Concat(Enumerable.Repeat((byte)0x00, dataLen - data.Length)).ToArray()
            : data;

        var status = _pcanUsb.WriteFrame(canId, dataLen, paddedData, canId > 0x7FF);
        // You can optionally log the status
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

    private void UpdateLatestCanIdLabel1(string id)
    {
        LatestCanIdLabel1.Text = _localizationResourceManager["CurrentBAS"] + " " + id;
    }

    private async void OnAngleSensorStatusClicked(object sender, EventArgs e)
    {
        await ShowAngleSensorConnectionStatusAsync();
    }

    private async void OnAngleSensorButtonClicked(object sender, EventArgs e)
    {
        // Determine connection state (replace with your actual check)
        bool isAngleSensorConnected = _isAngleSensorConnected;
        await Navigation.PushAsync(new ASConnectionStatusPage(isAngleSensorConnected));
    }

    private async Task ShowAngleSensorConnectionStatusAsync()
    {
        string message = _isAngleSensorConnected
            ? "Angle Sensor is CONNECTED."
            : "Angle Sensor is NOT CONNECTED.";
        await DisplayAlert("Angle Sensor Connection", message, "OK");
    }

    private async void OnCheckConnectionClicked(object sender, EventArgs e)
    {
#if WINDOWS
        bool isConnected = ASConnectionState.IsConnected;
        string message = isConnected
            ? "Angle Sensor is CONNECTED."
            : "Angle Sensor is NOT CONNECTED.";
        await DisplayAlert("Connection Status", message, "OK");
#endif
    }
#endif
    private void NewCanIdEntry_Focused1(object sender, FocusEventArgs e)
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
public class CanMessageViewModel1
{
    public string Direction { get; set; } = "";
    public string Id { get; set; } = "";
    public string Data { get; set; } = "";
}
