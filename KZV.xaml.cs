using LocalizationResourceManager.Maui;
using PCANAppM.Services;
using PCANAppM.Platforms.Windows;
using System;
using System.Globalization;
using System.Timers;
using Microsoft.Maui.Controls;

namespace PCANAppM
{
#if WINDOWS
    public partial class KZV : ContentPage
    {
        readonly ILocalizationResourceManager _loc;
        readonly PcanUsbStatusService         _statusService;

        Timer?   _connectionTimer;
        string?  _currentCanId;

        public KZV(
            ILocalizationResourceManager loc,
            PcanUsbStatusService         statusService
        )
        {
            InitializeComponent();
            _loc           = loc;
            _statusService = statusService;

            _statusService.StatusChanged += OnStatusChanged;
            _statusService.PcanUsb?.MessageReceived += OnCanMessageReceived;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateConnectionUI();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _statusService.StatusChanged -= OnStatusChanged;
            _statusService.PcanUsb?.MessageReceived -= OnCanMessageReceived;
        }

        void OnStatusChanged()
            => MainThread.BeginInvokeOnMainThread(UpdateConnectionUI);

        void UpdateConnectionUI()
        {
            bool c = _statusService.IsConnected;
            ConnectionStatusLabel.Text = c
                ? _loc["Status2"]
                : _loc["Status1"];
        }

        void OnCanMessageReceived(PCAN_USB.Packet packet)
        {
            uint pgn = (packet.Id >> 8) & 0xFFFF;
            if (pgn == 0xFECA)
            {
                ResetConnectionTimer();
            }

            _currentCanId = (packet.Id & 0xFF).ToString();
            MainThread.BeginInvokeOnMainThread(() =>
                CurrentCanIdLabel.Text = $"{_loc["CurrentKZV"]} {_currentCanId}");
        }

        void ResetConnectionTimer()
        {
            _connectionTimer?.Stop();
            _connectionTimer = new Timer(2000) { AutoReset = false };
            _connectionTimer.Elapsed += (_,__) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    ConnectionStatusLabel.Text = _loc["Status1"]);
            };
            _connectionTimer.Start();
        }

        async void OnSetCanIdClicked(object sender, EventArgs e)
        {
            SetCanIdView.IsVisible = true;
            InitialKZVView.IsVisible = false;
        }

        async void OnConfirmClicked(object sender, EventArgs e)
        {
            if (!int.TryParse(NewCanIdEntry.Text, out int newId) || newId < 0 || newId > 255)
            {
                await DisplayAlert("Invalid", "Enter 0-255", "OK");
                return;
            }

            uint canId = (0x18EF0000u) | ((uint)newId << 8) | 0x01u;
            byte[] data = new byte[8];
            data[3] = 0x04;
            data[4] = (byte)newId;
            _statusService.PcanUsb?.WriteFrame(canId, 8, data, true);

            SetCanIdView.IsVisible = false;
            InitialKZVView.IsVisible = true;
        }

        void OnCancelClicked(object sender, EventArgs e)
        {
            SetCanIdView.IsVisible = false;
            InitialKZVView.IsVisible = true;
        }

        // … your side-menu navigation …
    }
#endif
}
