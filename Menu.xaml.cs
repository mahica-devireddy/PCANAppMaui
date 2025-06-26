using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using LocalizationResourceManager.Maui;

namespace PCANAppM
{
#if WINDOWS
    public partial class Menu : ContentPage
    {
        readonly ILocalizationResourceManager _loc;
        bool                                  _sideMenuFirstOpen = true;

        public Menu(ILocalizationResourceManager localizationResourceManager)
        {
            InitializeComponent();
            _loc = localizationResourceManager;
        }

        // ── Language toggle in header ────────────────────────────────────────────
        private void OnLanguageButtonClicked(object sender, EventArgs e)
        {
            LanguageState.CurrentLanguage =
                LanguageState.CurrentLanguage == "en" ? "es" : "en";
            _loc.CurrentCulture =
                new CultureInfo(LanguageState.CurrentLanguage);
        }

        // ── Main menu buttons ────────────────────────────────────────────────────
        private async void OnBoomAngleSensorClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new BAS(_loc));

        private async void OnKZValveClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new KZV(_loc));

        private async void OnFluidTankLevelSensorClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new FTLS(_loc));

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
            => await Navigation.PushAsync(new Menu(_loc));

        // ── Show side menu ───────────────────────────────────────────────────────
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
            _sideMenuFirstOpen     = false;
            SideMenu.TranslationX = -SideMenu.Width;
            await SideMenu.TranslateTo(0, 0, 250, Easing.SinOut);
        }

        // ── Close side menu ──────────────────────────────────────────────────────
        private async void OnCloseSideMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 250, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
        }

        // ── Side‐menu navigation ─────────────────────────────────────────────────
        async Task CloseAndNavigate(Func<Task> nav)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
            await nav();
        }

        private void OnMenuClicked(object sender, EventArgs e)
            => _ = CloseAndNavigate(() => Navigation.PushAsync(new Menu(_loc)));

        private void OnAngleSensorMenuClicked(object sender, EventArgs e)
            => _ = CloseAndNavigate(() => Navigation.PushAsync(new BAS(_loc)));

        private void OnKzValveMenuClicked(object sender, EventArgs e)
            => _ = CloseAndNavigate(() => Navigation.PushAsync(new KZV(_loc)));

        private void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
            => _ = CloseAndNavigate(() => Navigation.PushAsync(new FTLS(_loc)));
    }
#endif
}
