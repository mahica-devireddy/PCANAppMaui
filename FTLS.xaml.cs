#if WINDOWS

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using LocalizationResourceManager.Maui;
using PCANAppM.Services;           // ← your single service
using PCANAppM.Platforms.Windows;
using PCANAppM.Resources.Languages;
using Peak.Can.Basic;
using Timer = System.Timers.Timer;

namespace PCANAppM
{
    public partial class FTLS : ContentPage
    {
        readonly ILocalizationResourceManager _loc;
        readonly ICanBusService               _bus;

        string? _currentCanId;
        string? _pendingCanId;
        bool    _isStarted;
        bool    _isFTLSConnected;
        Timer?  _connectionTimeoutTimer;
        bool    _sideMenuFirstOpen = true;

        public FTLS(
            ILocalizationResourceManager loc,
            ICanBusService               bus
        )
        {
            InitializeComponent();
            _loc = loc;
            _bus = bus;

            // Subscribe to incoming frames
            _bus.FrameReceived += OnFrameReceived;
            _isStarted = _bus.IsConnected;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _bus.FrameReceived -= OnFrameReceived;
        }

        void OnFrameReceived(PCAN_USB.Packet packet)
        {
            // FTLS PGN
            uint pgn = (packet.Id >> 8) & 0xFFFF;
            if (pgn == 0xFF52)
            {
                _isFTLSConnected = true;
                ResetConnectionTimeout();
            }

            // display last two hex digits
            var idHex = $"0x{packet.Id:X}";
            var lastTwo = idHex.Length >= 2
                ? idHex.Substring(idHex.Length - 2)
                : idHex;

            if (int.TryParse(
                lastTwo, NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out var cid))
            {
                _currentCanId = cid.ToString();
                MainThread.BeginInvokeOnMainThread(() =>
                    LatestCanIdLabel2.Text =
                        $"{_loc["CurrentFTLS"]} {_currentCanId}"
                );
            }
        }

        void ResetConnectionTimeout()
        {
            _connectionTimeoutTimer?.Stop();
            _connectionTimeoutTimer = new Timer(2000) { AutoReset = false };
            _connectionTimeoutTimer.Elapsed += (_, __) =>
            {
                _isFTLSConnected = false;
                _connectionTimeoutTimer?.Stop();
            };
            _connectionTimeoutTimer.Start();
        }

        // ─── Language toggle ─────────────────────────────────────────────────────
        private void OnLanguageButtonClicked(object sender, EventArgs e)
        {
            LanguageState.CurrentLanguage =
                LanguageState.CurrentLanguage == "en" ? "es" : "en";
            _loc.CurrentCulture =
                new CultureInfo(LanguageState.CurrentLanguage);
        }

        // ─── Set CAN ID flow ─────────────────────────────────────────────────────
        private void OnSetCanIdClicked(object sender, EventArgs e)
        {
            SetCanIdView2.IsVisible  = true;
            InitialFtlsView.IsVisible = false;
        }

        private async void OnSetClicked(object sender, EventArgs e)
        {
            var txt = NewCanIdEntry2.Text?.Trim();
            if (string.IsNullOrEmpty(txt)
                || !int.TryParse(txt, out var nid)
                || nid < 0 || nid > 255)
            {
                await DisplayAlert(
                    _loc["Error"],
                    _loc["Please enter a valid CAN ID value between 0-255."],
                    "OK");
                return;
            }

            ConfirmText2.Text            = $"Set The CAN ID to {nid}";
            SetCanIdView2.IsVisible      = false;
            ConfirmCanIdView2.IsVisible  = true;
            _pendingCanId                = nid.ToString();
            NewCanIdEntry2.Text          = string.Empty;
        }

        private void OnCancelConfirmClicked1(object sender, EventArgs e)
        {
            ConfirmCanIdView2.IsVisible = false;
            InitialFtlsView.IsVisible   = true;
        }

        private async void OnConfirmClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_pendingCanId)) return;

            if (!int.TryParse(_currentCanId, out var cur)) cur = 0;
            byte curB = (byte)cur;
            byte newB = (byte)int.Parse(_pendingCanId);

            // Protocol: send first ASCII payload
            var firstData = new byte[]{ 0x72,0x6F,0x74,0x61,0x2D,0x65,0x6E,0x6A };
            uint  canIdHex = uint.Parse($"0CEF{curB:X2}02", NumberStyles.HexNumber);
            _bus.SendFrame(canIdHex, firstData, extended: true);
            await Task.Delay(100);

            // then new ID byte
            _bus.SendFrame(canIdHex,
                new byte[]{ newB },
                extended: true
            );

            ConfirmCanIdView2.IsVisible = false;
            InitialFtlsView.IsVisible   = true;
        }

        // ─── Connection Status popup ────────────────────────────────────────────
        private async void OnCheckConnectionClicked(object sender, EventArgs e)
        {
            var msg = _isFTLSConnected
                ? "Fluid Tank Level Sensor is CONNECTED."
                : "Fluid Tank Level Sensor is NOT CONNECTED.";
            await DisplayAlert("Connection Status", msg, "OK");
        }

        private async void OnFTLSButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(
                new FTLSConnectionStatusPage(_isFTLSConnected)
            );
        }

        // ─── Side-menu Handlers ────────────────────────────────────────────────────

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

        private Task AnimateSideMenuIn() =>
            SideMenu.TranslateTo(0, 0, 250, Easing.SinOut);

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
            await AnimateSideMenuIn();
            await Navigation.PushAsync(new Menu(_loc, _bus));
        }

        private async void OnAngleSensorMenuClicked(object sender, EventArgs e)
        {
            await AnimateSideMenuIn();
            await Navigation.PushAsync(new BAS(_loc, _bus));
        }

        private async void OnKzValveMenuClicked(object sender, EventArgs e)
        {
            await AnimateSideMenuIn();
            await Navigation.PushAsync(new KZV(_loc, _bus));
        }

        private async void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
        {
            await AnimateSideMenuIn();
            await Navigation.PushAsync(new FTLS(_loc, _bus));
        }
    }
}

#endif
