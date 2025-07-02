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
}
#if WINDOWS
using System;
using System.Collections.Generic;
using System.Timers;
using Microsoft.Maui.Controls;
using Timer = System.Timers.Timer;
using System.Threading.Tasks;
using LocalizationResourceManager.Maui;
using System.Globalization;
using PCANAppM.Resources.Languages;
using PCANAppM.Services;
using PCANAppM.Platforms.Windows;

namespace PCANAppM
{
    public partial class MainPage : ContentPage
    {
        private readonly ILocalizationResourceManager _loc;
        private readonly CanBusService _bus;
        private int _previousDeviceCount = 0; // Track previous device count

        public MainPage(ILocalizationResourceManager localizationResourceManager, CanBusService canBusService)
        {
            _loc = localizationResourceManager;
            _bus = canBusService;
            InitializeComponent();

            // Subscribe to service events
            _bus.DeviceListChanged += OnDeviceListChanged;
            _bus.Feedback += OnFeedback;
            _bus.ErrorPrompt += OnErrorPrompt;
            _bus.LoggingStarted += OnLoggingStarted;
            _bus.LoggingStopped += OnLoggingStopped;
            _bus.MessageReceived += OnMessageReceived;

            UpdateDeviceList();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateDeviceList();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _bus.DeviceListChanged -= OnDeviceListChanged;
            _bus.Feedback -= OnFeedback;
            _bus.ErrorPrompt -= OnErrorPrompt;
            _bus.LoggingStarted -= OnLoggingStarted;
            _bus.LoggingStopped -= OnLoggingStopped;
            _bus.MessageReceived -= OnMessageReceived;
        }

        private void OnDeviceListChanged()
        {
            MainThread.BeginInvokeOnMainThread(UpdateDeviceList);
        }

        private void UpdateDeviceList()
        {
            var devices = _bus.AvailableDevices;
            if (this.FindByName<ListView>("DeviceListView") is ListView deviceListView)
                deviceListView.ItemsSource = devices;

            string status;
            string imageSource;

            // Device plugged in (transition from 0 to >0)
            if (_previousDeviceCount == 0 && devices.Count > 0)
            {
                status = (devices[0] ?? "PCAN USB") + " " + _loc["Status2"];
                imageSource = "green_check.png";
            }
            // Device unplugged (transition from >0 to 0)
            else if (_previousDeviceCount > 0 && devices.Count == 0)
            {
                status = _loc["Status1"];
                imageSource = "red_ex.png";
            }
            // No transition, just update to current state
            else if (devices.Count > 0)
            {
                status = (devices[0] ?? "PCAN USB") + " " + _loc["Status2"];
                imageSource = "green_check.png";
            }
            else
            {
                status = _loc["Status1"];
                imageSource = "red_ex.png";
            }

            _previousDeviceCount = devices.Count;

            if (this.FindByName<Label>("StatusLabel") is Label statusLabel)
                statusLabel.Text = status;
            if (this.FindByName<ImageButton>("StatusImage") is ImageButton StatusImage)
                StatusImage.Source = imageSource;
        }

        private void OnFeedback(string message)
        {
            // Optionally display feedback to the user (e.g., in a status bar or toast)
            // DisplayAlert("Feedback", message, "OK"); // or use a Snackbar, etc.
        }

        private void OnErrorPrompt(string message)
        {
            //MainThread.BeginInvokeOnMainThread(async () =>
            //{
            //    //await DisplayAlert("Device Error", message, "OK");
            //});
        }

        private void OnLoggingStarted()
        {
            // Optionally update UI to reflect logging started
        }

        private void OnLoggingStopped()
        {
            // Optionally update UI to reflect logging stopped
        }

        private void OnMessageReceived(PCAN_USB.Packet packet)
        {
            // Optionally update UI with new CAN message
        }

        private void OnDeviceSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is string deviceName)
            {
                bool success = _bus.Initialize(deviceName, "250 kbit/s", enableRead: false);
                UpdateDeviceList();
                if (!success)
                {
                    DisplayAlert("Error", "Failed to initialize device.", "OK");
                }
            }
        }

        private async void OnStatusImageClicked(object sender, EventArgs e)
        {
            var devices = _bus.AvailableDevices;
            if (devices.Count > 0)
            {
                // Device is connected, navigate to Menu
                await Navigation.PushAsync(new Menu(_loc, _bus));
            }
            else
            {
                // Device is not connected, show connection dialog
                ConnectionDialog.IsVisible = true;
                MainContent.IsVisible = false;
            }
        }

        private void OnConnectionDialogOkClicked(object sender, EventArgs e)
        {
            MainContent.IsVisible = true;
            ConnectionDialog.IsVisible = false;
        }

        private void OnOshkoshLogoClicked(object sender, EventArgs e) { /* … */ }
        private void OnLanguageButtonClicked(object sender, EventArgs e) { /* … */ }
        private void OnMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnAngleSensorMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnKzValveMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnFluidTankLevelMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnCloseSideMenuClicked(object sender, EventArgs e)
        {
            SideMenu.IsVisible = false;
            SideMenuDim.IsVisible = false;
            UpdateDeviceList();
        }
    }
}
#endif
