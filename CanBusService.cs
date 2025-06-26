#if WINDOWS
using Peak.Can.Basic;
using System;
using Timer = System.Timers.Timer;
using PCANAppM.Platforms.Windows;

namespace PCANAppM.Services
{
    public class CanBusService : ICanBusService
    {
        const double PollIntervalMs = 500;
        readonly Timer _timer;
        PCAN_USB?      _pcan;
        bool           _isConnected;
        string?        _deviceName;

        /// <summary>True when stick is physically plugged in & initialized.</summary>
        public bool IsConnected => _isConnected;
        public string? DeviceName => _deviceName;

        /// <summary>Fires whenever IsConnected flips.</summary>
        public event Action? StatusChanged;

        /// <summary>Fires for every incoming CAN frame (on UI thread).</summary>
        public event Action<PCAN_USB.Packet>? FrameReceived;

        public CanBusService()
        {
            _timer = new Timer(PollIntervalMs) { AutoReset = true };
            _timer.Elapsed += (_,__) => Poll();
            _timer.Start();
            Poll(); // initial check
        }

        void Poll()
        {
            var devs     = PCAN_USB.GetUSBDevices();
            bool present = devs.Count > 0;
            string? name = present ? devs[0] : null;

            if (present && _pcan == null)
            {
                // first-time plug in → init once
                _pcan = new PCAN_USB();
                _pcan.MessageReceived += pkt => Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                    FrameReceived?.Invoke(pkt)
                );
                var handle = PCAN_USB.DecodePEAKHandle(name!);
                _pcan.InitializeCAN(handle, "250 kbit/s", true);
            }
            else if (!present && _pcan != null)
            {
                // sustained unplug → tear down
                _pcan.Uninitialize();
                _pcan = null;
            }

            // fire only on real flip or name change
            if (present != _isConnected || name != _deviceName)
            {
                _isConnected = present;
                _deviceName  = name;
                StatusChanged?.Invoke();
            }
        }

        /// <summary>Send a CAN frame over the shared bus.</summary>
        public void SendFrame(uint id, byte[] data, bool extended)
        {
            if (_pcan != null && _isConnected)
                _pcan.WriteFrame(id, data.Length, data, extended);
        }
    }
}
#endif
