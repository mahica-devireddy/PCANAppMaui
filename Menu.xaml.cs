using Microsoft.Maui.Controls;
using LocalizationResourceManager.Maui;
using System;
using System.Globalization;
using PCANAppM.Services;

namespace PCANAppM;

public partial class Menu : ContentPage
{
    readonly ILocalizationResourceManager _loc;
    readonly ICanBusService               _bus;

    public Menu(ILocalizationResourceManager loc, ICanBusService bus)
    {
        InitializeComponent();
        _loc = loc;
        _bus = bus;
    }

    void OnLanguageButtonClicked(object sender, EventArgs e)
    {
        LanguageState.CurrentLanguage = LanguageState.CurrentLanguage == "en" ? "es" : "en";
        _loc.CurrentCulture = new CultureInfo(LanguageState.CurrentLanguage);
    }

    async void OnBoomAngleSensorClicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new BAS(_loc, _bus));

    async void OnFluidTankLevelSensorClicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new FTLS(_loc, _bus));

    async void OnKZValveClicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new KZV(_loc, _bus));

    // … side-menu, pointer animations, etc. …
}
