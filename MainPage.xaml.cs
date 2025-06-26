using System;
using System.ComponentModel;
using System.Globalization;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using LocalizationResourceManager.Maui;
using PCANAppM.Services;  // ← your single service namespace

namespace PCANAppM
{
#if WINDOWS
    public partial class MainPage : ContentPage
    {
        readonly ILocalizationResourceManager _loc;
        readonly ICanBusService               _bus;
        bool                                  _isDeviceConnected;
        string                                _lastStatus = "";

        public MainPage(
            ILocalizationResourceManager loc,
            ICanBusService               bus
        )
        {
            InitializeComponent();

            _loc = loc;
            _bus = bus;

            // Subscribe to live changes in IsConnected
            if (_bus is INotifyPropertyChanged npc)
                npc.PropertyChanged += OnBusPropertyChanged;

            // Set the initial UI state
            UpdateDeviceStatusUI();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Unsubscribe to avoid leaks
            if (_bus is INotifyPropertyChanged npc)
                npc.PropertyChanged -= OnBusPropertyChanged;
        }

        void OnBusPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ICanBusService.IsConnected))
                MainThread.BeginInvokeOnMainThread(UpdateDeviceStatusUI);
        }

        void UpdateDeviceStatusUI()
        {
            bool connected = _bus.IsConnected;
            string status = connected
                ? $"{_bus.DeviceName}  {_loc["Status2"]}"
                : _loc["Status1"];
            string icon = connected ? "green_check.png" : "red_ex.png";

            // only update if the text actually changed
            if (status != _lastStatus)
            {
                _lastStatus = status;
                StatusLabel.Text    = status;
                StatusImage1.Source = icon;
                _isDeviceConnected  = connected;
            }
        }

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

        // ─── your existing side-menu handlers below ───

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

        private Task AnimateSideMenuIn() =>
            SideMenu.TranslateTo(0, 0, 250, Easing.SinOut);

        private async void SideMenuOnFirstSizeChanged(object? sender, EventArgs e)
        {
            SideMenu.SizeChanged -= SideMenuOnFirstSizeChanged;
            SideMenu.TranslationX = -SideMenu.Width;
            await AnimateSideMenuIn();
        }

        private async void OnCloseSideMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 250, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
        }

        private async void OnMenuClicked(object sender, EventArgs e)
        {
            await AnimateSideMenuIn();
            await Navigation.PushAsync(new Menu(_loc, _bus));
        }
        private async void OnAngleSensorMenuClicked(object sender, EventArgs e)
        {
            await AnimateSideMenuIn();
            await Navigation.PushAsync(new BAS(_loc, _bus));
        }
        private async void OnKzValveMenuClicked(object sender, EventArgs e)
        {
            await AnimateSideMenuIn();
            await Navigation.PushAsync(new KZV(_loc, _bus));
        }
        private async void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
        {
            await AnimateSideMenuIn();
            await Navigation.PushAsync(new FTLS(_loc, _bus));
        }
    }
#endif
}

using LocalizationResourceManager.Maui;
using PCANAppM.Services;
using Microsoft.Maui.Controls;
using System.Globalization;

namespace PCANAppM
{
#if WINDOWS
    public partial class MainPage : ContentPage
    {
        readonly ILocalizationResourceManager _loc;
        readonly ICanBusService _bus;
        bool _haveDevice;

        public MainPage(ILocalizationResourceManager loc, ICanBusService bus)
        {
            InitializeComponent();
            _loc = loc;
            _bus = bus;
            _bus.PropertyChanged += OnBusChanged;
            UpdateUI();
        }

        void OnBusChanged(object s, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ICanBusService.IsConnected))
                MainThread.BeginInvokeOnMainThread(UpdateUI);
        }

        void UpdateUI()
        {
            _haveDevice = _bus.IsConnected;
            if (_haveDevice)
            {
                StatusLabel.Text   = $"{_bus.DeviceName} {_loc["Status2"]}";
                StatusImage1.Source = "green_check.png";
            }
            else
            {
                StatusLabel.Text   = _loc["Status1"];
                StatusImage1.Source = "red_ex.png";
            }
        }

        async void OnStatusImageClicked(object _, EventArgs __)
        {
            if (_haveDevice)
                await Navigation.PushAsync(new Menu(_loc));
            else
            {
                ConnectionDialog.IsVisible = true;
                MainContent.IsVisible = false;
            }
        }

        void OnConnectionDialogOkClicked(object _, EventArgs __)
        {
            ConnectionDialog.IsVisible = false;
            MainContent.IsVisible = true;
        }

        void OnLanguageButtonClicked(object _, EventArgs __)
        {
            LanguageState.CurrentLanguage = LanguageState.CurrentLanguage == "en" ? "es" : "en";
            _loc.CurrentCulture = new CultureInfo(LanguageState.CurrentLanguage);
        }

        // … your side‐menu handlers (OnOshkoshLogoClicked, OnMenuClicked, etc.) stay exactly as you had them …
    }

    public static class LanguageState
    {
        public static string CurrentLanguage { get; set; } = "en";
    }
#endif
}

