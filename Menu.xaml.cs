using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Globalization;
using LocalizationResourceManager.Maui;
using PCANAppM.Resources.Languages;
using PCANAppM.Services;    // ← add this

namespace PCANAppM;

public partial class Menu : ContentPage
{
    private readonly ILocalizationResourceManager _localizationResourceManager;
    private readonly ICanBusService             _canBusService;  // ← add this
    private bool _sideMenuFirstOpen = true;

    public Menu(
        ILocalizationResourceManager localizationResourceManager,
        ICanBusService               canBusService      // ← inject it
    )
    {
        InitializeComponent();
        _localizationResourceManager = localizationResourceManager;
        _canBusService               = canBusService;     // ← store it
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
            new BAS(_localizationResourceManager, _canBusService)  // ← pass it
        );
    }

    private async void OnFluidTankLevelSensorClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(
            new FTLS(_localizationResourceManager, _canBusService)  // ← pass it
        );
    }

    private async void OnKZValveClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(
            new KZV(_localizationResourceManager, _canBusService)   // ← pass it
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
        // Navigates to another instance of Menu?
        await Navigation.PushAsync(
            new Menu(_localizationResourceManager, _canBusService)  // ← pass it
        );
    }

    // SIDE MENU LOGIC
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

    private async void SideMenuOnFirstSizeChanged(object? sender, EventArgs e)
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

    private async void OnMenuClicked(object sender, EventArgs e)
    {
        await SideMenu.TranslateTo(-SideMenu.Width, 0, 200, Easing.SinIn);
        SideMenu.IsVisible    = false;
        SideMenuDim.IsVisible = false;
        await Navigation.PushAsync(
            new Menu(_localizationResourceManager, _canBusService)  // ← pass it
        );
    }
}
using Microsoft.Maui.Controls;
using LocalizationResourceManager.Maui;
using PCANAppM.Services;

namespace PCANAppM
{
#if WINDOWS
    public partial class Menu : ContentPage
    {
        readonly ILocalizationResourceManager _loc;
        readonly CanBusService               _bus;

        public Menu(
            ILocalizationResourceManager loc,
            CanBusService               bus
        )
        {
            InitializeComponent();
            _loc = loc;
            _bus = bus;
        }

        async void OnBoomAngleSensorClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new BAS(_loc, _bus));

        async void OnKZValveClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new KZV(_loc, _bus));

        async void OnFTLSClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new FTLS(_loc, _bus));
        
        // … your pointer animations, etc. …
    }
#endif
}
