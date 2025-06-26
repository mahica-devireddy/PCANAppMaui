using System;
using System.Globalization;
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
#if WINDOWS
    public partial class FTLS : ContentPage
    {
        readonly ILocalizationResourceManager _loc;
        readonly ICanBusService               _bus;

        string?      _currentCanId;
        string?      _pendingCanId;
        bool         _isFtlsConnected;
        Timer?       _connectionTimeoutTimer;
        bool         _sideMenuFirstOpen = true;

        public FTLS(
            ILocalizationResourceManager loc,
            ICanBusService               bus
        )
        {
            InitializeComponent();
            _loc = loc;
            _bus = bus;
            _bus.FrameReceived += OnFrameReceived;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _bus.FrameReceived -= OnFrameReceived;
        }

        void OnFrameReceived(PCAN_USB.Packet packet)
        {
            uint pgn = (packet.Id >> 8) & 0xFFFF;
            if (pgn == 0xFF52)
            {
                _isFtlsConnected = true;
                ResetConnectionTimeout();
            }

            var idHex = $"0x{packet.Id:X}";
            var lastTwo = idHex.Length >= 2 ? idHex[^2..] : idHex;
            if (int.TryParse(lastTwo, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var canIdInt))
            {
                _currentCanId = canIdInt.ToString();
                MainThread.BeginInvokeOnMainThread(() =>
                    LatestCanIdLabel2.Text = $"{_loc["CurrentFTLS"]} {_currentCanId}");
            }
        }

        void ResetConnectionTimeout()
        {
            _connectionTimeoutTimer?.Stop();
            _connectionTimeoutTimer = new Timer(2000) { AutoReset = false };
            _connectionTimeoutTimer.Elapsed += (_, __) =>
            {
                _isFtlsConnected = false;
                _connectionTimeoutTimer?.Stop();
            };
            _connectionTimeoutTimer.Start();
        }

        private void OnLanguageButtonClicked(object sender, EventArgs e)
        {
            LanguageState.CurrentLanguage =
                LanguageState.CurrentLanguage == "en" ? "es" : "en";
            _loc.CurrentCulture =
                new CultureInfo(LanguageState.CurrentLanguage);
        }

        private void OnSetCanIdClicked(object sender, EventArgs e)
        {
            SetCanIdView2.IsVisible = true;
            InitialFtlsView.IsVisible = false;
        }

        private async void OnSetClicked(object sender, EventArgs e)
        {
            var txt = NewCanIdEntry2.Text?.Trim();
            if (string.IsNullOrEmpty(txt)
                || !int.TryParse(txt, out var newId)
                || newId < 0
                || newId > 255)
            {
                await DisplayAlert(
                    _loc["Error"],
                    _loc["Please enter a valid CAN ID value between 0-255."],
                    "OK");
                return;
            }

            ConfirmText2.Text        = $"Set The CAN ID to {newId}";
            SetCanIdView2.IsVisible  = false;
            ConfirmCanIdView2.IsVisible = true;
            _pendingCanId            = newId.ToString();
            NewCanIdEntry2.Text      = string.Empty;
        }

        private void OnCancelConfirmClicked2(object sender, EventArgs e)
        {
            ConfirmCanIdView2.IsVisible = false;
            InitialFtlsView.IsVisible   = true;
        }

        private async void OnConfirmClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_pendingCanId)) return;

            int.TryParse(_currentCanId, out var curr);
            byte currB = (byte)curr;
            byte newB  = (byte)int.Parse(_pendingCanId);

            // first message (e.g. "0CEF…")
            var canIdHex = $"0CEF{currB:X2}02";
            _bus.SendFrame(
                uint.Parse(canIdHex, NumberStyles.HexNumber),
                new byte[]{0x72,0x6F,0x74,0x61,0x2D,0x65,0x6E,0x6A},
                extended: true);

            await Task.Delay(100);

            // second message: single‐byte payload
            _bus.SendFrame(
                uint.Parse(canIdHex, NumberStyles.HexNumber),
                new byte[]{newB},
                extended: true);

            ConfirmCanIdView2.IsVisible = false;
            InitialFtlsView.IsVisible   = true;
        }

        private async void OnFTLSStatusClicked(object sender, EventArgs e)
        {
            string msg = _isFtlsConnected
                ? "Fluid Tank Level Sensor is CONNECTED."
                : "Fluid Tank Level Sensor is NOT CONNECTED.";
            await DisplayAlert("Connection Status", msg, "OK");
        }

        private async void OnFTLSButtonClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new FTLSConnectionStatusPage(_isFtlsConnected));

        private void OnExitClicked(object sender, EventArgs e)
        {
            SetCanIdView2.IsVisible       = false;
            ConfirmCanIdView2.IsVisible   = false;
            InitialFtlsView.IsVisible     = true;
        }

        // ── side menu (same pattern) ────────────────────────────────────────────
        private void OnOshkoshLogoClicked(object sender, EventArgs e)
        {
            SideMenu.IsVisible    = true;
            SideMenuDim.IsVisible = true;
            if (SideMenu.Width == 0)
                SideMenu.SizeChanged += SideMenu_SizeChangedAnimateIn;
            else
                _ = AnimateSideMenuIn();
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

        async Task CloseAndNavigate(Func<Task> nav)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await nav();
        }

        private void OnMenuClicked(object sender, EventArgs e)
            => _ = CloseAndNavigate(() => Navigation.PushAsync(new Menu(_loc)));

        private void OnAngleSensorMenuClicked(object sender, EventArgs e)
            => _ = CloseAndNavigate(() => Navigation.PushAsync(new BAS(_loc)));

        private void OnKzValveMenuClicked(object sender, EventArgs e)
            => _ = CloseAndNavigate(() => Navigation.PushAsync(new KZV(_loc)));

        private void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
            => _ = CloseAndNavigate(() => Navigation.PushAsync(new FTLS(_loc)));
    }
#endif
}
