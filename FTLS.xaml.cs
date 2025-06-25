using LocalizationResourceManager.Maui;
using Peak.Can.Basic;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Maui.Controls;
using PCANAppM.Services;
using PCANAppM.Resources.Languages;

namespace PCANAppM
{
    public partial class FTLS : ContentPage
    {
        private string? _currentCanId;
        private string? _pendingNewCanId;
        private readonly ILocalizationResourceManager _localizationResourceManager;
        private readonly ICanBusService             _canBusService;
        private Timer?                               _connectionTimeoutTimer;

        public FTLS(
            ILocalizationResourceManager localizationResourceManager,
            ICanBusService               canBusService
        )
        {
            _localizationResourceManager = localizationResourceManager;
            _canBusService               = canBusService;
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _canBusService.FrameReceived += OnCanMessageReceived;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _canBusService.FrameReceived -= OnCanMessageReceived;
        }

        private void OnCanMessageReceived(PCAN_USB.Packet packet)
        {
            uint pgn = (packet.Id >> 8) & 0xFFFF;
            if (pgn == 0xFF52)
            {
                FTLSConnectionState.IsConnected = true;
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
                    UpdateLatestCanIdLabel(_currentCanId));
            }
        }

        private void ResetConnectionTimeout()
        {
            _connectionTimeoutTimer?.Stop();
            _connectionTimeoutTimer = new Timer(2000) { AutoReset = false };
            _connectionTimeoutTimer.Elapsed += (s, e) =>
            {
                FTLSConnectionState.IsConnected = false;
                _connectionTimeoutTimer?.Stop();
            };
            _connectionTimeoutTimer.Start();
        }

        private async void OnSetCanIdClicked(object sender, EventArgs e)
        {
            SetCanIdView2.IsVisible    = true;
            InitialFtlsView.IsVisible  = false;
        }

        private async void OnSetClicked(object sender, EventArgs e)
        {
            var newCanIdText = NewCanIdEntry2.Text?.Trim();
            if (string.IsNullOrEmpty(newCanIdText)
                || !int.TryParse(newCanIdText, out var newCanIdInt)
                || newCanIdInt < 0
                || newCanIdInt > 255)
            {
                await DisplayAlert(
                    "Invalid Input",
                    "Please enter a valid CAN ID (0-255).",
                    "OK"
                );
                return;
            }

            ConfirmText2.Text           = $"Set The CAN ID to {newCanIdInt}";
            SetCanIdView2.IsVisible     = false;
            ConfirmCanIdView2.IsVisible = true;
            _pendingNewCanId            = newCanIdInt.ToString();
            NewCanIdEntry2.Text         = string.Empty;
        }

        private async void OnConfirmClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_pendingNewCanId))
                return;

            int.TryParse(_currentCanId, out var currentIdInt);
            var newIdInt = int.Parse(_pendingNewCanId);

            var currentIdHex = currentIdInt.ToString("X2");
            var newIdHex     = newIdInt.ToString("X2");

            // First message
            _canBusService.SendFrame(
                uint.Parse($"0CEF{currentIdHex}02", NumberStyles.HexNumber),
                new byte[] { 0x72, 0x6F, 0x74, 0x61, 0x2D, 0x65, 0x6E, 0x6A },
                extended: true
            );

            await Task.Delay(100);

            // Second message
            _canBusService.SendFrame(
                uint.Parse($"0CEF{currentIdHex}02", NumberStyles.HexNumber),
                new byte[] { Convert.ToByte(newIdHex, 16) },
                extended: true
            );

            ConfirmCanIdView2.IsVisible = false;
            InitialFtlsView.IsVisible   = true;
        }

        private void OnCancelConfirmClicked(object sender, EventArgs e)
        {
            ConfirmCanIdView2.IsVisible = false;
            InitialFtlsView.IsVisible   = true;
        }

        private void OnExitClicked(object sender, EventArgs e)
        {
            SetCanIdView2.IsVisible      = false;
            InitialFtlsView.IsVisible    = true;
            ConfirmCanIdView2.IsVisible  = false;
        }

        private void UpdateLatestCanIdLabel(string id)
        {
            LatestCanIdLabel2.Text = $"{_localizationResourceManager["CurrentFTLS"]} {id}";
        }

        private async void OnFTLSStatusClicked(object sender, EventArgs e)
        {
            await Task.Run(async () =>
            {
                var message = FTLSConnectionState.IsConnected
                    ? "Fluid Tank Level Sensor is CONNECTED."
                    : "Fluid Tank Level Sensor is NOT CONNECTED.";
                await DisplayAlert("Fluid Tank Level Sensor Connection", message, "OK");
            });
        }

        private async void OnFTLSButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(
                new FTLSConnectionStatusPage(FTLSConnectionState.IsConnected)
            );
        }

        private async void OnCheckConnectionClicked(object sender, EventArgs e)
        {
            var message = FTLSConnectionState.IsConnected
                ? "Fluid Tank Level Sensor is CONNECTED."
                : "Fluid Tank Level Sensor is NOT CONNECTED.";
            await DisplayAlert("Connection Status", message, "OK");
        }

        private async void OnMenuClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(
                new Menu(_localizationResourceManager, _canBusService)
            );
        }

        private async void OnAngleSensorMenuClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(
                new BAS(_localizationResourceManager, _canBusService)
            );
        }

        private async void OnKzValveMenuClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(
                new KZV(_localizationResourceManager, _canBusService)
            );
        }

        private async void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(
                new FTLS(_localizationResourceManager, _canBusService)
            );
        }

        private async void OnLanguageButtonClicked(object sender, EventArgs e)
        {
            LanguageState.CurrentLanguage =
                LanguageState.CurrentLanguage == "en" ? "es" : "en";
            _localizationResourceManager.CurrentCulture =
                new CultureInfo(LanguageState.CurrentLanguage);
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

        private async void OnCloseSideMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 250, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
        }
    }

    public class CanMessageViewModel2
    {
        public string Direction { get; set; } = "";
        public string Id        { get; set; } = "";
        public string Data      { get; set; } = "";
    }
}
