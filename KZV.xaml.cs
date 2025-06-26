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
    public partial class KZV : ContentPage
    {
        readonly ILocalizationResourceManager _loc;
        readonly ICanBusService               _bus;

        string? _currentCanId;
        string? _pendingCanId;
        bool    _isStarted;
        bool    _isKZVConnected;
        Timer?  _connectionTimeoutTimer;
        bool    _sideMenuFirstOpen = true;

        public KZV(
            ILocalizationResourceManager loc,
            ICanBusService               bus
        )
        {
            InitializeComponent();
            _loc       = loc;
            _bus       = bus;
            _bus.FrameReceived += OnFrameReceived;
            _isStarted = _bus.IsConnected;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _bus.FrameReceived -= OnFrameReceived;
        }

        // Called on every incoming CAN frame
        private void OnFrameReceived(PCAN_USB.Packet packet)
        {
            uint pgn = (packet.Id >> 8) & 0xFFFF;
            if (pgn == 0xFECA)
            {
                _isKZVConnected = true;
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
                    LatestCanIdLabel.Text = $"{_loc["CurrentKZV"]} {_currentCanId}"
                );
            }
        }

        private void ResetConnectionTimeout()
        {
            _connectionTimeoutTimer?.Stop();
            _connectionTimeoutTimer = new Timer(2000) { AutoReset = false };
            _connectionTimeoutTimer.Elapsed += (_, __) =>
            {
                _isKZVConnected = false;
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
            SetCanIdView.IsVisible    = true;
            InitialKzvView.IsVisible   = false;
        }

        private async void OnSetClicked(object sender, EventArgs e)
        {
            var text = NewCanIdEntry.Text?.Trim();
            if (string.IsNullOrEmpty(text)
                || !int.TryParse(text, out var newId)
                || newId < 0
                || newId > 255)
            {
                await DisplayAlert(
                    _loc["Error"],
                    _loc["Please enter a valid CAN ID value between 0-255."],
                    "OK"
                );
                return;
            }

            ConfirmText.Text     = $"Set The CAN ID to {newId}";
            SetCanIdView.IsVisible      = false;
            ConfirmCanIdView.IsVisible  = true;
            _pendingCanId               = newId.ToString();
            NewCanIdEntry.Text          = string.Empty;
        }

        private void OnCancelConfirmClicked(object sender, EventArgs e)
        {
            ConfirmCanIdView.IsVisible = false;
            InitialKzvView.IsVisible    = true;
        }

        private void OnExitClicked(object sender, EventArgs e)
        {
            SetCanIdView.IsVisible      = false;
            InitialKzvView.IsVisible     = true;
            ConfirmCanIdView.IsVisible   = false;
        }

        private async void OnConfirmClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_pendingCanId)) return;
            if (!int.TryParse(_currentCanId, out var curr)) curr = 0;
            byte currB = (byte)curr;
            byte newB  = (byte)int.Parse(_pendingCanId);

            uint canId = (0x18EF0000u)
                       | ((uint)currB << 8)
                       | 0x01u;

            var data = new byte[8];
            data[3]  = 0x04;
            data[4]  = newB;

            _bus.SendFrame(canId, data, extended: true);

            ConfirmCanIdView.IsVisible = false;
            InitialKzvView.IsVisible   = true;
        }

        // ─── Check Connection Dialog ───────────────────────────────────────
        private async void OnCheckConnectionClicked(object sender, EventArgs e)
        {
            string msg = _isKZVConnected
                ? "KZ Valve is CONNECTED."
                : "KZ Valve is NOT CONNECTED.";
            await DisplayAlert("Connection Status", msg, "OK");
        }

        // ─── Side‐Menu Logic ────────────────────────────────────────────────
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
