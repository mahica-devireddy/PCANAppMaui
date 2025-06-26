using Microsoft.Maui.Dispatching;
using Peak.Can.Basic;
using System;
using System.ComponentModel;
using System.Timers;
using PCANAppM.Platforms.Windows;

namespace PCANAppM.Services
{
#if WINDOWS
    public class CanBusService : ICanBusService
    {
        const int PollMs = 500;
        const int DebounceNeeded = 3;

        readonly Timer _poll;
        readonly IDispatcher _dispatcher;

        int _presentCount, _absentCount;
        PCAN_USB? _dev;
        ushort _handle;

        bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected == value) return;
                _isConnected = value;
                PropertyChanged?.Invoke(this, new(nameof(IsConnected)));
            }
        }

        public string? DeviceName { get; private set; }
        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<PCAN_USB.Packet>? FrameReceived;

        public CanBusService(IDispatcher disp)
        {
            _dispatcher = disp;
            _poll = new Timer(PollMs) { AutoReset = true };
            _poll.Elapsed += (_,_) => Check();
            _poll.Start();
        }

        void Check()
        {
            var devs = PCAN_USB.GetUSBDevices();
            bool present = devs.Count > 0;

            if (present) { _presentCount++; _absentCount = 0; }
            else        { _absentCount++; _presentCount = 0; }

            // on stable connect:
            if (_presentCount >= DebounceNeeded && _dev == null)
                TryInit(devs[0]);

            // on stable disconnect:
            if (_absentCount  >= DebounceNeeded && _dev != null)
                Teardown();
        }

        void TryInit(string name)
        {
            try
            {
                DeviceName = name;
                _handle = PCAN_USB.DecodePEAKHandle(name);
                var usb = new PCAN_USB();
                var st  = usb.InitializeCAN(_handle, "250 kbit/s", true);
                if (st == TPCANStatus.PCAN_ERROR_OK)
                {
                    _dev = usb;
                    usb.MessageReceived += pkt => 
                        _dispatcher.Dispatch(() => FrameReceived?.Invoke(pkt));
                    IsConnected = true;
                }
                else
                {
                    usb.Uninitialize();
                }
            }
            catch
            {
                _dev?.Uninitialize();
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
#endif
}
