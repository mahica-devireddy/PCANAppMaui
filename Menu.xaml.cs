using System;
using System.Collections.Generic;
using System.Timers;
using Microsoft.Maui.Controls;
using Timer = System.Timers.Timer;
using System.Threading.Tasks;
using LocalizationResourceManager.Maui;
using System.Globalization;
using PCANAppM.Resources.Languages;

namespace PCANAppM;

public partial class Menu : ContentPage
{
    private readonly ILocalizationResourceManager _localizationResourceManager;

    public Menu(ILocalizationResourceManager localizationResourceManager)
	{
        base.OnAppearing();
        InitializeComponent();
        _localizationResourceManager = localizationResourceManager;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
       
    }

    private void OnLanguageButtonClicked(object sender, ToggledEventArgs e)
    {
        LanguageState.CurrentLanguage = e.Value ? "es" : "en";
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
