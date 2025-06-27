using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;   // for MainThread
using PCANAppM.Services;                // your ICanBusService + CanBusService

namespace PCANAppM
{
    public partial class MainPage : ContentPage
    {
        private readonly ICanBusService _canBusService;

        public MainPage()
        {
            InitializeComponent();

            // if you’re not using DI, just new it up here:
            _canBusService = new CanBusService();

            // hook the event
            _canBusService.ConnectionStatusChanged += OnConnectionStatusChanged;

            // set initial UI state
            UpdateStatus(_canBusService.IsConnected);

            // kick off polling
            _canBusService.StartMonitoring();
        }

        private void OnConnectionStatusChanged(object sender, bool isConnected)
        {
            // ensure UI thread
            MainThread.BeginInvokeOnMainThread(() =>
                UpdateStatus(isConnected)
            );
        }

        private void UpdateStatus(bool isConnected)
        {
            if (isConnected)
            {
                StatusLabel.Text        = "Connected";
                StatusImage1.Source     = "green_check.png";
            }
            else
            {
                StatusLabel.Text        = "Disconnected";
                StatusImage1.Source     = "red_ex.png";
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _canBusService.StopMonitoring();
            _canBusService.ConnectionStatusChanged -= OnConnectionStatusChanged;
        }

        // XAML‐wired handlers you already have
        private void OnStatusImageClicked(object sender, EventArgs e)
        {
            if (!_canBusService.IsConnected)
                ConnectionDialog.IsVisible = true;
        }

        private void OnConnectionDialogOkClicked(object sender, EventArgs e)
            => ConnectionDialog.IsVisible = false;

        private void OnOshkoshLogoClicked(object sender, EventArgs e) { /* … */ }
        private void OnLanguageButtonClicked(object sender, EventArgs e) { /* … */ }
        private void OnMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnAngleSensorMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnKzValveMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnFluidTankLevelMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnCloseSideMenuClicked(object sender, EventArgs e)
        {
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
        }
    }
}
