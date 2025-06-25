using System;
using System.Collections.Generic;
using System.Timers;
using Microsoft.Maui.Controls;
using Timer = System.Timers.Timer;
using System.Threading.Tasks;
using LocalizationResourceManager.Maui;
using System.Globalization;
using PCANAppM.Resources.Languages;
using PCANAppM;


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
        private bool _sideMenuFirstOpen = true;
        private bool _isLanguageGlowing = false;
        private bool _isPointerOverLanguageButton = false;

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

//         private void UpdateDeviceStatus()
//         {
// #if WINDOWS
//             var devices = PCAN_USB.GetUSBDevices();
//             string status;
//             string imageSource;
//             if (devices != null && devices.Count > 0)
//             {
//                 status = devices[0] + "  " + _localizationResourceManager["Status2"];
//                 imageSource = "green_check.png";
//                 isDeviceConnected = true;
//             }
//             else
//             {
//                 status = _localizationResourceManager["Status1"];
//                 imageSource = "red_ex.png";
//                 isDeviceConnected = false;
//             }

//             if (status != lastStatus)
//             {
//                 lastStatus = status;
//                 MainThread.BeginInvokeOnMainThread(() =>
//                 {
//                     StatusLabel.Text = status;
//                     StatusImage1.Source = imageSource;
//                 });
//             }
// #endif
//         }
        private void UpdateDeviceStatus()
        {
            // 1) Poll the physical USB device list
            var devices = PCAN_USB.GetUSBDevices();
            bool physicallyPresent = devices.Count > 0;
        
            // 2) If it just got plugged in, (re)initialize the shared PCAN service
            if (physicallyPresent && !PCanService.IsStarted)
            {
                PCanService.TryInitialize();
            }
        
            // 3) Decide “connected” from the service state, not just the enumeration
            bool connected = PCanService.IsStarted;
        
            // 4) Build status text + icon
            string status, imageSource;
            if (connected)
            {
                status       = $"{devices[0]}  {_localizationResourceManager["Status2"]}";
                imageSource  = "green_check.png";
                _isDeviceConnected = true;
            }
            else
            {
                status       = _localizationResourceManager["Status1"];
                imageSource  = "red_ex.png";
                _isDeviceConnected = false;
            }
        
            // 5) Only update the UI when the status text really changes
            if (status != _lastStatus)
            {
                _lastStatus = status;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusLabel.Text    = status;
                    StatusImage1.Source = imageSource;
                });
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
}
