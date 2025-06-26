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
        readonly ILocalizationResourceManager _loc;
        readonly ICanBusService               _bus;
        bool                                  _sideMenuFirstOpen = true;

        public Menu(
            ILocalizationResourceManager loc,
            ICanBusService               bus
        )
        {
            InitializeComponent();
            _loc = loc;
            _bus = bus;
        }

        // ─── Language Toggle ────────────────────────────────────────────────
        private void OnLanguageButtonClicked(object sender, EventArgs e)
        {
            LanguageState.CurrentLanguage =
                LanguageState.CurrentLanguage == "en" ? "es" : "en";
            _loc.CurrentCulture =
                new CultureInfo(LanguageState.CurrentLanguage);
        }

        // ─── Navigation to Other Pages ──────────────────────────────────────
        private async void OnBoomAngleSensorClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new BAS(_loc, _bus));

        private async void OnKZValveClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new KZV(_loc, _bus));

        private async void OnFluidTankLevelSensorClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new FTLS(_loc, _bus));

        // ─── Button Animations ──────────────────────────────────────────────
        private void OnPointerEntered(object sender, EventArgs e)
        {
            if (sender is VisualElement el)
                el.ScaleTo(1.1, 100);
        }
        private void OnPointerExited(object sender, EventArgs e)
        {
            if (sender is VisualElement el)
                el.ScaleTo(1.0, 100);
        }
        private async void MenuButton_Pressed(object sender, EventArgs e)
        {
            if (sender is Button btn)
                await btn.ScaleTo(0.95, 20, Easing.SinIn);
        }
        private async void MenuButton_Released(object sender, EventArgs e)
        {
            if (sender is Button btn)
                await btn.ScaleTo(1.0, 20, Easing.SinOut);
        }

        // ─── Side‐Menu Slide In/Out ─────────────────────────────────────────
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
            SideMenu.SizeChanged -= SideMenu_SizeChangedAnimateIn;
            await AnimateSideMenuIn();
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

        // ─── (Optional) “Reload this menu” entry ─────────────────────────────
        private async void OnMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await Navigation.PushAsync(new Menu(_loc, _bus));
        }
    }
#endif
}
