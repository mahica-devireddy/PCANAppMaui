using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;            // for MainThread
using LocalizationResourceManager.Maui;
using System.Globalization;
using PCANAppM.Resources.Languages;
using PCANAppM.Services;

namespace PCANAppM
{
#if WINDOWS
    public partial class MainPage : ContentPage
    {
        readonly ILocalizationResourceManager _localizationResourceManager;
        bool _isDeviceConnected;
        bool _sideMenuFirstOpen    = true;
        bool _isLanguageGlowing     = false;
        bool _isPointerOverLangBtn  = false;
        string _lastStatus          = "";

        public MainPage(ILocalizationResourceManager localizationResourceManager)
        {
            _localizationResourceManager = localizationResourceManager;
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Hook the debounced service event
            PcanUsbStatusService.Instance.StatusChanged += OnStatusChanged;
            // Seed the UI
            UpdateDeviceStatus();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            PcanUsbStatusService.Instance.StatusChanged -= OnStatusChanged;
        }

        void OnStatusChanged()
        {
            // Ensure we’re on UI thread
            MainThread.BeginInvokeOnMainThread(UpdateDeviceStatus);
        }

        void UpdateDeviceStatus()
        {
            var svc       = PcanUsbStatusService.Instance;
            bool connected = svc.IsConnected;
            _isDeviceConnected = connected;

            string status = connected
                ? $"{svc.DeviceName}  {_localizationResourceManager["Status2"]}"
                : _localizationResourceManager["Status1"];
            string icon   = connected ? "green_check.png" : "red_ex.png";

            // Only update if something actually changed
            if (status != _lastStatus)
            {
                _lastStatus = status;
                StatusLabel.Text    = status;
                StatusImage1.Source = icon;
            }
        }

        private async void OnStatusImageClicked(object sender, EventArgs e)
        {
            if (_isDeviceConnected)
                await Navigation.PushAsync(new Menu(_localizationResourceManager));
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
            _localizationResourceManager.CurrentCulture =
                new CultureInfo(LanguageState.CurrentLanguage);
        }

        private async void OnNextButtonClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new Menu(_localizationResourceManager));

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
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
        }

        private async void OnMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new Menu(_localizationResourceManager));
        }

        private async void OnAngleSensorMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new BAS(_localizationResourceManager));
        }

        private async void OnKzValveMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new KZV(_localizationResourceManager));
        }

        private async void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new FTLS(_localizationResourceManager));
        }
    }

    public static class LanguageState
    {
        public static string CurrentLanguage { get; set; } = "en";
        public static bool   IsSpanish       => CurrentLanguage == "es";
    }
#endif
}
