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

namespace PCANAppM;

public partial class Menu : ContentPage
{
    private readonly ILocalizationResourceManager _localizationResourceManager;
    private bool _sideMenuFirstOpen = true;

    public Menu(ILocalizationResourceManager localizationResourceManager)
    {
        InitializeComponent();
        _localizationResourceManager = localizationResourceManager;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
    }

    private void OnLanguageButtonClicked(object sender, EventArgs e)
    {
        LanguageState.CurrentLanguage = LanguageState.CurrentLanguage == "en" ? "es" : "en";
        _localizationResourceManager.CurrentCulture = new CultureInfo(LanguageState.CurrentLanguage);
    }

    private async void OnBoomAngleSensorClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new BAS(_localizationResourceManager));
    }

    private async void OnFluidTankLevelSensorClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new FTLS(_localizationResourceManager));
    }

    private async void OnKZValveClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new KZV(_localizationResourceManager));
    }

    private void OnPointerEntered(object sender, EventArgs e) 
    {
        if (sender is VisualElement element)
        {
            element.ScaleTo(1.1, 100); 
        }
    }
    private void OnPointerExited(object sender, EventArgs e)
    {
        if (sender is VisualElement element)
        {
            element.ScaleTo(1.0, 100);
        }
    }

    private async void MenuButton_Pressed(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            // Animate to 0.95 scale over 60ms (shorter duration)
            await btn.ScaleTo(0.95, 20, Easing.SinIn);
        }
    }

    private async void MenuButton_Released(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            // Animate back to normal scale over 60ms
            await btn.ScaleTo(1, 20, Easing.SinOut);
        }
    }


    private async void OnNextButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Menu(_localizationResourceManager));
    }

    private void StatusImage1_Clicked(object sender, EventArgs e)
    {

    }

    //SIDE MENU LOGIC
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


}
