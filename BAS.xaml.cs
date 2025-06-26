#if WINDOWS

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using LocalizationResourceManager.Maui;
using PCANAppM.Services;       // ← your single service
using PCANAppM.Platforms.Windows;
using PCANAppM.Resources.Languages;
using Peak.Can.Basic;
using Timer = System.Timers.Timer;

namespace PCANAppM
{

    public partial class BAS : ContentPage
    {
        readonly ILocalizationResourceManager _loc;
        readonly ICanBusService               _bus;

        string?                             _currentCanId;
        string?                             _pendingCanId;
        bool                                _isStarted;
        bool                                _isAngleSensorConnected;
        System.Timers.Timer?                _connectionTimeoutTimer;
        bool                                _sideMenuFirstOpen = true;

        public BAS(
            ILocalizationResourceManager loc,
            ICanBusService               bus
        )
        {
            InitializeComponent();
            _loc = loc;
            _bus = bus;

            // subscribe to live CAN frames
            _bus.FrameReceived += OnFrameReceived;
            _isStarted = _bus.IsConnected;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _bus.FrameReceived -= OnFrameReceived;
        }

        private void OnFrameReceived(PCAN_USB.Packet packet)
        {
            // detect your device’s PGN
            uint pgn = (packet.Id >> 8) & 0xFFFF;
            if (pgn == 0xFFBB)
            {
                _isAngleSensorConnected = true;
                ResetConnectionTimeout();
            }

            // extract last‐two hex digits as decimal
            var idHex = $"0x{packet.Id:X}";
            var lastTwo = idHex.Length >= 2
                ? idHex.Substring(idHex.Length - 2)
                : idHex;

            if (int.TryParse(lastTwo, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int canIdInt))
            {
                _currentCanId = canIdInt.ToString();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LatestCanIdLabel1.Text = $"{_loc["CurrentBAS"]} {_currentCanId}";
                });
            }
        }

        private void ResetConnectionTimeout()
        {
            _connectionTimeoutTimer?.Stop();
            _connectionTimeoutTimer = new System.Timers.Timer(2000) { AutoReset = false };
            _connectionTimeoutTimer.Elapsed += (_, __) =>
            {
                _isAngleSensorConnected = false;
                _connectionTimeoutTimer?.Stop();
            };
            _connectionTimeoutTimer.Start();
        }

        // ─── Set CAN ID flow ─────────────────────────────────────────────────────

        private void OnLanguageButtonClicked(object sender, EventArgs e) 
        {
            LanguageState.CurrentLanguage = 
                LanguageState.CurrentLanguage == "en" ? "es" : "en"; 
            _loc.CurrentCulture = new System.Globalization.CultureInfo(LanguageState.CurrentLanguage); 
        }


        private void OnSetCanIdClicked(object sender, EventArgs e)
        {
            SetCanIdView1.IsVisible = true;
            InitialBasView.IsVisible = false;
        }

        private async void OnSetClicked(object sender, EventArgs e)
        {
            var text = NewCanIdEntry1.Text?.Trim();
            if (string.IsNullOrEmpty(text)
                || !int.TryParse(text, out int newCanId)
                || newCanId < 0
                || newCanId > 255)
            {
                await DisplayAlert(
                    _loc["Error"],
                    _loc["Please enter a valid CAN ID value between 0-255."],
                    "OK");
                return;
            }

            ConfirmText1.Text = $"Set The CAN ID to {newCanId}";
            SetCanIdView1.IsVisible     = false;
            ConfirmCanIdView1.IsVisible = true;
            _pendingCanId               = newCanId.ToString();
            NewCanIdEntry1.Text         = string.Empty;
        }

        private void OnCancelConfirmClicked1(object sender, EventArgs e)
        {
            ConfirmCanIdView1.IsVisible = false;
            InitialBasView.IsVisible    = true;
        }

        private async void OnConfirmClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_pendingCanId)) return;

            // build your two‐step protocol (4 messages)
            int.TryParse(_currentCanId, out int currId);
            byte currByte = (byte) currId;
            byte newByte  = (byte) int.Parse(_pendingCanId);

            // msg #1
            _bus.SendFrame(
                uint.Parse($"18EA{currByte:X2}00", NumberStyles.HexNumber),
                new byte[]{0x00,0xEF,0x00,0x00,0x00,0x00,0x00,0x00},
                extended: true);
            // msg #2
            _bus.SendFrame(
                uint.Parse($"18EF{currByte:X2}00", NumberStyles.HexNumber),
                new byte[]{0x06,newByte,0x00,0xFF,0xFF,0xFF,0xFF,0xFF},
                extended: true);
            // msg #3
            _bus.SendFrame(
                uint.Parse($"18EA{currByte:X2}00", NumberStyles.HexNumber),
                new byte[]{0x00,0xEF,0x00,0x00,0x00,0x00,0x00,0x00},
                extended: true);
            // msg #4
            _bus.SendFrame(
                uint.Parse($"18EF{currByte:X2}00", NumberStyles.HexNumber),
                new byte[]{0xFA,0x73,0x61,0x76,0x65,0x00,0x00,0x00},
                extended: true);

            ConfirmCanIdView1.IsVisible = false;
            InitialBasView.IsVisible    = true;
        }

        // ─── Connection Status popup ────────────────────────────────────────────

        private async void OnAngleSensorStatusClicked(object sender, EventArgs e)
        {
            string msg = _isAngleSensorConnected
                ? "Angle Sensor is CONNECTED."
                : "Angle Sensor is NOT CONNECTED.";
            await DisplayAlert("Connection Status", msg, "OK");
        }

        private async void OnAngleSensorButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ASConnectionStatusPage(_isAngleSensorConnected));
        }

        // ─── Side-menu (exactly your existing handlers) ──────────────────────────

        private async void OnCheckConnectionClicked(object sender, EventArgs e) 
        {
            bool isConnected = _bus.IsConnected;
        }

        private void OnOshkoshLogoClicked(object sender, EventArgs e)
        {
            SideMenu.IsVisible    = true;
            SideMenuDim.IsVisible = true;
            if (SideMenu.Width == 0)
                SideMenu.SizeChanged += SideMenu_SizeChangedAnimateIn;
            else
                AnimateSideMenuIn();
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
            _sideMenuFirstOpen     = false;
            SideMenu.TranslationX  = -SideMenu.Width;
            await SideMenu.TranslateTo(0, 0, 250, Easing.SinOut);
        }

        private async void OnCloseSideMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 250, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
        }

        private async void OnMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new Menu(_loc, _bus));
        }

        private async void OnAngleSensorMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new BAS(_loc, _bus));
        }

        private async void OnKzValveMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new KZV(_loc, _bus));
        }

        private async void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new FTLS(_loc, _bus));
        }
    }
}
#endif
