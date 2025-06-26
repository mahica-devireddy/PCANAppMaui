using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
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
    public partial class FTLS : ContentPage
    {
        readonly ILocalizationResourceManager _loc;
        readonly ICanBusService               _bus;

        string? _currentCanId;
        string? _pendingCanId;
        bool    _isStarted;
        bool    _isFtlsConnected;
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

            if (!_isStarted)
                DisplayAlert("Error", "No PCAN device found.", "OK");
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _bus.FrameReceived -= OnFrameReceived;
        }

        void OnFrameReceived(PCAN_USB.Packet packet)
        {
            // PGN FF52 = FTLS keep-alive
            uint pgn = (packet.Id >> 8) & 0xFFFF;
            if (pgn == 0xFF52)
            {
                _isFtlsConnected = true;
                ResetConnectionTimeout();
            }

            var idHex = $"0x{packet.Id:X}";
            var lastTwo = idHex.Length >= 2
                ? idHex.Substring(idHex.Length - 2)
                : idHex;

            if (int.TryParse(lastTwo, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int val))
            {
                _currentCanId = val.ToString();
                MainThread.BeginInvokeOnMainThread(() =>
                    LatestCanIdLabel2.Text = $"{_loc["CurrentFTLS"]} {_currentCanId}"
                );
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

        // ─── Set CAN ID Workflow ─────────────────────────────────────────────────

        void OnSetCanIdClicked(object sender, EventArgs e)
        {
            SetCanIdView2.IsVisible = true;
            InitialFtlsView.IsVisible = false;
        }

        async void OnSetClicked(object sender, EventArgs e)
        {
            var text = NewCanIdEntry2.Text?.Trim();
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

            ConfirmText2.Text = $"Set The CAN ID to {newId}";
            SetCanIdView2.IsVisible     = false;
            ConfirmCanIdView2.IsVisible = true;
            _pendingCanId               = newId.ToString();
            NewCanIdEntry2.Text         = string.Empty;
        }

        void OnCancelConfirmClicked1(object sender, EventArgs e)
        {
            ConfirmCanIdView2.IsVisible = false;
            InitialFtlsView.IsVisible   = true;
        }

        async void OnConfirmClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_pendingCanId)) return;

            int.TryParse(_currentCanId, out var curr);
            var newId = byte.Parse(_pendingCanId);

            // Compose the two‐step FTLS protocol
            string hexId = curr.ToString("X2");
            uint   canId = uint.Parse($"0CEF{hexId}02", NumberStyles.HexNumber);

            // Step 1
            var firstData = new byte[]{0x72,0x6F,0x74,0x61,0x2D,0x65,0x6E,0x6A};
            _bus.SendFrame(canId, firstData, extended: true);

            await Task.Delay(100);

            // Step 2
            var secondData = new byte[]{newId};
            _bus.SendFrame(canId, secondData, extended: true);

            ConfirmCanIdView2.IsVisible = false;
            InitialFtlsView.IsVisible   = true;
        }

        // ─── Connection Status Dialog ───────────────────────────────────────────

        async void OnFTLSStatusClicked(object sender, EventArgs e)
        {
            var msg = _isFtlsConnected
                ? "Fluid Tank Level Sensor is CONNECTED."
                : "Fluid Tank Level Sensor is NOT CONNECTED.";
            await DisplayAlert("Connection Status", msg, "OK");
        }

        async void OnFTLSButtonClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new FTLSConnectionStatusPage(_isFtlsConnected));

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

        async void OnCloseSideMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 250, Easing.SinIn);
            SideMenu.IsVisible    = false;
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
