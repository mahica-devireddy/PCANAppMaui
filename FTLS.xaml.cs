using LocalizationResourceManager.Maui;
using PCANAppM.Services;
using PCANAppM.Platforms.Windows;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Maui.Controls;

namespace PCANAppM
{
#if WINDOWS
    public partial class FTLS : ContentPage
    {
        readonly ILocalizationResourceManager _loc;
        readonly PcanUsbStatusService         _statusService;

        Timer?   _connectionTimer;
        string?  _currentCanId;

        public FTLS(
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
            if (pgn == 0xFF52)
            {
                ResetConnectionTimer();
            }

            _currentCanId = (packet.Id & 0xFF).ToString();
            MainThread.BeginInvokeOnMainThread(() =>
                CurrentCanIdLabel.Text = $"{_loc["CurrentFTLS"]} {_currentCanId}");
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
            InitialFTLSView.IsVisible = false;
        }

        async void OnConfirmClicked(object sender, EventArgs e)
        {
            if (!int.TryParse(NewCanIdEntry.Text, out int newId) || newId < 0 || newId > 255)
            {
                await DisplayAlert("Invalid", "Enter 0-255", "OK");
                return;
            }

            // Example FTLS CAN-ID change:
            string hex = newId.ToString("X2");
            uint canId = Convert.ToUInt32($"0CEF{hex}02", 16);
            byte[] first = new byte[]{0x72,0x6F,0x74,0x61,0x2D,0x65,0x6E,0x6A};
            byte[] second = new byte[]{(byte)newId};

            _statusService.PcanUsb?.WriteFrame(canId, 8, first, true);
            await Task.Delay(100);
            _statusService.PcanUsb?.WriteFrame(canId, 1, second, true);

            SetCanIdView.IsVisible = false;
            InitialFTLSView.IsVisible = true;
        }

        void OnCancelClicked(object sender, EventArgs e)
        {
            SetCanIdView.IsVisible = false;
            InitialFTLSView.IsVisible = true;
        }

        // … your side-menu navigation …
    }
#endif
}
