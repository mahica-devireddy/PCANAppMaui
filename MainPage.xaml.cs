using System;
using System.Timers;
using Microsoft.Maui.Controls;
using Timer = System.Timers.Timer;
using LocalizationResourceManager.Maui;
using System.Globalization;
using PCANAppM.Resources.Languages;
using PCANAppM.Services;

namespace PCANAppM
{
    public partial class MainPage : ContentPage
    {
        private readonly ILocalizationResourceManager _localizationResourceManager;
        private readonly ICanBusService             _canBusService;
        private Timer                                _deviceCheckTimer;
        private string                               _lastStatus = "";
        private bool                                 _isDeviceConnected;
        private bool                                 _sideMenuFirstOpen = true;
        private bool                                 _isLanguageGlowing = false;
        private bool                                 _isPointerOverLanguageButton = false;

        public MainPage(
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
            StartDeviceCheckTimer();
        }

        private void StartDeviceCheckTimer()
        {
            _deviceCheckTimer = new Timer(500);
            _deviceCheckTimer.Elapsed += (_, _) => UpdateDeviceStatus();
            _deviceCheckTimer.AutoReset = true;
            _deviceCheckTimer.Start();
        }

        private void UpdateDeviceStatus()
        {
            bool connected = _canBusService.IsConnected;
            string status = connected
                ? $"{_canBusService.DeviceName}  {_localizationResourceManager["Status2"]}"
                : _localizationResourceManager["Status1"];
            string imageSource = connected ? "green_check.png" : "red_ex.png";
            _isDeviceConnected = connected;

            if (status != _lastStatus)
            {
                _lastStatus = status;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusLabel.Text   = status;
                    StatusImage1.Source = imageSource;
                });
            }
        }

        private async void OnStatusImageClicked(object sender, EventArgs e)
        {
            if (_isDeviceConnected)
                await Navigation.PushAsync(new Menu(_localizationResourceManager, _canBusService));
            else
            {
                ConnectionDialog.IsVisible = true;
                MainContent.IsVisible      = false;
            }
        }

        private void OnConnectionDialogOkClicked(object sender, EventArgs e)
        {
            MainContent.IsVisible      = true;
            ConnectionDialog.IsVisible = false;
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
            await Navigation.PushAsync(new Menu(_localizationResourceManager, _canBusService));
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

        private async void OnMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new Menu(_localizationResourceManager, _canBusService));
        }

        private async void OnAngleSensorMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new BAS(_localizationResourceManager, _canBusService));
        }

        private async void OnKzValveMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new KZV(_localizationResourceManager, _canBusService));
        }

        private async void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new FTLS(_localizationResourceManager, _canBusService));
        }
    }

    public static class LanguageState
    {
        public static string CurrentLanguage { get; set; } = "en";
        public static bool IsSpanish => CurrentLanguage == "es";
    }
}
