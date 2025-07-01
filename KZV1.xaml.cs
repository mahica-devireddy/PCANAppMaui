using LocalizationResourceManager.Maui;
using Microsoft.Maui.Controls;
using PCANAppM.Services;
using PCANAppM.Platforms.Windows; // for PCAN_USB.Packet
using Peak.Can.Basic;
using System;
using System.Globalization;
using System.Timers;

namespace PCANAppM
{
    public partial class KZV : ContentPage
    {
        private readonly ILocalizationResourceManager _loc;
        private readonly CanBusService _bus;

        // for connection‐timeout logic
        private DateTime _lastPacketTime = DateTime.MinValue;
        private readonly Timer _connectionTimer;

        // for ID‐change flow
        private byte _currentId;
        private byte? _pendingId;

        public KZV(
            ILocalizationResourceManager loc,
            CanBusService bus
        )
        {
            InitializeComponent();

            _loc = loc;
            _bus = bus;

            // whenever any CAN packet arrives, handle it
            _bus.MessageReceived += OnBusPacket;

            // 2s timer to flip to “disconnected” if no packets arrive
            _connectionTimer = new Timer(2000) { AutoReset = false };
            _connectionTimer.Elapsed += (_, __) =>
                MainThread.BeginInvokeOnMainThread(() =>
                    ConnectionStatusLabel.Text = _loc["Disconnected"]
                );
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // reset UI
            LatestCanIdLabel.Text      = $"{_loc["CurrentKZV"]}: --";
            ConnectionStatusLabel.Text = _loc["Disconnected"];
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _bus.MessageReceived -= OnBusPacket;
            _connectionTimer.Stop();
        }

        private void OnBusPacket(PCAN_USB.Packet pkt)
        {
            // only extended frames
            if (!pkt.IsExtended) return;

            // record arrival
            _lastPacketTime = DateTime.UtcNow;
            _connectionTimer.Stop();
            _connectionTimer.Start();

            // take the full 29-bit ID, to hex string
            string hex = pkt.Id.ToString("X");
            string lastTwo = hex.Length >= 2
                ? hex.Substring(hex.Length - 2)
                : hex;

            // parse lastTwo as hex → decimal
            if (!int.TryParse(lastTwo, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var dec))
                return;

            _currentId = (byte)dec;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                LatestCanIdLabel.Text      = $"{_loc["CurrentKZV"]}: {dec}";
                ConnectionStatusLabel.Text = _loc["Connected"];
            });
        }

        // “Check Connection” button
        private async void OnCheckConnectionClicked(object sender, EventArgs e)
        {
            bool connected = (DateTime.UtcNow - _lastPacketTime).TotalMilliseconds < 2000;
            string msg = connected
                ? "KZ Valve is CONNECTED."
                : "KZ Valve is NOT CONNECTED.";
            await DisplayAlert("Connection Status", msg, "OK");
        }

        // “Set New ID” button
        private void OnSetCanIdClicked(object sender, EventArgs e)
        {
            InitialKzvView.IsVisible = false;
            SetCanIdView.IsVisible   = true;
        }

        // after entering new ID
        private async void OnSetClicked(object sender, EventArgs e)
        {
            string txt = NewCanIdEntry.Text?.Trim() ?? "";
            if (!byte.TryParse(txt, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
            {
                await DisplayAlert("Invalid", "Enter a number 0–255.", "OK");
                return;
            }

            _pendingId        = val;
            ConfirmText.Text  = $"Set CAN ID to {val}";
            SetCanIdView.IsVisible     = false;
            ConfirmCanIdView.IsVisible = true;
        }

        // confirm sending the new ID
        private async void OnConfirmClicked(object sender, EventArgs e)
        {
            if (_pendingId == null) return;

            // build J1939 “set address” ID:
            uint canId = 0x18EF0000u
                       | ((uint)_currentId << 8)
                       | 0x01u;

            // data payload
            var data = new byte[8];
            data[3] = 0x04;
            data[4] = _pendingId.Value;

            var res = _bus.SendFrame(canId, data.Length, data, isExtended: true);
            if (res != TPCANStatus.PCAN_ERROR_OK)
                await DisplayAlert("Error", $"Send failed: {res}", "OK");

            ConfirmCanIdView.IsVisible = false;
            InitialKzvView.IsVisible   = true;
            _pendingId                 = null;
        }

        // cancel the change flow
        private void OnCancelConfirmClicked(object sender, EventArgs e)
        {
            ConfirmCanIdView.IsVisible = false;
            InitialKzvView.IsVisible   = true;
        }
    }
}
