using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using LocalizationResourceManager.Maui;
using PCANAppM.Services;

namespace PCANAppM
{
#if WINDOWS
    public partial class Menu : ContentPage
    {
        // Injected localization + bus service
        private readonly ILocalizationResourceManager _localizationResourceManager;
        private readonly ICanBusService             _canBusService;

        private bool _sideMenuFirstOpen = true;

        public Menu(
            ILocalizationResourceManager localizationResourceManager,
            ICanBusService               canBusService
        )
        {
            InitializeComponent();

            _localizationResourceManager = localizationResourceManager;
            _canBusService               = canBusService;
        }

        private void OnLanguageButtonClicked(object sender, EventArgs e)
        {
            LanguageState.CurrentLanguage =
                LanguageState.CurrentLanguage == "en" ? "es" : "en";
            _localizationResourceManager.CurrentCulture =
                new CultureInfo(LanguageState.CurrentLanguage);
        }

        private async void OnBoomAngleSensorClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(
                new BAS(_localizationResourceManager, _canBusService)
            );
        }

        private async void OnFluidTankLevelSensorClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(
                new FTLS(_localizationResourceManager, _canBusService)
            );
        }

        private async void OnKZValveClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(
                new KZV(_localizationResourceManager, _canBusService)
            );
        }

        private void OnPointerEntered(object sender, EventArgs e)
        {
            if (sender is VisualElement element)
                element.ScaleTo(1.1, 100);
        }

        private void OnPointerExited(object sender, EventArgs e)
        {
            if (sender is VisualElement element)
                element.ScaleTo(1.0, 100);
        }

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

        private async void OnNextButtonClicked(object sender, EventArgs e)
        {
            // If you really need to re-open Menu
            await Navigation.PushAsync(
                new Menu(_localizationResourceManager, _canBusService)
            );
        }

        // ─── SIDE MENU ANIMATION ───────────────────────────────────────────────────

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
                new Menu(_localizationResourceManager, _canBusService)
            );
        }
    }
#endif
}
