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

namespace PCANAppM;

public partial class Menu : ContentPage
{
    private readonly ILocalizationResourceManager _localizationResourceManager;

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
        await Navigation.PushAsync(new BAS());
    }

    private async void OnFluidTankLevelSensorClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new FTLS());
    }

    private async void OnKZValveClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new KZV());
    }
}
