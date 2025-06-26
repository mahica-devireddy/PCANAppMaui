using System;
using System.Globalization;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using LocalizationResourceManager.Maui;
using PCANAppM.Services;
using PCANAppM.Platforms.Windows;
using PCANAppM.Resources.Languages;
using Peak.Can.Basic;

namespace PCANAppM
{
#if WINDOWS
    public partial class KZV : ContentPage
    {
        readonly ILocalizationResourceManager _loc;
        readonly ICanBusService               _bus;

        string? _currentCanId;
        string? _pendingCanId;
        bool    _isKzvStarted;
        bool    _isKzvConnected;
        Timer?  _connectionTimeoutTimer;
        bool    _sideMenuFirstOpen = true;

        public KZV(
            ILocalizationResourceManager loc,
            ICanBusService               bus
        )
        {
            InitializeComponent();
            _loc = loc;
            _bus = bus;

            // Listen for every incoming CAN frame
            _bus.FrameReceived += OnFrameReceived;
            _isKzvStarted = _bus.IsConnected;

            if (!_isKzvStarted)
                DisplayAlert("Error", "No PCAN device found.", "OK");
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _bus.FrameReceived -= OnFrameReceived;
        }

        void OnFrameReceived(PCAN_USB.Packet packet)
        {
            // PGN FECA = KZ Valve keep-alive
            uint pgn = (packet.Id >> 8) & 0xFFFF;
            if (pgn == 0xFECA)
            {
                _isKzvConnected = true;
                ResetConnectionTimeout();
            }

            // show last two hex digits as decimal
            var idHex = $"0x{packet.Id:X}";
            var lastTwo = idHex.Length >= 2
                ? idHex.Substring(idHex.Length - 2)
                : idHex;

            if (int.TryParse(lastTwo, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int val))
            {
                _currentCanId = val.ToString();
                MainThread.BeginInvokeOnMainThread(() =>
                    LatestCanIdLabel.Text = $"{_loc["CurrentKZV"]} {_currentCanId}"
                );
            }
        }

        void ResetConnectionTimeout()
        {
            _connectionTimeoutTimer?.Stop();
            _connectionTimeoutTimer = new Timer(2000) { AutoReset = false };
            _connectionTimeoutTimer.Elapsed += (_, __) =>
            {
                _isKzvConnected = false;
                _connectionTimeoutTimer?.Stop();
            };
            _connectionTimeoutTimer.Start();
        }

        // ─── Set CAN ID Workflow ─────────────────────────────────────────────────

        void OnSetCanIdClicked(object sender, EventArgs e)
        {
            SetCanIdView.IsVisible = true;
            InitialKzvView.IsVisible = false;
        }

        async void OnSetClicked(object sender, EventArgs e)
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
                    "OK");
                return;
            }

            ConfirmText.Text = $"Set The CAN ID to {newId}";
            SetCanIdView.IsVisible = false;
            ConfirmCanIdView.IsVisible = true;
            _pendingCanId = newId.ToString();
            NewCanIdEntry.Text = string.Empty;
        }

        void OnCancelConfirmClicked(object sender, EventArgs e)
        {
            ConfirmCanIdView.IsVisible = false;
            InitialKzvView.IsVisible = true;
        }

        async void OnConfirmClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_pendingCanId)) return;

            int.TryParse(_currentCanId, out var curr);
            var newId = byte.Parse(_pendingCanId);

            // build your single-write frame
            uint canId = (0x18EF0000u)
                       | ((uint)((byte)curr) << 8)
                       | 0x01u;

            var data = new byte[8];
            data[3] = 0x04;
            data[4] = newId;

            _bus.SendFrame(canId, data, extended: true);

            ConfirmCanIdView.IsVisible = false;
            InitialKzvView.IsVisible = true;
        }

        // ─── Connection Status Dialog ───────────────────────────────────────────

        async void OnKzValveStatusClicked(object sender, EventArgs e)
        {
            var msg = _isKzvConnected
                ? "KZ Valve is CONNECTED."
                : "KZ Valve is NOT CONNECTED.";
            await DisplayAlert("Connection Status", msg, "OK");
        }

        async void OnKzValveButtonClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new KZVConnectionStatusPage(_isKzvConnected));

        // ─── Side Menu (unchanged) ───────────────────────────────────────────────

        void OnOshkoshLogoClicked(object sender, EventArgs e)
        {
            SideMenu.IsVisible = true;
            SideMenuDim.IsVisible = true;
            if (SideMenu.Width == 0)
                SideMenu.SizeChanged += SideMenu_SizeChangedAnimateIn;
            else
                _ = AnimateSideMenuIn();
        }

        async void SideMenu_SizeChangedAnimateIn(object sender, EventArgs e)
        {
            if (SideMenu.Width > 0)
            {
                SideMenu.SizeChanged -= SideMenu_SizeChangedAnimateIn;
                await AnimateSideMenuIn();
            }
        }

        async Task AnimateSideMenuIn()
        {
            SideMenu.TranslationX = -SideMenu.Width;
            await SideMenu.TranslateTo(0, 0, 250, Easing.SinOut);
        }

        async void SideMenuOnFirstSizeChanged(object sender, EventArgs e)
        {
            SideMenu.SizeChanged -= SideMenuOnFirstSizeChanged;
            SideMenu.TranslationX = -SideMenu.Width;
            await SideMenu.TranslateTo(0, 0, 250, Easing.SinOut);
            _sideMenuFirstOpen = false;
        }

        async void OnCloseSideMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 250, Easing.SinIn);
            SideMenu.IsVisible = false;
            SideMenuDim.IsVisible = false;
        }

        async void OnMenuClicked(object sender, EventArgs e)
            => await NavigateTo(new Menu(_loc, _bus));

        async void OnAngleSensorMenuClicked(object sender, EventArgs e)
            => await NavigateTo(new BAS(_loc, _bus));

        async void OnKzValveMenuClicked(object sender, EventArgs e)
            => await NavigateTo(new KZV(_loc, _bus));

        async void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
            => await NavigateTo(new FTLS(_loc, _bus));

        Task NavigateTo(Page p)
        {
            SideMenu.IsVisible = SideMenuDim.IsVisible = false;
            return Navigation.PushAsync(p);
        }
    }
#endif
}
