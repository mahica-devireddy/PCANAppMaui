using System;
using System.Collections.Generic;
using System.Timers;
using Microsoft.Maui.Controls;
using Timer = System.Timers.Timer;
using System.Threading.Tasks;
using LocalizationResourceManager.Maui;
using System.Globalization;
using PCANAppM.Resources.Languages;

#if WINDOWS
using PCANAppM.Platforms.Windows;
#endif

namespace PCANAppM
{
    public partial class MainPage : ContentPage
    {
        private readonly ILocalizationResourceManager _localizationResourceManager;
        private Timer deviceCheckTimer;
        private string lastStatus = "";
        private bool isDeviceConnected;

        public MainPage(ILocalizationResourceManager localizationResourceManager)
        {
            _localizationResourceManager = localizationResourceManager;
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateDeviceStatus();
            StartDeviceCheckTimer();
        }

        private void StartDeviceCheckTimer()
        {
            deviceCheckTimer = new Timer(1000);
            deviceCheckTimer.Elapsed += DeviceCheckTimer_Elapsed;
            deviceCheckTimer.AutoReset = true;
            deviceCheckTimer.Start();
        }

        private void DeviceCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            UpdateDeviceStatus();
        }

        private void UpdateDeviceStatus()
        {
#if WINDOWS
            var devices = PCAN_USB.GetUSBDevices();
            string status;
            string imageSource;
            if (devices != null && devices.Count > 0)
            {
                status = devices[0] + "  " + _localizationResourceManager["Status2"];
                imageSource = "green_check.png";
                isDeviceConnected = true;
            }
            else
            {
                status = _localizationResourceManager["Status1"];
                imageSource = "red_ex.png";
                isDeviceConnected = false;
            }

            if (status != lastStatus)
            {
                lastStatus = status;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusLabel.Text = status;
                    StatusImage1.Source = imageSource;
                });
            }
#endif
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
            }
        }

        private void OnConnectionDialogOkClicked(object sender, EventArgs e)
        {
            ConnectionDialog.IsVisible = false;
        }

        private async void OnLanguageButtonClicked(object sender, EventArgs e)
        {
            LanguageState.CurrentLanguage = LanguageState.CurrentLanguage == "en" ? "es" : "en";
            _localizationResourceManager.CurrentCulture = new CultureInfo(LanguageState.CurrentLanguage);
            //UpdateDeviceStatus();
        }

        private async void OnNextButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Menu(_localizationResourceManager));
        }

        private void StatusImage1_Clicked(object sender, EventArgs e)
        {

        }
    }

    public static class LanguageState
    {
        public static string CurrentLanguage { get; set; } = "en";
        public static bool IsSpanish => CurrentLanguage == "es";
    }
}
