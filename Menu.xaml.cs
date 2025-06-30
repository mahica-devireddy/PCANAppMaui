#if WINDOWS

using LocalizationResourceManager.Maui;
using Microsoft.Maui.Controls;
using PCANAppM.Services;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace PCANAppM
{
    public partial class Menu : ContentPage
    {
        private readonly ILocalizationResourceManager _loc;
        private readonly CanBusService _bus;
        private bool _wasConnected = false;

        public Menu(ILocalizationResourceManager localizationResourceManager, CanBusService canBusService)
        {
            _loc = localizationResourceManager;
            _bus = canBusService;
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _bus.DeviceListChanged += OnDeviceListChanged;
            // Track connection state on entry
            _wasConnected = _bus.AvailableDevices.Count > 0;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _bus.DeviceListChanged -= OnDeviceListChanged;
        }

        private void OnDeviceListChanged()
        {
            var isConnected = _bus.AvailableDevices.Count > 0;
            // Only show dialog if the device was connected and now is disconnected
            if (_wasConnected && !isConnected)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (this.FindByName<ContentView>("MainContent") is ContentView mainContent)
                        mainContent.IsVisible = false;
                    if (this.FindByName<Grid>("PCANAlert") is Grid pcanAlert)
                        pcanAlert.IsVisible = true;
                });
            }
            _wasConnected = isConnected;
        }

        private void OnConnectionDialogOkClicked(object sender, EventArgs e)
        {
            if (this.FindByName<ContentView>("MainContent") is ContentView mainContent)
                mainContent.IsVisible = true;
            if (this.FindByName<Grid>("PCANAlert") is Grid pcanAlert)
                pcanAlert.IsVisible = false;
        }

        // ── Language toggle in header ────────────────────────────────────────────
        private void OnLanguageButtonClicked(object sender, EventArgs e) { /* … */ }
        private void OnOshkoshLogoClicked(object sender, EventArgs e) { /* … */ }


        // ── Main menu buttons ────────────────────────────────────────────────────
        //private async void OnBoomAngleSensorClicked(object sender, EventArgs e)
        //    => await Navigation.PushAsync(new BAS(_loc, _bus));

        private async void OnKZValveClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new KZV(_loc, _bus));

        //private async void OnFluidTankLevelSensorClicked(object sender, EventArgs e)
        //    => await Navigation.PushAsync(new FTLS(_loc, _bus));

        private void OnAngleSensorMenuClicked(object sender, EventArgs e) { /* … */ }
        //private void OnKzValveMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnFluidTankLevelMenuClicked(object sender, EventArgs e) { /* … */ }

        // ── Pointer‐over effects ─────────────────────────────────────────────────
        private void OnPointerEntered(object sender, EventArgs e)
        {
            if (sender is VisualElement elt)
                _ = elt.ScaleTo(1.1, 100);
        }
        private void OnPointerExited(object sender, EventArgs e)
        {
            if (sender is VisualElement elt)
                _ = elt.ScaleTo(1.0, 100);
        }

        // ── Press/release animations ─────────────────────────────────────────────
        private async void MenuButton_Pressed(object sender, EventArgs e)
        {
            if (sender is Button btn)
                await btn.ScaleTo(0.95, 20, Easing.SinIn);
        }
        private async void MenuButton_Released(object sender, EventArgs e)
        {
            if (sender is Button btn)
                await btn.ScaleTo(1, 20, Easing.SinOut);
        }

        // ── “Next” (if you still want it) ────────────────────────────────────────
        private async void OnNextButtonClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new Menu(_loc, _bus));

        //// ── Show side menu ───────────────────────────────────────────────────────
        //private void OnOshkoshLogoClicked(object sender, EventArgs e)
        //{
        //    SideMenu.IsVisible = true;
        //    SideMenuDim.IsVisible = true;
        //    if (SideMenu.Width == 0)
        //        SideMenu.SizeChanged += SideMenu_SizeChangedAnimateIn;
        //    else
        //        _ = AnimateSideMenuIn();
        //}

        //private async void SideMenu_SizeChangedAnimateIn(object? sender, EventArgs e)
        //{
        //    if (SideMenu.Width > 0)
        //    {
        //        SideMenu.SizeChanged -= SideMenu_SizeChangedAnimateIn;
        //        await AnimateSideMenuIn();
        //    }
        //}

        //private async Task AnimateSideMenuIn()
        //{
        //    SideMenu.TranslationX = -SideMenu.Width;
        //    await SideMenu.TranslateTo(0, 0, 250, Easing.SinOut);
        //}

        //private async void SideMenuOnFirstSizeChanged(object? sender, EventArgs e)
        //{
        //    SideMenu.SizeChanged -= SideMenuOnFirstSizeChanged;
        //    _sideMenuFirstOpen = false;
        //    SideMenu.TranslationX = -SideMenu.Width;
        //    await SideMenu.TranslateTo(0, 0, 250, Easing.SinOut);
        //}

        //// ── Close side menu ──────────────────────────────────────────────────────
        //private async void OnCloseSideMenuClicked(object sender, EventArgs e)
        //{
        //    await SideMenu.TranslateTo(-SideMenu.Width, 0, 250, Easing.SinIn);
        //    SideMenu.IsVisible = false;
        //    SideMenuDim.IsVisible = false;
        //}

        //// ── Side‐menu navigation ─────────────────────────────────────────────────
        //async Task CloseAndNavigate(Func<Task> nav)
        //{
        //    await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
        //    SideMenu.IsVisible = false;
        //    SideMenuDim.IsVisible = false;
        //    await nav();
        //}

        //private void OnMenuClicked(object sender, EventArgs e)
        //    => _ = CloseAndNavigate(() => Navigation.PushAsync(new Menu(_loc, _bus)));

        //private void OnAngleSensorMenuClicked(object sender, EventArgs e)
        //    => _ = CloseAndNavigate(() => Navigation.PushAsync(new BAS(_loc, _bus)));

        //private void OnKzValveMenuClicked(object sender, EventArgs e)
        //    => _ = CloseAndNavigate(() => Navigation.PushAsync(new KZV(_loc, _bus)));

        //private void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
        //    => _ = CloseAndNavigate(() => Navigation.PushAsync(new FTLS(_loc, _bus)));

        private void OnCloseSideMenuClicked(object sender, EventArgs e)
        {
            SideMenu.IsVisible = false;
            SideMenuDim.IsVisible = false;
        }
        private void OnMenuClicked(object sender, EventArgs e) { /* … */ }

    }
}
#endif
