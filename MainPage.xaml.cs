using Microsoft.Maui;
using Microsoft.Maui.Controls;
using LocalizationResourceManager.Maui;
using PCANAppM.Services;
using System;
using System.Globalization;

namespace PCANAppM;

public partial class MainPage : ContentPage
{
    readonly ILocalizationResourceManager _loc;
    readonly ICanBusService               _bus;
    bool                                  _isDeviceConnected;

    public MainPage(
        ILocalizationResourceManager loc,
        ICanBusService               bus
    )
    {
        InitializeComponent();
        _loc = loc;
        _bus = bus;

        // live status updates:
        _bus.StatusChanged += OnStatusChanged;
        UpdateUI();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _bus.StatusChanged -= OnStatusChanged;
    }

    void OnStatusChanged()
    {
        MainThread.BeginInvokeOnMainThread(UpdateUI);
    }

    void UpdateUI()
    {
        bool c = _bus.IsConnected;
        StatusLabel.Text   = c
            ? $"{_bus.DeviceName}  {_loc["Status2"]}"
            : _loc["Status1"];
        StatusImage1.Source = c ? "green_check.png" : "red_ex.png";
        _isDeviceConnected  = c;
    }

    async void OnStatusImageClicked(object sender, EventArgs e)
    {
        if (_isDeviceConnected)
            await Navigation.PushAsync(new Menu(_loc, _bus));
        else
        {
            ConnectionDialog.IsVisible = true;
            MainContent.IsVisible      = false;
        }
    }

    void OnConnectionDialogOkClicked(object sender, EventArgs e)
    {
        ConnectionDialog.IsVisible = false;
        MainContent.IsVisible      = true;
    }

    // … your side-menu handlers exactly as before …
}
