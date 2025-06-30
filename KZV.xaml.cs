#if WINDOWS

using System;
using System.Globalization;
using System.Linq;
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
        private readonly CanBusService _bus;

        private Timer? _readTimer;
        private int _previousDeviceCount = 0;

        public KZV(ILocalizationResourceManager localizationResourceManager, CanBusService canBusService)
        {
            _loc = localizationResourceManager;
            _bus = canBusService;
            InitializeComponent();

            // Subscribe to device list changes for disconnect alert
            _bus.DeviceListChanged += OnDeviceListChanged;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _previousDeviceCount = _bus.AvailableDevices.Count;
            _bus.StartReading(); // Start reading CAN messages

           
            _readTimer = new Timer(100);
            _readTimer.Elapsed += (_, __) => ReadCanMessages();
            _readTimer.AutoReset = true;
            _readTimer.Start();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _bus.DeviceListChanged -= OnDeviceListChanged;
            _bus.StopReading(); // Stop reading CAN messages
            if (_readTimer != null)
            {
                _readTimer.Stop();
                _readTimer.Dispose();
                _readTimer = null;
            }
        }

        // Alert if disconnected
        private void OnDeviceListChanged()
        {
            var devices = _bus.AvailableDevices;
            if (_previousDeviceCount > 0 && devices.Count == 0)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Disconnected", "PCAN USB device has been disconnected.", "OK");
                });
            }
            _previousDeviceCount = devices.Count;
        }

        // Display current CAN ID (last two hex digits)
        private void ReadCanMessages()
        {
            var latest = _bus.ReceivedPackets
                .Where(pkt => pkt.IsExtended)
                .OrderByDescending(pkt => pkt.Microseconds)
                .FirstOrDefault();

            string labelText;
            if (latest != null)
            {
                string idHex = latest.Id.ToString("X");
                string lastTwo = idHex.Length >= 2 ? idHex.Substring(idHex.Length - 2) : idHex;
                labelText = $"{_loc["CurrentKZV"]} {lastTwo}";
            }
            else
            {
                labelText = $"{_loc["CurrentKZV"]} --";
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (this.FindByName<Label>("LatestCanIdLabel") is Label label)
                    label.Text = labelText;
            });
        }

        // --- Side menu handlers unchanged ---
        private void OnOshkoshLogoClicked(object sender, EventArgs e) { /* … */ }
        private void OnLanguageButtonClicked(object sender, EventArgs e) { /* … */ }
        private void OnMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnAngleSensorMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnKzValveMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnFluidTankLevelMenuClicked(object sender, EventArgs e) { /* … */ }
        private void OnCloseSideMenuClicked(object sender, EventArgs e) { /* ...*/ }
    }
}
#endif
