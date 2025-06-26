using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using LocalizationResourceManager.Maui;
using PCANAppM.Resources.Languages;
using PCANAppM.Services;  // ← your global PCAN USB service

namespace PCANAppM
{
#if WINDOWS
    public partial class MainPage : ContentPage
    {
        readonly ILocalizationResourceManager _localizationResourceManager;
        readonly PcanUsbStatusService         _statusService;
        bool                                  _isDeviceConnected;

        private bool _sideMenuFirstOpen = true;

        public MainPage(
            ILocalizationResourceManager localizationResourceManager
        )
        {
            InitializeComponent();

            _localizationResourceManager = localizationResourceManager;
            _statusService               = PcanUsbStatusService.Instance;

            // Subscribe once to get live updates
            _statusService.StatusChanged += OnStatusChanged;

            // Initial state
            UpdateDeviceStatusUI();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Clean up
            _statusService.StatusChanged -= OnStatusChanged;
        }

        void OnStatusChanged()
        {
            // Always run on UI thread
            MainThread.BeginInvokeOnMainThread(UpdateDeviceStatusUI);
        }

        void UpdateDeviceStatusUI()
        {
            var connected = _statusService.IsConnected;
            StatusLabel.Text   = connected
                ? $"{_statusService.DeviceName}  {_localizationResourceManager["Status2"]}"
                : _localizationResourceManager["Status1"];
            StatusImage1.Source = connected ? "green_check.png" : "red_ex.png";
            _isDeviceConnected  = connected;
        }

        private async void OnStatusImageClicked(object sender, EventArgs e)
        {
            if (_isDeviceConnected)
            {
                // Pass the same service into Menu
                await Navigation.PushAsync(
                    new Menu(_localizationResourceManager, _statusService)
                );
            }
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
        {
            await Navigation.PushAsync(
                new Menu(_localizationResourceManager, _statusService)
            );
        }

        // ─── Side‐menu handlers ────────────────────────────────────────────────────

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
            await Navigation.PushAsync(
                new Menu(_localizationResourceManager, _statusService)
            );
        }

        private async void OnAngleSensorMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(
                new BAS(_localizationResourceManager, _statusService)
            );
        }

        private async void OnKzValveMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(
                new KZV(_localizationResourceManager, _statusService)
            );
        }

        private async void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(
                new FTLS(_localizationResourceManager, _statusService)
            );
        }
    }

    public static class LanguageState
    {
        public static string CurrentLanguage { get; set; } = "en";
        public static bool   IsSpanish      => CurrentLanguage == "es";
    }
#endif
}
