#if WINDOWS

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using LocalizationResourceManager.Maui;
using PCANAppM.Services;
using PCANAppM.Platforms.Windows;
using PCANAppM.Resources.Languages;
using Peak.Can.Basic;
using Timer = System.Timers.Timer;

namespace PCANAppM
{
    public partial class KZV : ContentPage
    {
        private readonly ILocalizationResourceManager _loc;
        private readonly ICanBusService _bus;

        private Timer? _readTimer;

        public KZV(ILocalizationResourceManager localizationResourceManager, ICanBusService canBusService)
        {
            _loc = localizationResourceManager;
            _bus = canBusService;
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _readTimer = new Timer(50);
            _readTimer.Elapsed += (_, __) => ReadCanMessages();
            _readTimer.AutoReset = true;
            _readTimer.Start();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_readTimer != null)
            {
                _readTimer.Stop();
                _readTimer.Dispose();
                _readTimer = null;
            }
        }

        private void ReadCanMessages()
        {
            _bus.ReadMessages((msg, timestamp) =>
            {
                // Only process extended frames
                if ((msg.MSGTYPE & TPCANMessageType.PCAN_MESSAGE_EXTENDED) == 0)
                    return;

                // Get the last two hex digits of the CAN ID
                string idHex = msg.ID.ToString("X");
                string lastTwo = idHex.Length >= 2 ? idHex.Substring(idHex.Length - 2) : idHex;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LatestCanIdLabel.Text = $"{_loc["CurrentKZV"]} {lastTwo}";
                });
            });
        }




        //private void UpdateLatestCanIdLabel(string id)
        //{
        //    if (_latestCanIdLabel != null)
        //    {
        //        _latestCanIdLabel.Text = _loc["CurrentKZV"] + " " + id;
        //    }
        //}

        //private async void OnSetClicked(object sender, EventArgs e)
        //{
        //    var newCanId = NewCanIdEntry.Text?.Trim();
        //    if (string.IsNullOrEmpty(newCanId) || newCanId.Length > 2 || !int.TryParse(newCanId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        //    {
        //        await DisplayAlert("Invalid Input", "Please enter a valid 2-digit hex CAN ID.", "OK");
        //        return;
        //    }

        //    ConfirmText.Text = $"Set The CAN ID to {newCanId.ToUpper()}";
        //    SetCanIdView.IsVisible = false;
        //    ConfirmCanIdView.IsVisible = true;
        //    _pendingNewCanId = newCanId.ToUpper();
        //    NewCanIdEntry.Text = string.Empty;
        //}

        //private async void OnConfirmClicked(object sender, EventArgs e)
        //{
        //    if (string.IsNullOrEmpty(_pendingNewCanId)) return;

        //    string currentId = _currentCanId ?? "00";
        //    string newId = _pendingNewCanId.PadLeft(2, '0');

        //    byte currentIdByte = byte.Parse(currentId, NumberStyles.HexNumber);
        //    byte newIdByte = byte.Parse(newId, NumberStyles.HexNumber);

        //    uint canId = (0x18EF0000u) | ((uint)currentIdByte << 8) | 0x01u;
        //    var data = new byte[8];
        //    data[3] = 0x04;
        //    data[4] = newIdByte;

        //    _bus.SendFrame(canId, 8, data);

        //    _currentCanId = newId;
        //    UpdateLatestCanIdLabel(newId);
        //    ConfirmCanIdView.IsVisible = false;
        //    InitialKzvView.IsVisible = true;
        //}

        //private void OnCancelConfirmClicked(object sender, EventArgs e)
        //{
        //    ConfirmCanIdView.IsVisible = false;
        //    InitialKzvView.IsVisible = true;
        //}

        //private async void OnKZVClicked(object sender, EventArgs e)
        //{
        //    await Navigation.PushAsync(
        //        new KZVConnectionStatusPage(false)); // Connection status logic removed
        //}

        //private async void OnCheckConnectionClicked(object sender, EventArgs e)
        //{
        //    await DisplayAlert("Connection Status", "Connection status logic removed.", "OK");
        //}

        private void OnExitClicked(object sender, EventArgs e)
        {
            SetCanIdView.IsVisible = false;
            ConfirmCanIdView.IsVisible = false;
            InitialKzvView.IsVisible = true;
        }

        // --- Side menu handlers unchanged ---
        private void OnOshkoshLogoClicked(object sender, EventArgs e) { /* … */ }
        private void OnLanguageButtonClicked(object sender, EventArgs e) { /* … */ }
        private void OnMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnAngleSensorMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnKzValveMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnFluidTankLevelMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnCloseSideMenuClicked(object sender, EventArgs e) {/* ...*/}
    }
}
#endif
