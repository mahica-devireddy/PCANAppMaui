#if WINDOWS
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Maui.Controls;
using LocalizationResourceManager.Maui;
using PCANAppM.Services;
using PCANAppM.Platforms.Windows;
using PCANAppM.Resources.Languages;

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
            uint pgn = (packet.Id >> 8) & 0xFFFF;
            if (pgn == 0xFF52)
            {
                _isFTLSConnected = true;
                ResetConnectionTimeout();
            }

            var idHex = $"0x{packet.Id:X}";
            var lastTwo = idHex.Length >= 2
                ? idHex.Substring(idHex.Length - 2)
                : idHex;

            if (int.TryParse(lastTwo, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var canIdInt))
            {
                _currentCanId = canIdInt.ToString();
                MainThread.BeginInvokeOnMainThread(() =>
                    LatestCanIdLabel2.Text = $"{_loc["CurrentFTLS"]} {_currentCanId}"
                );
            }
        }

        private void ResetConnectionTimeout()
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

        // ─── Language Toggle ─────────────────────────────────────────────
        private void OnLanguageButtonClicked(object sender, EventArgs e)
        {
            LanguageState.CurrentLanguage =
                LanguageState.CurrentLanguage == "en" ? "es" : "en";
            _loc.CurrentCulture =
                new CultureInfo(LanguageState.CurrentLanguage);
        }

        // ─── “Set CAN ID” Flow ────────────────────────────────────────────
        private void OnSetCanIdClicked(object sender, EventArgs e)
        {
            SetCanIdView2.IsVisible  = true;
            InitialFtlsView.IsVisible = false;
        }

        private async void OnSetClicked(object sender, EventArgs e)
        {
            var text = NewCanIdEntry2.Text?.Trim();
            if (string.IsNullOrEmpty(text)
                || !int.TryParse(text, out var newId)
                || newId < 0
                || newId > 255)
            {
                await DisplayAlert(
                    _loc["Error"],
                    _loc["Please enter a valid CAN ID (0-255)."],
                    "OK"
                );
                return;
            }

            ConfirmText2.Text     = $"Set The CAN ID to {newId}";
            SetCanIdView2.IsVisible       = false;
            ConfirmCanIdView2.IsVisible   = true;
            _pendingCanId                 = newId.ToString();
            NewCanIdEntry2.Text           = string.Empty;
        }

        private void OnCancelConfirmClicked2(object sender, EventArgs e)
        {
            ConfirmCanIdView2.IsVisible = false;
            InitialFtlsView.IsVisible   = true;
        }

        private void OnExitClicked(object sender, EventArgs e)
        {
            SetCanIdView2.IsVisible       = false;
            InitialFtlsView.IsVisible     = true;
            ConfirmCanIdView2.IsVisible   = false;
        }

        private async void OnConfirmClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_pendingCanId)) return;
            if (!int.TryParse(_currentCanId, out var curr)) curr = 0;
            byte currB = (byte)curr;
            byte newB  = (byte)int.Parse(_pendingCanId);

            // compose CAN-ID
            string canIdHex = $"0CEF{currB:X2}02";
            uint   canId    = UInt32.Parse(canIdHex, NumberStyles.HexNumber);

            // first message (8 bytes)
            _bus.SendFrame(
                canId,
                new byte[]{ 0x72,0x6F,0x74,0x61,0x2D,0x65,0x6E,0x6A },
                extended: canId > 0x7FF
            );

            // optional delay
            await Task.Delay(100);

            // second message (1 byte)
            _bus.SendFrame(
                canId,
                new byte[]{ newB },
                extended: canId > 0x7FF
            );

            ConfirmCanIdView2.IsVisible = false;
            InitialFtlsView.IsVisible   = true;
        }

        // ─── Check Connection Dialog ───────────────────────────────────────
        private async void OnCheckConnectionClicked(object sender, EventArgs e)
        {
            string msg = _isFTLSConnected
                ? "Fluid Tank Level Sensor is CONNECTED."
                : "Fluid Tank Level Sensor is NOT CONNECTED.";
            await DisplayAlert("Connection Status", msg, "OK");
        }

        // ─── Side-Menu Logic ───────────────────────────────────────────────
        private void OnOshkoshLogoClicked(object sender, EventArgs e)
        {
            SideMenu.IsVisible    = true;
            SideMenuDim.IsVisible = true;
            if (SideMenu.Width == 0)
                SideMenu.SizeChanged += SideMenu_SizeChangedAnimateIn;
            else
                _ = AnimateSideMenuIn();
        }

        private async void SideMenu_SizeChangedAnimateIn(object sender, EventArgs e)
        {
            SideMenu.SizeChanged -= SideMenu_SizeChangedAnimateIn;
            await AnimateSideMenuIn();
        }

        private async Task AnimateSideMenuIn()
        {
            SideMenu.TranslationX = -SideMenu.Width;
            await SideMenu.TranslateTo(0, 0, 250, Easing.SinOut);
        }

        private async void OnCloseSideMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 250, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
        }

        private async void OnMenuClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new Menu(_loc, _bus));

        private async void OnAngleSensorMenuClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new BAS(_loc, _bus));

        private async void OnKzValveMenuClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new KZV(_loc, _bus));

        private async void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new FTLS(_loc, _bus));
    }
}
#endif
