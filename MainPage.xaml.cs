using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using LocalizationResourceManager.Maui;
using System.Globalization;
using PCANAppM.Resources.Languages;
using PCANAppM;

#if WINDOWS
using PCANAppM.Platforms.Windows;
using PCANAppM.Services;
#endif

namespace PCANAppM
{
#if WINDOWS

    public partial class MainPage : ContentPage
    {
        readonly ILocalizationResourceManager _localizationResourceManager;
        readonly PcanUsbStatusService _statusService;
        bool isDeviceConnected;

        private bool _sideMenuFirstOpen = true;
        private bool _isLanguageGlowing = false;
        private bool _isPointerOverLanguageButton = false;

        public MainPage(ILocalizationResourceManager localizationResourceManager)
        {
            _localizationResourceManager = localizationResourceManager;
            _statusService = PcanUsbStatusService.Instance;
            InitializeComponent();
            _statusService.StatusChanged += OnStatusChanged;
            UpdateDeviceStatusUI();

        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _statusService.StatusChanged -= OnStatusChanged;
        }

        private void OnStatusChanged()
        {
            MainThread.BeginInvokeOnMainThread(UpdateDeviceStatusUI);
        }

        private void UpdateDeviceStatusUI()
        {
            if (_statusService.IsConnected)
            {
                StatusLabel.Text = $"{_statusService.DeviceName}  {_localizationResourceManager["Status2"]}";
                StatusImage1.Source = "green_check.png";
                isDeviceConnected = true;
            }
            else
            {
                StatusLabel.Text = _localizationResourceManager["Status1"];
                StatusImage1.Source = "red_ex.png";
                isDeviceConnected = false;
            }
        }

        private async void OnStatusImageClicked(object sender, EventArgs e)
        {
            if (isDeviceConnected)
            {
                await Navigation.PushAsync(new Menu(_localizationResourceManager));
            }
            else
            {
                ConnectionDialog.IsVisible = true;
                MainContent.IsVisible = false; // Hide all except header and dialog
            }
        }

        private void OnConnectionDialogOkClicked(object sender, EventArgs e)
        {
            MainContent.IsVisible = true; // Show main content again
            ConnectionDialog.IsVisible = false;
        }

        private void OnLanguageButtonClicked(object sender, EventArgs e)
        {
            LanguageState.CurrentLanguage = LanguageState.CurrentLanguage == "en" ? "es" : "en";
            _localizationResourceManager.CurrentCulture = new CultureInfo(LanguageState.CurrentLanguage);
        }

        private async void OnNextButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Menu(_localizationResourceManager));
        }

        private void StatusImage1_Clicked(object sender, EventArgs e)
        {

        }

        private void OnOshkoshLogoClicked(object sender, EventArgs e)
        {
            SideMenu.IsVisible = true;
            SideMenuDim.IsVisible = true;

            if (SideMenu.Width == 0)
            {
                // Wait for the menu to be measured, then animate
                SideMenu.SizeChanged += SideMenu_SizeChangedAnimateIn;
            }
            else
            {
                AnimateSideMenuIn();
            }
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
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 250, Easing.SinIn); // Slide out
            SideMenu.IsVisible = false;
            SideMenuDim.IsVisible = false;
        }

        private async void OnMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new Menu(_localizationResourceManager));
        }

        private async void OnAngleSensorMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new BAS(_localizationResourceManager));
        }

        private async void OnKzValveMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new KZV(_localizationResourceManager));
        }

        private async void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new FTLS(_localizationResourceManager));
        }
    }

    public static class LanguageState
    {
        public static string CurrentLanguage { get; set; } = "en";
        public static bool IsSpanish => CurrentLanguage == "es";
    }
#endif
}

using Microsoft.Maui.Controls;
using LocalizationResourceManager.Maui;
using PCANAppM.Services;
using System;

namespace PCANAppM
{
#if WINDOWS
    public partial class MainPage : ContentPage
    {
        readonly ILocalizationResourceManager _loc;
        readonly CanBusService               _bus;
        bool                                 _isDeviceConnected;

        public MainPage(
            ILocalizationResourceManager loc,
            CanBusService               bus
        )
        {
            InitializeComponent();
            _loc = loc;
            _bus = bus;

            // live updates:
            _bus.StatusChanged += OnStatusChanged;
            UpdateUI();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _bus.StatusChanged -= OnStatusChanged;
        }

        void OnStatusChanged() =>
            Microsoft.Maui.ApplicationModel.MainThread
              .BeginInvokeOnMainThread(UpdateUI);

        void UpdateUI()
        {
            bool c = _bus.IsConnected;
            StatusLabel.Text   = c
                ? $"{_bus.DeviceName}  {_loc["Status2"]}"
                : _loc["Status1"];
            StatusImage1.Source = c ? "green_check.png" : "red_ex.png";
            _isDeviceConnected  = c;
        }

        async void OnStatusImageClicked(object sender, EventArgs e)
        {
            if (_isDeviceConnected)
                await Navigation.PushAsync(new Menu(_loc, _bus));
            else
            {
                ConnectionDialog.IsVisible = true;
                MainContent.IsVisible      = false;
            }
        }

        void OnConnectionDialogOkClicked(object sender, EventArgs e)
        {
            ConnectionDialog.IsVisible = false;
            MainContent.IsVisible      = true;
        }

        // … rest of your side‐menu handlers …
    }
#endif
}
