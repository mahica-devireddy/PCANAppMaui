#if WINDOWS

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

    public partial class BAS : ContentPage
    {
        readonly ILocalizationResourceManager _loc;
        readonly ICanBusService _bus;

        string? _currentCanId;
        string? _pendingCanId;
        bool _isAngleSensorConnected;
        Timer? _connectionTimeoutTimer;
        bool _sideMenuFirstOpen = true;

        public BAS(
            ILocalizationResourceManager loc,
            ICanBusService bus
        )
        {
            InitializeComponent();
            _loc = loc;
            _bus = bus;

            // subscribe to CAN frames
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
            if (pgn == 0xFFBB)
            {
                _isAngleSensorConnected = true;
                ResetConnectionTimeout();
            }

            var idHex = $"0x{packet.Id:X}";
            var lastTwo = idHex.Length >= 2 ? idHex[^2..] : idHex;
            if (int.TryParse(lastTwo, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var canIdInt))
            {
                _currentCanId = canIdInt.ToString();
                MainThread.BeginInvokeOnMainThread(() =>
                    LatestCanIdLabel1.Text = $"{_loc["CurrentBAS"]} {_currentCanId}");
            }
        }

        void ResetConnectionTimeout()
        {
            _connectionTimeoutTimer?.Stop();
            _connectionTimeoutTimer = new Timer(2000) { AutoReset = false };
            _connectionTimeoutTimer.Elapsed += (_, __) =>
            {
                _isAngleSensorConnected = false;
                _connectionTimeoutTimer?.Stop();
            };
            _connectionTimeoutTimer.Start();
        }

        // ─── Language toggle ───────────────────────────────────────────────────────
        private void OnLanguageButtonClicked(object sender, EventArgs e)
        {
            LanguageState.CurrentLanguage =
                LanguageState.CurrentLanguage == "en" ? "es" : "en";
            _loc.CurrentCulture =
                new CultureInfo(LanguageState.CurrentLanguage);
        }

        // ─── CAN‐ID change flow ─────────────────────────────────────────────────────
        private void OnSetCanIdClicked(object sender, EventArgs e)
        {
            SetCanIdView1.IsVisible = true;
            InitialBasView.IsVisible = false;
        }

        private async void OnSetClicked(object sender, EventArgs e)
        {
            var txt = NewCanIdEntry1.Text?.Trim();
            if (string.IsNullOrEmpty(txt)
                || !int.TryParse(txt, out var newCanId)
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
            SetCanIdView1.IsVisible = false;
            ConfirmCanIdView1.IsVisible = true;
            _pendingCanId = newCanId.ToString();
            NewCanIdEntry1.Text = string.Empty;
        }

        private void OnCancelConfirmClicked1(object sender, EventArgs e)
        {
            ConfirmCanIdView1.IsVisible = false;
            InitialBasView.IsVisible = true;
        }

        private async void OnConfirmClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_pendingCanId)) return;

            int.TryParse(_currentCanId, out var currId);
            byte currB = (byte)currId;
            byte newB = (byte)int.Parse(_pendingCanId);

            // your 4‐msg protocol
            _bus.SendFrame(
                uint.Parse($"18EA{currB:X2}00", NumberStyles.HexNumber),
                new byte[] { 0x00, 0xEF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                extended: true);
            _bus.SendFrame(
                uint.Parse($"18EF{currB:X2}00", NumberStyles.HexNumber),
                new byte[] { 0x06, newB, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF },
                extended: true);
            _bus.SendFrame(
                uint.Parse($"18EA{currB:X2}00", NumberStyles.HexNumber),
                new byte[] { 0x00, 0xEF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                extended: true);
            _bus.SendFrame(
                uint.Parse($"18EF{currB:X2}00", NumberStyles.HexNumber),
                new byte[] { 0xFA, 0x73, 0x61, 0x76, 0x65, 0x00, 0x00, 0x00 },
                extended: true);

            ConfirmCanIdView1.IsVisible = false;
            InitialBasView.IsVisible = true;
        }

        // ─── Show connection status ───────────────────────────────────────────────
        private async void OnAngleSensorStatusClicked(object sender, EventArgs e)
        {
            string msg = _isAngleSensorConnected
                ? "Angle Sensor is CONNECTED."
                : "Angle Sensor is NOT CONNECTED.";
            await DisplayAlert("Connection Status", msg, "OK");
        }

        private async void OnAngleSensorButtonClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new ASConnectionStatusPage(_isAngleSensorConnected));

        // ─── Exit from Set flow ────────────────────────────────────────────────────
        private void OnExitClicked(object sender, EventArgs e)
        {
            SetCanIdView1.IsVisible = false;
            ConfirmCanIdView1.IsVisible = false;
            InitialBasView.IsVisible = true;
        }


        // ─── Side‐menu (your existing handlers) ──────────────────────────────────
        private void OnOshkoshLogoClicked(object sender, EventArgs e)
        {
            SideMenu.IsVisible = true;
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

        async Task CloseAndNavigate(Func<Task> nav)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible = false;
            SideMenuDim.IsVisible = false;
            await nav();
        }

        private void OnMenuClicked(object sender, EventArgs e)
            => _ = CloseAndNavigate(() => Navigation.PushAsync(new Menu(_loc, _bus)));

        private void OnAngleSensorMenuClicked(object sender, EventArgs e)
            => _ = CloseAndNavigate(() => Navigation.PushAsync(new BAS(_loc, _bus)));

        private void OnKzValveMenuClicked(object sender, EventArgs e)
            => _ = CloseAndNavigate(() => Navigation.PushAsync(new KZV(_loc, _bus)));

        private void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
            => _ = CloseAndNavigate(() => Navigation.PushAsync(new FTLS(_loc, _bus)));
    }
}
#endif
