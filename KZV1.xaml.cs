using System;
using System.Timers;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using LocalizationResourceManager.Maui;
using PCANAppM.Services;
using Peak.Can.Basic;

namespace PCANAppM
{
    public partial class KZVPage : ContentPage
    {
        private readonly ILocalizationResourceManager _loc;
        private readonly ICanBusService _canBus;
        private Timer _readTimer;

        public KZVPage(
            ILocalizationResourceManager loc,
            ICanBusService canBusService)
        {
            InitializeComponent();
            _loc    = loc;
            _canBus = canBusService;
            _canBus.ConnectionStatusChanged += OnConnectionChanged;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _canBus.StartMonitoring();

            _readTimer = new Timer(50);
            _readTimer.Elapsed += (_, __) => ReadMessages();
            _readTimer.AutoReset = true;
            _readTimer.Start();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _readTimer?.Stop();
            _canBus.StopMonitoring();
            _canBus.ConnectionStatusChanged -= OnConnectionChanged;
        }

        private void OnConnectionChanged(object sender, bool isConnected)
        {
            MainThread.BeginInvokeOnMainThread(() =>
                ConnectionStatusLabel.Text = isConnected
                    ? _loc["Connected"]
                    : _loc["Disconnected"]
            );
        }

        private void ReadMessages()
        {
            if (!_canBus.IsConnected)
                return;

            _canBus.ReadMessages((msg, ts) =>
            {
                if ((msg.MSGTYPE & TPCANMessageType.PCAN_MESSAGE_EXTENDED) == 0)
                    return;

                var full = msg.ID.ToString("X");
                var last = full.Length >= 2
                    ? full.Substring(full.Length - 2)
                    : full;

                MainThread.BeginInvokeOnMainThread(() =>
                    LatestCanIdLabel.Text = $"{_loc["CurrentKZV"]}: {last}"
                );
            });
        }
    }
}
