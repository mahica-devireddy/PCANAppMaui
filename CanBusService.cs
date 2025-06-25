using System;
using System.ComponentModel;
using System.Timers;
using Microsoft.Maui.Dispatching;
using PCANAppM.Platforms.Windows;
using Peak.Can.Basic;

namespace PCANAppM.Services
{
    public class CanBusService : ICanBusService, INotifyPropertyChanged, IDisposable
    {
        const int PollMs = 500;
        const int DebounceCountNeeded = 3;  // ~1.5s

        readonly Timer _poll;
        readonly IDispatcher _dispatcher;

        int _consecutivePresent;
        int _consecutiveAbsent;

        PCAN_USB? _dev;
        ushort    _h;

        bool _isConn;
        public bool IsConnected
        {
            get => _isConn;
            private set
            {
                if (_isConn == value) return;
                _isConn = value;
                PropertyChanged?.Invoke(this, new(nameof(IsConnected)));
            }
        }

        public string? DeviceName { get; private set; }
        public event Action<PCAN_USB.Packet>? FrameReceived;
        public event PropertyChangedEventHandler? PropertyChanged;

        public CanBusService(IDispatcher disp)
        {
            _dispatcher = disp;
            _poll = new Timer(PollMs) { AutoReset = true };
            _poll.Elapsed += (_,__) => Check();
            _poll.Start();
        }

        void Check()
        {
            var devs = PCAN_USB.GetUSBDevices();
            bool present = devs.Count > 0;

            if (present)
            {
                _consecutivePresent++;
                _consecutiveAbsent = 0;
            }
            else
            {
                _consecutiveAbsent++;
                _consecutivePresent = 0;
            }

            // only initialize if seen present *enough* times in a row
            if (_consecutivePresent >= DebounceCountNeeded && _dev == null)
                TryInit(devs[0]);

            // only teardown if seen absent enough times in a row
            if (_consecutiveAbsent >= DebounceCountNeeded && _dev != null)
                Teardown();
        }

        void TryInit(string name)
        {
            try
            {
                DeviceName = name;
                _h = PCAN_USB.DecodePEAKHandle(name);
                _dev = new PCAN_USB();
                var st = _dev.InitializeCAN(_h, "250 kbit/s", true);
                if (st == TPCANStatus.PCAN_ERROR_OK)
                {
                    _dev.MessageReceived += pkt =>
                        _dispatcher.Dispatch(() => FrameReceived?.Invoke(pkt));
                    IsConnected = true;
                }
                else
                {
                    _dev.Uninitialize();
                    _dev = null;
                }
            }
            catch
            {
                _dev?.Uninitialize();
                _dev = null;
            }
        }

        void Teardown()
        {
            _dev!.Uninitialize();
            _dev = null;
            DeviceName = null;
            IsConnected = false;
        }

        public void SendFrame(uint id, byte[] data, bool extended)
        {
            if (_dev != null && IsConnected)
                _dev.WriteFrame(id, data.Length, data, extended);
        }

        public void Dispose()
        {
            _poll.Stop();
            _poll.Dispose();
            if (_dev != null) _dev.Uninitialize();
        }
    }
}
