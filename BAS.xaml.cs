using System;
using System.Globalization;
using System.Linq;
using System.Timers;
using Microsoft.Maui.Controls;
using LocalizationResourceManager.Maui;
using PCANAppM.Platforms.Windows;  // for PCAN_USB.Packet
using PCANAppM.Services;

namespace PCANAppM
{
#if WINDOWS
    public partial class BAS : ContentPage
    {
        readonly ILocalizationResourceManager _loc;
        readonly PcanUsbStatusService         _statusService;

        Timer?   _connectionTimer;
        string?  _currentCanId;
        bool     _sideMenuFirstOpen = true;

        public BAS(
            ILocalizationResourceManager loc,
            PcanUsbStatusService         statusService
        )
        {
            InitializeComponent();
            _loc           = loc;
            _statusService = statusService;

            // 1) Watch for USB plug/unplug
            _statusService.StatusChanged += OnStatusChanged;
            // 2) Watch for incoming CAN frames
            _statusService.PcanUsb?.MessageReceived += OnCanMessageReceived;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateConnectionUI();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _statusService.StatusChanged -= OnStatusChanged;
            _statusService.PcanUsb?.MessageReceived -= OnCanMessageReceived;
        }

        void OnStatusChanged()
            => MainThread.BeginInvokeOnMainThread(UpdateConnectionUI);

        void UpdateConnectionUI()
        {
            bool connected = _statusService.IsConnected;
            ConnectionStatusLabel.Text = connected
                ? _loc["Status2"]
                : _loc["Status1"];
        }

        void OnCanMessageReceived(PCAN_USB.Packet packet)
        {
            // detect your BAS PGN, e.g. 0xFFBB
            uint pgn = (packet.Id >> 8) & 0xFFFF;
            if (pgn == 0xFFBB)
                ResetConnectionTimer();

            // display the low byte as “current CAN ID”
            _currentCanId = (packet.Id & 0xFF).ToString();
            MainThread.BeginInvokeOnMainThread(() =>
                CurrentCanIdLabel.Text = $"{_loc["CurrentBAS"]} {_currentCanId}");
        }

        void ResetConnectionTimer()
        {
            _connectionTimer?.Stop();
            _connectionTimer = new Timer(2000) { AutoReset = false };
            _connectionTimer.Elapsed += (_,__) =>
                MainThread.BeginInvokeOnMainThread(() =>
                    ConnectionStatusLabel.Text = _loc["Status1"]);
            _connectionTimer.Start();
        }

        // ───── your “set CAN ID” flow ─────

        async void OnSetCanIdClicked(object sender, EventArgs e)
        {
            SetCanIdView.IsVisible     = true;
            InitialBasView.IsVisible    = false;
        }

        async void OnConfirmClicked(object sender, EventArgs e)
        {
            if (!int.TryParse(NewCanIdEntry.Text, out var newId) || newId < 0 || newId > 255)
            {
                await DisplayAlert("Invalid Input",
                    "Please enter a valid CAN ID value between 0-255.",
                    "OK");
                return;
            }

            // Example BAS protocol: two messages
            string hex = newId.ToString("X2");
            uint canId1 = Convert.ToUInt32($"18EA{hex}00", 16);
            byte[] data1 = new byte[]{ /* your bytes */ };
            _statusService.PcanUsb?.WriteFrame(canId1, 8, data1, true);

            uint canId2 = Convert.ToUInt32($"18EF{hex}00", 16);
            byte[] data2 = new byte[]{ /* your bytes */ };
            _statusService.PcanUsb?.WriteFrame(canId2, 8, data2, true);

            ConfirmCanIdView1.IsVisible = false;
            InitialBasView.IsVisible    = true;
        }

        void OnCancelClicked(object sender, EventArgs e)
        {
            ConfirmCanIdView1.IsVisible = false;
            InitialBasView.IsVisible    = true;
        }

        // ───── side-menu logic (identical to your other pages) ─────

        void OnOshkoshLogoClicked(object sender, EventArgs e)
        {
            SideMenu.IsVisible    = true;
            SideMenuDim.IsVisible = true;
            if (SideMenu.Width == 0)
                SideMenu.SizeChanged += SideMenu_SizeChangedAnimateIn;
            else
                _ = AnimateSideMenuIn();
        }

        async void SideMenu_SizeChangedAnimateIn(object? sender, EventArgs e)
        {
            if (SideMenu.Width > 0)
            {
                SideMenu.SizeChanged -= SideMenu_SizeChangedAnimateIn;
                await AnimateSideMenuIn();
            }
        }

        Task AnimateSideMenuIn()
        {
            SideMenu.TranslationX = -SideMenu.Width;
            return SideMenu.TranslateTo(0, 0, 250, Easing.SinOut);
        }

        async void OnCloseSideMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 250, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
        }

        async void OnMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new Menu(_loc, _statusService));
        }

        async void OnAngleSensorMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new BAS(_loc, _statusService));
        }

        async void OnKzValveMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new KZV(_loc, _statusService));
        }

        async void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new FTLS(_loc, _statusService));
        }
    }
#endif
}
