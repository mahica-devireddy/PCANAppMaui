using LocalizationResourceManager.Maui;
using Peak.Can.Basic;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Maui.Controls;
using PCANAppM.Services;
using PCANAppM.Resources.Languages;

#if WINDOWS
using PCANAppM.Services;
using PCANAppM.Platforms.Windows;
using System.Timers; 
#endif

namespace PCANAppM
{
#if WINDOWS
    public partial class BAS : ContentPage
    {
        private string? _currentCanId1;
        private string? _pendingNewCanId1;
        private readonly ILocalizationResourceManager _localizationResourceManager;
        private readonly ICanBusService _canBusService;
        private System.Timers.Timer? _connectionTimeoutTimer;
        private bool _sideMenuFirstOpen = true;

        public BAS(
            ILocalizationResourceManager localizationResourceManager,
            ICanBusService canBusService
        )
        {
            _localizationResourceManager = localizationResourceManager;
            _canBusService = canBusService;
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _canBusService.FrameReceived += OnCanMessageReceived;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _canBusService.FrameReceived -= OnCanMessageReceived;
        }

        private void OnCanMessageReceived(PCAN_USB.Packet packet)
        {
            uint pgn = (packet.Id >> 8) & 0xFFFF;
            if (pgn == 0xFFBB)
            {
                ASConnectionState.IsConnected = true;
                ResetConnectionTimeout();
            }

            var idHex = $"0x{packet.Id:X}";
            var lastTwo = idHex.Length >= 2
                ? idHex.Substring(idHex.Length - 2)
                : idHex;

            if (int.TryParse(lastTwo, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var canIdInt))
            {
                _currentCanId1 = canIdInt.ToString();
                MainThread.BeginInvokeOnMainThread(() =>
                    UpdateLatestCanIdLabel1(_currentCanId1));
            }
        }

        private void ResetConnectionTimeout()
        {
            _connectionTimeoutTimer?.Stop();
            _connectionTimeoutTimer = new System.Timers.Timer(2000) { AutoReset = false };
            _connectionTimeoutTimer.Elapsed += (s, e) =>
            {
                ASConnectionState.IsConnected = false;
                _connectionTimeoutTimer?.Stop();
            };
            _connectionTimeoutTimer.Start();
        }

        private async void OnSetCanIdClicked(object sender, EventArgs e)
        {
            SetCanIdView1.IsVisible = true;
            InitialBasView.IsVisible = false;
        }

        private async void OnSetClicked(object sender, EventArgs e)
        {
            var newCanId = NewCanIdEntry1.Text?.Trim();
            if (string.IsNullOrEmpty(newCanId)
                || !int.TryParse(newCanId, out var newCanIdInt)
                || newCanIdInt < 0
                || newCanIdInt > 255)
            {
                await DisplayAlert(
                    "Invalid Input",
                    "Please enter a valid CAN ID value between 0-255.",
                    "OK"
                );
                return;
            }

            ConfirmText1.Text = $"Set The CAN ID to {newCanIdInt}";
            SetCanIdView1.IsVisible = false;
            ConfirmCanIdView1.IsVisible = true;
            _pendingNewCanId1 = newCanIdInt.ToString();
            NewCanIdEntry1.Text = string.Empty;
        }

        private async void OnConfirmClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_pendingNewCanId1))
                return;

            int.TryParse(_currentCanId1, out var currentIdInt);
            var newIdInt = int.Parse(_pendingNewCanId1);

            var currentIdHex = currentIdInt.ToString("X2");
            var newIdHex = newIdInt.ToString("X2");

            // Send your four messages via the shared service:
            _canBusService.SendFrame(
                uint.Parse($"18EA{currentIdHex}00", NumberStyles.HexNumber),
                new byte[] { 0x00, 0xEF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                extended: true
            );
            _canBusService.SendFrame(
                uint.Parse($"18EF{currentIdHex}00", NumberStyles.HexNumber),
                new byte[] { 0x06, Convert.ToByte(newIdHex, 16), 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF },
                extended: true
            );
            _canBusService.SendFrame(
                uint.Parse($"18EA{currentIdHex}00", NumberStyles.HexNumber),
                new byte[] { 0x00, 0xEF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                extended: true
            );
            _canBusService.SendFrame(
                uint.Parse($"18EF{currentIdHex}00", NumberStyles.HexNumber),
                new byte[] { 0xFA, 0x73, 0x61, 0x76, 0x65, 0x00, 0x00, 0x00 },
                extended: true
            );

            ConfirmCanIdView1.IsVisible = false;
            InitialBasView.IsVisible = true;
        }

        private void OnCancelConfirmClicked1(object sender, EventArgs e)
        {
            ConfirmCanIdView1.IsVisible = false;
            InitialBasView.IsVisible = true;
        }

        private void OnExitClicked(object sender, EventArgs e)
        {
            SetCanIdView1.IsVisible = false;
            InitialBasView.IsVisible = true;
            ConfirmCanIdView1.IsVisible = false;
        }

        private void UpdateLatestCanIdLabel1(string id)
        {
            LatestCanIdLabel1.Text = $"{_localizationResourceManager["CurrentBAS"]} {id}";
        }

        private async void OnAngleSensorStatusClicked(object sender, EventArgs e)
        {
            await DisplayAlert(
                "Connection Status",
                ASConnectionState.IsConnected
                    ? "Angle Sensor is CONNECTED."
                    : "Angle Sensor is NOT CONNECTED.",
                "OK"
            );
        }

        private async void OnAngleSensorButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(
                new ASConnectionStatusPage(ASConnectionState.IsConnected)
            );
        }

        private async void OnMenuClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(
                new Menu(_localizationResourceManager, _canBusService)
            );
        }

        private async void OnAngleSensorMenuClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(
                new BAS(_localizationResourceManager, _canBusService)
            );
        }

        private async void OnKzValveMenuClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(
                new KZV(_localizationResourceManager, _canBusService)
            );
        }

        private async void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(
                new FTLS(_localizationResourceManager, _canBusService)
            );
        }

        private async void OnLanguageButtonClicked(object sender, EventArgs e)
        {
            LanguageState.CurrentLanguage =
                LanguageState.CurrentLanguage == "en" ? "es" : "en";
            _localizationResourceManager.CurrentCulture =
                new CultureInfo(LanguageState.CurrentLanguage);
        }

        private void OnOshkoshLogoClicked(object sender, EventArgs e)
        {
            SideMenu.IsVisible = true;
            SideMenuDim.IsVisible = true;
            if (SideMenu.Width == 0)
                SideMenu.SizeChanged += SideMenu_SizeChangedAnimateIn;
            else
                AnimateSideMenuIn();
        }

        private async void SideMenu_SizeChangedAnimateIn(object? sender, EventArgs e)
        {
            if (SideMenu.Width > 0)
            {
                SideMenu.SizeChanged -= SideMenu_SizeChangedAnimateIn;
                await AnimateSideMenuIn();
            }
        }

        private async Task AnimateSideMenuIn()
        {
            SideMenu.TranslationX = -SideMenu.Width;
            await SideMenu.TranslateTo(0, 0, 250, Easing.SinOut);
        }

        private async void OnCloseSideMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 250, Easing.SinIn);
            SideMenu.IsVisible = false;
            SideMenuDim.IsVisible = false;
        }
    }

    public class CanMessageViewModel1
    {
        public string Direction { get; set; } = "";
        public string Id { get; set; } = "";
        public string Data { get; set; } = "";
    }
#endif
}

using Microsoft.Maui.Controls;
using LocalizationResourceManager.Maui;
using PCANAppM.Services;
using System;
using System.Globalization;
using System.Timers;

namespace PCANAppM
{
#if WINDOWS
    public partial class BAS : ContentPage
    {
        readonly ILocalizationResourceManager _loc;
        readonly CanBusService               _bus;
        Timer                                _timeout;

        public BAS(
            ILocalizationResourceManager loc,
            CanBusService               bus
        )
        {
            InitializeComponent();
            _loc = loc;
            _bus = bus;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _bus.FrameReceived += OnFrame;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _bus.FrameReceived -= OnFrame;
            _timeout?.Stop();
        }

        void OnFrame(PCAN_USB.Packet pkt)
        {
            // PGN for BAS?
            uint pgn = (pkt.Id >> 8) & 0xFFFF;
            if (pgn == 0xFFBB)
            {
                ASConnectionState.IsConnected = true;
                DebounceTimeout();
            }

            // extract last‐two hex chars:
            string hx = pkt.Id.ToString("X");
            string last2 = hx.Length >= 2 ? hx[^2..] : hx;
            if (int.TryParse(last2, NumberStyles.HexNumber, null, out var idInt))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    LatestCanIdLabel1.Text = $"{_loc["CurrentBAS"]} {idInt}"
                );
            }
        }

        void DebounceTimeout()
        {
            _timeout?.Stop();
            _timeout = new Timer(2000) { AutoReset = false };
            _timeout.Elapsed += (_,__) => ASConnectionState.IsConnected = false;
            _timeout.Start();
        }

        async void OnSetClicked(object sender, EventArgs e)
        {
            // your UI-driven ID pick logic here…
            byte newId = /* … */;
            uint canId = /* build PGN */;
            _bus.SendFrame(canId, new byte[]{ /* … */ }, canId > 0x7FF);
        }
    }
#endif
}
