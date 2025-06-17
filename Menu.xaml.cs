using System;
using System.Globalization;
using LocalizationResourceManager.Maui;
using Microsoft.Maui.Controls;

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
