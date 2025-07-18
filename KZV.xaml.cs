#if WINDOWS

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using LocalizationResourceManager.Maui;
using PCANAppM.Services;
using PCANAppM.Platforms.Windows;
using PCANAppM.Resources.Languages;
using Peak.Can.Basic;
using Timer = System.Timers.Timer;

namespace PCANAppM
{
    public partial class KZV : ContentPage
    {
        private readonly ILocalizationResourceManager _loc;
        private readonly CanBusService _bus;

        private Timer? _readTimer;
        private int _previousDeviceCount = 0;

        public KZV(ILocalizationResourceManager localizationResourceManager, CanBusService canBusService)
        {
            _loc = localizationResourceManager;
            _bus = canBusService;
            InitializeComponent();

            // Subscribe to device list changes for disconnect alert
            _bus.DeviceListChanged += OnDeviceListChanged;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _previousDeviceCount = _bus.AvailableDevices.Count;
            _bus.StartReading(); // Start reading CAN messages

           
            _readTimer = new Timer(100);
            _readTimer.Elapsed += (_, __) => ReadCanMessages();
            _readTimer.AutoReset = true;
            _readTimer.Start();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _bus.DeviceListChanged -= OnDeviceListChanged;
            _bus.StopReading(); // Stop reading CAN messages
            if (_readTimer != null)
            {
                _readTimer.Stop();
                _readTimer.Dispose();
                _readTimer = null;
            }
        }

        // Alert if disconnected
        private void OnDeviceListChanged()
        {
            var devices = _bus.AvailableDevices;
            if (_previousDeviceCount > 0 && devices.Count == 0)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Disconnected", "PCAN USB device has been disconnected.", "OK");
                });
            }
            _previousDeviceCount = devices.Count;
        }

        // Display current CAN ID (last two hex digits)
        private void ReadCanMessages()
        {
            var latest = _bus.ReceivedPackets
                .Where(pkt => pkt.IsExtended)
                .OrderByDescending(pkt => pkt.Microseconds)
                .FirstOrDefault();

            string labelText;
            if (latest != null)
            {
                string idHex = latest.Id.ToString("X");
                string lastTwo = idHex.Length >= 2 ? idHex.Substring(idHex.Length - 2) : idHex;
                labelText = $"{_loc["CurrentKZV"]} {lastTwo}";
            }
            else
            {
                labelText = $"{_loc["CurrentKZV"]} --";
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (this.FindByName<Label>("LatestCanIdLabel") is Label label)
                    label.Text = labelText;
            });
        }

        // --- Side menu handlers unchanged ---
        private void OnOshkoshLogoClicked(object sender, EventArgs e) { /* … */ }
        private void OnLanguageButtonClicked(object sender, EventArgs e) { /* … */ }
        private void OnMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnAngleSensorMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnKzValveMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnFluidTankLevelMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnCloseSideMenuClicked(object sender, EventArgs e) { /* ...*/ }
    }
}
#endif


------------------------------------------------------------------------------
using LocalizationResourceManager.Maui;
using Peak.Can.Basic;
using System.Collections.ObjectModel;
using System.Globalization;
using PCANAppM;

#if WINDOWS
using PCANAppM.Platforms.Windows;
using PCANAppM.Services;
#endif

namespace PCANAppM;

public partial class KZV : ContentPage
{
#if WINDOWS
    private string? _currentCanId = null;
    private string? _pendingNewCanId = null;
    private readonly ILocalizationResourceManager _localizationResourceManager;

    private bool _isStarted = false;
    private bool _sideMenuFirstOpen = true;
    private bool _isKZVConnected = false;
    private System.Timers.Timer? _connectionTimeoutTimer;

    // Use the shared service for device access
    private PCAN_USB? PcanUsb => PcanUsbStatusService.Instance.IsConnected ? PcanUsbStatusService.Instance.PcanUsb : null;

    public KZV(ILocalizationResourceManager localizationResourceManager)
    {
        _localizationResourceManager = localizationResourceManager;
        InitializeComponent();

        if (!PcanUsbStatusService.Instance.IsConnected || PcanUsb == null)
        {
            DisplayAlert("Error", "No PCAN devices found.", "OK");
            return;
        }

        SubscribeToPcanUsbEvents();
        _isStarted = true;
    }

    private void SubscribeToPcanUsbEvents()
    {
        if (PcanUsb == null) return;
        PcanUsb.MessageReceived += OnCanMessageReceived;
        PcanUsb.Feedback += OnPcanFeedback;
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
        _connectionTimeoutTimer?.Stop();
        _connectionTimeoutTimer = new System.Timers.Timer(2000); // 2 seconds
        _connectionTimeoutTimer.Elapsed += (s, e) =>
        {
            KZVConnectionState.IsConnected = false;
            _connectionTimeoutTimer?.Stop();
        };
        _connectionTimeoutTimer.AutoReset = false;
        _connectionTimeoutTimer.Start();
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

        int currentIdInt = 0;
        int.TryParse(_currentCanId, out currentIdInt);

        int newIdInt = int.Parse(_pendingNewCanId);

        byte currentIdByte = (byte)currentIdInt;
        byte newIdByte = (byte)newIdInt;

        uint canId = (0x18EF0000u) | ((uint)currentIdByte << 8) | 0x01u;

        byte[] data = new byte[8];
        data[3] = 0x04;
        data[4] = newIdByte;

        SendCanMessage(canId, data, 8);

        ConfirmCanIdView.IsVisible = false;
        InitialKzvView.IsVisible = true;
    }

    private void SendCanMessage(uint canId, byte[] data, int dataLen)
    {
        if (PcanUsb == null || !_isStarted)
            return;

        byte[] paddedData = data.Length < dataLen
            ? data.Concat(Enumerable.Repeat((byte)0x00, dataLen - data.Length)).ToArray()
            : data;

        var status = PcanUsb.WriteFrame(canId, dataLen, paddedData, canId > 0x7FF);
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

    private void UpdateLatestCanIdLabel(string id)
    {
        LatestCanIdLabel.Text = _localizationResourceManager["CurrentKZV"] + " " + id;
    }

    private async void OnKZVClicked(object sender, EventArgs e)
    {
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

    private async void OnCheckConnectionClicked(object sender, EventArgs e)
    {
        bool isConnected = KZVConnectionState.IsConnected;
        string message = isConnected
            ? "KZ Valve is CONNECTED."
            : "KZ Valve is NOT CONNECTED.";
        await DisplayAlert("Connection Status", message, "OK");
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

    private async void SideMenuOnFirstSizeChanged(object? sender, EventArgs e)
    {
        SideMenu.SizeChanged -= SideMenuOnFirstSizeChanged;
        _sideMenuFirstOpen = false;
        SideMenu.TranslationX = -SideMenu.Width;
        await SideMenu.TranslateTo(0, 0, 250, Easing.SinOut);
    }

    private async void OnCloseSideMenuClicked(object sender, EventArgs e)
    {
        await SideMenu.TranslateTo(-SideMenu.Width, 0, 250, Easing.SinIn);
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
#endif
}

public class CanMessageViewModel
{
    public string Direction { get; set; } = "";
    public string Id { get; set; } = "";
    public string Data { get; set; } = "";
}
