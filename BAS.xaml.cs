#if WINDOWS

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using LocalizationResourceManager.Maui;
using PCANAppM.Services;       // ← your single service
using PCANAppM.Platforms.Windows;
using PCANAppM.Resources.Languages;
using Peak.Can.Basic;
using Timer = System.Timers.Timer;

namespace PCANAppM
{
    public partial class BAS : ContentPage
    {
        readonly ILocalizationResourceManager _loc;
        readonly ICanBusService               _bus;

        string?      _currentCanId;
        string?      _pendingCanId;
        bool         _isStarted;
        bool         _isAngleSensorConnected;
        Timer?       _connectionTimeoutTimer;
        bool         _sideMenuFirstOpen = true;

        public BAS(
            ILocalizationResourceManager loc,
            ICanBusService               bus
        )
        {
            InitializeComponent();
            _loc = loc;
            _bus = bus;

            // subscribe to live CAN frames
            _bus.FrameReceived += OnFrameReceived;
            _isStarted = _bus.IsConnected;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _bus.FrameReceived -= OnFrameReceived;
        }

        void OnFrameReceived(PCAN_USB.Packet packet)
        {
            uint pgn = (packet.Id >> 8) & 0xFFFF;
            if (pgn == 0xFFBB)
            {
                _isAngleSensorConnected = true;
                ResetConnectionTimeout();
            }

            var idHex = $"0x{packet.Id:X}";
            var lastTwo = idHex.Length >= 2
                ? idHex.Substring(idHex.Length - 2)
                : idHex;

            if (int.TryParse(
                lastTwo, NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out var cid))
            {
                _currentCanId = cid.ToString();
                MainThread.BeginInvokeOnMainThread(() =>
                    LatestCanIdLabel1.Text =
                        $"{_loc["CurrentBAS"]} {_currentCanId}"
                );
            
            }
        }

        private void ResetConnectionTimeout()
        {
            _connectionTimeoutTimer?.Stop();
            _connectionTimeoutTimer = new Timer(2000) { AutoReset = false };
            _connectionTimeoutTimer.Elapsed += (_, __) =>
            {
                _isAngleSensorConnected = false;
                _connectionTimeoutTimer?.Stop();
            };
            _connectionTimeoutTimer.Start();
        }

        // ─── Language toggle ─────────────────────────────────────────────────────
        private void OnLanguageButtonClicked(object sender, EventArgs e)
        {
            LanguageState.CurrentLanguage =
                LanguageState.CurrentLanguage == "en" ? "es" : "en";
            _loc.CurrentCulture =
                new CultureInfo(LanguageState.CurrentLanguage);
        }

        // ─── Set CAN ID flow ──────────────────────────────────────────────────────
        private void OnSetCanIdClicked(object sender, EventArgs e)
        {
            SetCanIdView1.IsVisible = true;
            InitialBasView.IsVisible = false;
        }

        private async void OnSetClicked(object sender, EventArgs e)
        {
            var txt = NewCanIdEntry1.Text?.Trim();
            if (string.IsNullOrEmpty(txt)
                || !int.TryParse(txt, out var nid)
                || nid < 0 || nid > 255)
            {
                await DisplayAlert(
                    _loc["Error"],
                    _loc["Please enter a valid CAN ID value between 0-255."],
                    "OK");
                return;
            }

            ConfirmText1.Text            = $"Set The CAN ID to {nid}";
            SetCanIdView1.IsVisible      = false;
            ConfirmCanIdView1.IsVisible  = true;
            _pendingCanId                = nid.ToString();
            NewCanIdEntry1.Text          = string.Empty;
        }

        private void OnCancelConfirmClicked1(object sender, EventArgs e)
        {
            ConfirmCanIdView1.IsVisible = false;
            InitialBasView.IsVisible    = true;
        }

        private void OnExitClicked(object sender, EventArgs e)
        {
            SetCanIdView1.IsVisible      = false;
            InitialBasView.IsVisible    = true;
            ConfirmCanIdView1.IsVisible  = false;
        }

        private async void OnConfirmClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_pendingCanId)) return;

            if (!int.TryParse(_currentCanId, out var cur)) cur = 0;
            byte currB = (byte)cur;
            byte newB  = (byte)int.Parse(_pendingCanId);

            // your 4-msg protocol:
            _bus.SendFrame(
                uint.Parse($"18EA{currB:X2}00", NumberStyles.HexNumber),
                new byte[]{0x00,0xEF,0x00,0x00,0x00,0x00,0x00,0x00},
                extended: true
            );
            _bus.SendFrame(
                uint.Parse($"18EF{currB:X2}00", NumberStyles.HexNumber),
                new byte[]{0x06,newB,0x00,0xFF,0xFF,0xFF,0xFF,0xFF},
                extended: true
            );
            _bus.SendFrame(
                uint.Parse($"18EA{currB:X2}00", NumberStyles.HexNumber),
                new byte[]{0x00,0xEF,0x00,0x00,0x00,0x00,0x00,0x00},
                extended: true
            );
            _bus.SendFrame(
                uint.Parse($"18EF{currB:X2}00", NumberStyles.HexNumber),
                new byte[]{0xFA,0x73,0x61,0x76,0x65,0x00,0x00,0x00},
                extended: true
            );

            ConfirmCanIdView1.IsVisible = false;
            InitialBasView.IsVisible    = true;
        }

        // ─── Connection-status pop-up ────────────────────────────────────────────
        private async void OnAngleSensorStatusClicked(object sender, EventArgs e)
        {
            var msg = _isAngleSensorConnected
                ? "Angle Sensor is CONNECTED."
                : "Angle Sensor is NOT CONNECTED.";
            await DisplayAlert("Connection Status", msg, "OK");
        }

        private async void OnAngleSensorButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(
                new ASConnectionStatusPage(_isAngleSensorConnected)
            );
        }

        // ─── Side-menu handlers ───────────────────────────────────────────────────
        private async void OnCheckConnectionClicked(object sender, EventArgs e)
        {
            var msg = _isAngleSensorConnected
                ? "Angle Sensor is CONNECTED."
                : "Angle Sensor is NOT CONNECTED.";
            await DisplayAlert("Connection Status", msg, "OK");
        }

        private void OnOshkoshLogoClicked(object sender, EventArgs e)
        {
            SideMenu.IsVisible    = true;
            SideMenuDim.IsVisible = true;
            if (SideMenu.Width == 0)
                SideMenu.SizeChanged += SideMenu_SizeChangedAnimateIn;
            else
                _ = AnimateSideMenuIn();
        }

        private async void SideMenu_SizeChangedAnimateIn(object? sender, EventArgs e)
        {
            if (SideMenu.Width > 0)
            {
                SideMenu.SizeChanged -= SideMenu_SizeChangedAnimateIn;
                await AnimateSideMenuIn();
            }
        }

        private Task AnimateSideMenuIn() =>
            SideMenu.TranslateTo(0, 0, 250, Easing.SinOut);

        private async void SideMenuOnFirstSizeChanged(object? sender, EventArgs e)
        {
            SideMenu.SizeChanged -= SideMenuOnFirstSizeChanged;
            _sideMenuFirstOpen = false;
            SideMenu.TranslationX = -SideMenu.Width;
            await SideMenu.TranslateTo(0, 0, 250, Easing.SinOut);
        }

        private async void OnCloseSideMenuClicked(object sender, EventArgs e)
        {
            await SideMenu.TranslateTo(-SideMenu.Width, 0, 250, Easing.SinIn);
            SideMenu.IsVisible    = false;
            SideMenuDim.IsVisible = false;
        }

        private async void OnMenuClicked(object sender, EventArgs e)
        {
            await AnimateSideMenuIn();
            await Navigation.PushAsync(new Menu(_loc, _bus));
        }

        private async void OnAngleSensorMenuClicked(object sender, EventArgs e)
        {
            await AnimateSideMenuIn();
            await Navigation.PushAsync(new BAS(_loc, _bus));
        }

        private async void OnKzValveMenuClicked(object sender, EventArgs e)
        {
            await AnimateSideMenuIn();
            await Navigation.PushAsync(new KZV(_loc, _bus));
        }

        private async void OnFluidTankLevelMenuClicked(object sender, EventArgs e)
        {
            await AnimateSideMenuIn();
            await Navigation.PushAsync(new FTLS(_loc, _bus));
        }
    }
}
#endif

using LocalizationResourceManager.Maui;
using PCANAppM.Services;
using Peak.Can.Basic;
using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace PCANAppM
{
#if WINDOWS
    public partial class BAS : ContentPage
    {
        readonly ILocalizationResourceManager _loc;
        readonly ICanBusService _bus;

        string? _currentId, _pendingId;
        bool    _deviceStarted, _isConnected;
        Timer?  _timeout;

        public BAS(ILocalizationResourceManager loc, ICanBusService bus)
        {
            InitializeComponent();
            _loc = loc;
            _bus = bus;

            // live CAN frames
            _bus.FrameReceived += OnFrame;
            _deviceStarted = _bus.IsConnected;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _bus.FrameReceived -= OnFrame;
        }

        void OnFrame(PCAN_USB.Packet p)
        {
            // detect PGN and update connection timeout
            uint pgn = (p.Id >> 8) & 0xFFFF;
            if (pgn == 0xFFBB)
            {
                _isConnected = true;
                ResetTimeout();
            }

            // extract last‐2 hex digits
            var hx = $"0x{p.Id:X}";
            var last = hx.Length >= 2 ? hx[^2..] : hx;
            if (int.TryParse(last, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id))
            {
                _currentId = id.ToString();
                MainThread.BeginInvokeOnMainThread(() =>
                    LatestCanIdLabel1.Text = $"{_loc["CurrentBAS"]} {_currentId}"
                );
            }
        }

        void ResetTimeout()
        {
            _timeout?.Stop();
            _timeout = new Timer(2000) { AutoReset = false };
            _timeout.Elapsed += (_,__) => _isConnected = false;
            _timeout.Start();
        }

        // … your OnLanguageButtonClicked, side‐menu methods exactly as before …

        async void OnSetClicked(object s, EventArgs e)
        {
            var txt = NewCanIdEntry1.Text?.Trim();
            if (!int.TryParse(txt, out var newId) || newId < 0 || newId > 255)
            {
                await DisplayAlert("Error", "Enter 0–255", "OK");
                return;
            }
            ConfirmText1.Text         = $"Set to {newId}";
            SetCanIdView1.IsVisible   = false;
            ConfirmCanIdView1.IsVisible = true;
            _pendingId                = newId.ToString();
        }

        async void OnConfirmClicked(object s, EventArgs e)
        {
            if (string.IsNullOrEmpty(_pendingId)) return;
            if (!int.TryParse(_currentId, out var curr)) curr = 0;
            var cb = (byte)curr;
            var nb = (byte)int.Parse(_pendingId);

            // 4-msg protocol:
            _bus.SendFrame(uint.Parse($"18EA{cb:X2}00", NumberStyles.HexNumber),
                new byte[]{0x00,0xEF,0x00,0x00,0x00,0x00,0x00,0x00}, true);
            _bus.SendFrame(uint.Parse($"18EF{cb:X2}00", NumberStyles.HexNumber),
                new byte[]{0x06,nb,0x00,0xFF,0xFF,0xFF,0xFF,0xFF}, true);
            _bus.SendFrame(uint.Parse($"18EA{cb:X2}00", NumberStyles.HexNumber),
                new byte[]{0x00,0xEF,0x00,0x00,0x00,0x00,0x00,0x00}, true);
            _bus.SendFrame(uint.Parse($"18EF{cb:X2}00", NumberStyles.HexNumber),
                new byte[]{0xFA,0x73,0x61,0x76,0x65,0x00,0x00,0x00}, true);

            ConfirmCanIdView1.IsVisible = false;
            InitialBasView.IsVisible    = true;
        }

        // … your OnCancelConfirmClicked1, OnExitClicked, OnCheckConnectionClicked, etc. …
    }
#endif
}

