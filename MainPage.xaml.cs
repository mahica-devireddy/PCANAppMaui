using System;
using System.Globalization;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using LocalizationResourceManager.Maui;
using PCANAppM.Services;        // <-- your ICanBusService
using PCANAppM.Resources.Languages;

namespace PCANAppM
{
#if WINDOWS
    public partial class MainPage : ContentPage
    {
        readonly ILocalizationResourceManager _loc;
        readonly ICanBusService               _bus;

        bool _isDeviceConnected;
        bool _sideMenuFirstOpen = true;

        public MainPage(
            ILocalizationResourceManager loc,
            ICanBusService               bus
        )
        {
            InitializeComponent();

            _loc = loc;
            _bus = bus;

            // Subscribe to connection status changes
            _bus.StatusChanged += OnBusStatusChanged;

            // Initial UI update
            UpdateDeviceStatusUI();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _bus.StatusChanged -= OnBusStatusChanged;
        }

        void OnBusStatusChanged()
        {
            MainThread.BeginInvokeOnMainThread(UpdateDeviceStatusUI);
        }

        void UpdateDeviceStatusUI()
        {
            _isDeviceConnected = _bus.IsConnected;

            if (_isDeviceConnected)
            {
                StatusLabel.Text      = $"{_bus.DeviceName}  {_loc["Status2"]}";
                StatusImage1.Source   = "green_check.png";
            }
            else
            {
                StatusLabel.Text      = _loc["Status1"];
                StatusImage1.Source   = "red_ex.png";
            }
        }

        // ─── Your existing handlers below ───────────────────────────────────────

        private async void OnStatusImageClicked(object sender, EventArgs e)
        {
            if (_isDeviceConnected)
                await Navigation.PushAsync(new Menu(_loc, _bus));
            else
            {
                ConnectionDialog.IsVisible = true;
                MainContent.IsVisible      = false;
            }
        }

        private void OnConnectionDialogOkClicked(object sender, EventArgs e)
        {
            ConnectionDialog.IsVisible = false;
            MainContent.IsVisible      = true;
        }

        private void OnLanguageButtonClicked(object sender, EventArgs e)
        {
            LanguageState.CurrentLanguage =
                LanguageState.CurrentLanguage == "en" ? "es" : "en";
            _loc.CurrentCulture =
                new CultureInfo(LanguageState.CurrentLanguage);
        }

        private async void OnNextButtonClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new Menu(_loc, _bus));

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

        private async void OnCloseSideMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 250, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
        }

        private async void OnMenuClicked(object sender, EventArgs e)
        {
            await CloseMenuAnd(async () => await Navigation.PushAsync(new Menu(_loc, _bus)));
        }

        private async void OnAngleSensorMenuClicked(object sender, EventArgs e)
            => await CloseMenuAnd(async () => await Navigation.PushAsync(new BAS(_loc, _bus)));

        private async void OnKzValveMenuClicked(object sender, EventArgs e)
            => await CloseMenuAnd(async () => await Navigation.PushAsync(new KZV(_loc, _bus)));

        private async void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
            => await CloseMenuAnd(async () => await Navigation.PushAsync(new FTLS(_loc, _bus)));

        async Task CloseMenuAnd(Func<Task> nav)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await nav();
        }
    }

    public static class LanguageState
    {
        public static string CurrentLanguage { get; set; } = "en";
        public static CultureInfo CurrentCulture { get; set; } = new CultureInfo("en");
        public static bool IsSpanish => CurrentLanguage == "es";
    }
#endif
}
