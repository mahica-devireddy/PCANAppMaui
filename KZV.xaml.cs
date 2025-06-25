using LocalizationResourceManager.Maui;
using Peak.Can.Basic;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Maui.Controls;
using PCANAppM.Services;

namespace PCANAppM
{
    public partial class KZV : ContentPage
    {
        private string? _currentCanId = null;
        private string? _pendingNewCanId = null;
        private readonly ILocalizationResourceManager _localizationResourceManager;
        private readonly ICanBusService             _canBusService;
        private Timer?                               _connectionTimeoutTimer;
        private bool                                 _sideMenuFirstOpen = true;

        public KZV(
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
            // Subscribe to receive frames
            _canBusService.FrameReceived += OnCanMessageReceived;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Unsubscribe to avoid leaks
            _canBusService.FrameReceived -= OnCanMessageReceived;
        }

        private void OnCanMessageReceived(PCAN_USB.Packet packet)
        {
            uint pgn = (packet.Id >> 8) & 0xFFFF;
            if (pgn == 0xFECA)
            {
                KZVConnectionState.IsConnected = true;
                ResetConnectionTimeout();
            }

            var idHex = $"0x{packet.Id:X}";
            string lastTwo = idHex.Length >= 2
                ? idHex.Substring(idHex.Length - 2)
                : idHex;

            if (int.TryParse(lastTwo, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int canIdInt))
            {
                _currentCanId = canIdInt.ToString();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateLatestCanIdLabel(_currentCanId);
                });
            }
        }

        private void ResetConnectionTimeout()
        {
            _connectionTimeoutTimer?.Stop();
            _connectionTimeoutTimer = new Timer(2000) { AutoReset = false };
            _connectionTimeoutTimer.Elapsed += (s, e) =>
            {
                KZVConnectionState.IsConnected = false;
                _connectionTimeoutTimer?.Stop();
            };
            _connectionTimeoutTimer.Start();
        }

        private async void OnSetCanIdClicked(object sender, EventArgs e)
        {
            SetCanIdView.IsVisible = true;
            InitialKzvView.IsVisible = false;
        }

        private async void OnSetClicked(object sender, EventArgs e)
        {
            var newCanId = NewCanIdEntry.Text?.Trim();
            if (string.IsNullOrEmpty(newCanId)
                || !int.TryParse(newCanId, out int newCanIdInt)
                || newCanIdInt < 0
                || newCanIdInt > 255)
            {
                await DisplayAlert("Invalid Input",
                    "Please enter a valid CAN ID value between 0-255.",
                    "OK");
                return;
            }

            ConfirmText.Text = $"Set The CAN ID to {newCanIdInt}";
            SetCanIdView.IsVisible     = false;
            ConfirmCanIdView.IsVisible = true;
            _pendingNewCanId           = newCanIdInt.ToString();
            NewCanIdEntry.Text         = string.Empty;
        }

        private async void OnConfirmClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_pendingNewCanId))
                return;

            int.TryParse(_currentCanId, out int currentIdInt);
            int newIdInt = int.Parse(_pendingNewCanId);

            byte currentIdByte = (byte)currentIdInt;
            byte newIdByte     = (byte)newIdInt;

            uint canId = (0x18EF0000u)
                       | ((uint)currentIdByte << 8)
                       | 0x01u;

            byte[] data = new byte[8];
            data[3] = 0x04;
            data[4] = newIdByte;

            // Send via the shared service
            _canBusService.SendFrame(canId, data, canId > 0x7FF);

            ConfirmCanIdView.IsVisible = false;
            InitialKzvView.IsVisible   = true;
        }

        private void OnCancelConfirmClicked(object sender, EventArgs e)
        {
            ConfirmCanIdView.IsVisible = false;
            InitialKzvView.IsVisible   = true;
        }

        private void OnExitClicked(object sender, EventArgs e)
        {
            SetCanIdView.IsVisible     = false;
            InitialKzvView.IsVisible   = true;
            ConfirmCanIdView.IsVisible = false;
        }

        private void UpdateLatestCanIdLabel(string id)
        {
            LatestCanIdLabel.Text = $"{_localizationResourceManager["CurrentKZV"]} {id}";
        }

        private async void OnKZVClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(
                new KZVConnectionStatusPage(KZVConnectionState.IsConnected)
            );
        }

        private async Task ShowKZVConnectionStatusAsync()
        {
            string message = KZVConnectionState.IsConnected
                ? "KZ Valve is CONNECTED."
                : "KZ Valve is NOT CONNECTED.";
            await DisplayAlert("KZ Valve Connection", message, "OK");
        }

        private async void OnCheckConnectionClicked(object sender, EventArgs e)
        {
            bool isConnected = KZVConnectionState.IsConnected;
            string message = isConnected
                ? "KZ Valve is CONNECTED."
                : "KZ Valve is NOT CONNECTED.";
            await DisplayAlert("Connection Status", message, "OK");
        }

        private void NewCanIdEntry_Focused(object sender, FocusEventArgs e) { }

        private async void OnLanguageButtonClicked(object sender, EventArgs e)
        {
            LanguageState.CurrentLanguage =
                LanguageState.CurrentLanguage == "en" ? "es" : "en";
            _localizationResourceManager.CurrentCulture =
                new CultureInfo(LanguageState.CurrentLanguage);
        }

        private async void OnOshkoshLogoClicked(object sender, EventArgs e)
        {
            SideMenu.IsVisible    = true;
            SideMenuDim.IsVisible = true;

            if (SideMenu.Width == 0)
                SideMenu.SizeChanged += SideMenu_SizeChangedAnimateIn;
            else
                await AnimateSideMenuIn();
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
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(
                new Menu(_localizationResourceManager, _canBusService)
            );
        }

        private async void OnAngleSensorMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(
                new BAS(_localizationResourceManager, _canBusService)
            );
        }

        private async void OnKzValveMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(
                new KZV(_localizationResourceManager, _canBusService)
            );
        }

        private async void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(
                new FTLS(_localizationResourceManager, _canBusService)
            );
        }
    }
}
