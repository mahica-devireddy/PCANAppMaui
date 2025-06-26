using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using LocalizationResourceManager.Maui;
using PCANAppM.Services;    // ← your singleton service
#if WINDOWS
using PCANAppM.Platforms.Windows;  // ← for PCAN_USB
#endif

namespace PCANAppM
{
#if WINDOWS
    public partial class MainPage : ContentPage
    {
        readonly ILocalizationResourceManager _loc;
        readonly PcanUsbStatusService         _statusService;
        bool                                  _isDeviceConnected;
        bool                                  _sideMenuFirstOpen = true;

        public MainPage(ILocalizationResourceManager localizationResourceManager)
        {
            InitializeComponent();
            _loc           = localizationResourceManager;
            _statusService = PcanUsbStatusService.Instance;

            // live status updates
            _statusService.StatusChanged += OnStatusChanged;
            UpdateDeviceStatusUI();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // ensure we remain subscribed
            _statusService.StatusChanged += OnStatusChanged;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _statusService.StatusChanged -= OnStatusChanged;
        }

        void OnStatusChanged()
        {
            MainThread.BeginInvokeOnMainThread(UpdateDeviceStatusUI);
        }

        void UpdateDeviceStatusUI()
        {
            if (_statusService.IsConnected)
            {
                StatusLabel.Text   = $"{_statusService.DeviceName}  {_loc["Status2"]}";
                StatusImage1.Source = "green_check.png";
                _isDeviceConnected = true;
            }
            else
            {
                StatusLabel.Text   = _loc["Status1"];
                StatusImage1.Source = "red_ex.png";
                _isDeviceConnected = false;
            }
        }

        //  ── Header buttons ──────────────────────────────────────────────────────

        private void OnLanguageButtonClicked(object sender, EventArgs e)
        {
            // toggle en ↔ es
            LanguageState.CurrentLanguage =
                LanguageState.CurrentLanguage == "en" ? "es" : "en";
            _loc.CurrentCulture =
                new CultureInfo(LanguageState.CurrentLanguage);
        }

        private async void OnStatusImageClicked(object sender, EventArgs e)
        {
            if (_isDeviceConnected)
                await Navigation.PushAsync(new Menu(_loc));
            else
            {
                // show the “please plug in” dialog
                ConnectionDialog.IsVisible = true;
                MainContent.IsVisible      = false;
            }
        }

        private void OnConnectionDialogOkClicked(object sender, EventArgs e)
        {
            ConnectionDialog.IsVisible = false;
            MainContent.IsVisible      = true;
        }

        private async void OnNextButtonClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new Menu(_loc));

        //  ── Side-menu show/hide ─────────────────────────────────────────────────

        private void OnOshkoshLogoClicked(object sender, EventArgs e)
        {
            SideMenu.IsVisible    = true;
            SideMenuDim.IsVisible = true;
            if (SideMenu.Width == 0)
                SideMenu.SizeChanged += SideMenu_SizeChangedAnimateIn;
            else
                _ = AnimateSideMenuIn();
        }

        private async void SideMenu_SizeChangedAnimateIn(object sender, EventArgs e)
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

        private async void SideMenuOnFirstSizeChanged(object sender, EventArgs e)
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

        //  ── Side-menu navigation ────────────────────────────────────────────────

        private async Task CloseSideMenu()
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
        }

        private async void OnMenuClicked(object sender, EventArgs e)
        {
            await CloseSideMenu();
            await Navigation.PushAsync(new Menu(_loc));
        }

        private async void OnAngleSensorMenuClicked(object sender, EventArgs e)
        {
            await CloseSideMenu();
            await Navigation.PushAsync(new BAS(_loc));
        }

        private async void OnKzValveMenuClicked(object sender, EventArgs e)
        {
            await CloseSideMenu();
            await Navigation.PushAsync(new KZV(_loc));
        }

        private async void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
        {
            await CloseSideMenu();
            await Navigation.PushAsync(new FTLS(_loc));
        }
    }

    // language helper
    public static class LanguageState
    {
        public static string CurrentLanguage { get; set; } = "en";
        public static CultureInfo CurrentCulture
        {
            get => CultureInfo.CurrentCulture;
            set => CultureInfo.CurrentCulture = value;
        }
    }
#endif
}
