#if WINDOWS
using Microsoft.Maui.Dispatching;
using Peak.Can.Basic;
using System;
using System.Timers;
using PCANAppM.Platforms.Windows;

namespace PCANAppM.Services
{
    public class CanBusService : ICanBusService, IDisposable
    {
        const int PollIntervalMs = 500;
        const int DebounceThreshold = 3;

        readonly Timer _pollTimer;
        int _presentCount, _absentCount;

        PCAN_USB? _device;
        ushort    _handle;
        bool      _isConnected;
        string?   _deviceName;

        public bool IsConnected   => _isConnected;
        public string? DeviceName => _deviceName;

        public event Action? StatusChanged;
        public event Action<PCAN_USB.Packet>? FrameReceived;

        public CanBusService(IDispatcher dispatcher)
        {
            // poll half-second
            _pollTimer = new Timer(PollIntervalMs) { AutoReset = true };
            _pollTimer.Elapsed += (_,__) => Poll();
            _pollTimer.Start();
            // immediate first pass
            Poll();
        }

        void Poll()
        {
            var list = PCAN_USB.GetUSBDevices();
            bool present = list.Count > 0;

            if (present) { _presentCount++; _absentCount = 0; }
            else         { _absentCount++; _presentCount = 0; }

            // init only after seen present X times
            if (_presentCount >= DebounceThreshold && _device == null)
                TryInit(list[0]);

            // teardown only after seen absent X times
            if (_absentCount >= DebounceThreshold && _device != null)
                Teardown();
        }

        void TryInit(string name)
        {
            try
            {
                var handle = PCAN_USB.DecodePEAKHandle(name);
                var dev = new PCAN_USB();
                var st = dev.InitializeCAN(handle, "250 kbit/s", true);
                if (st == TPCANStatus.PCAN_ERROR_OK)
                {
                    _device      = dev;
                    _handle      = handle;
                    _deviceName  = name;
                    _isConnected = true;
                    // marshal incoming frames to UI thread
                    dev.MessageReceived += pkt => FrameReceived?.Invoke(pkt);
                    StatusChanged?.Invoke();
                }
                else
                {
                    dev.Uninitialize();
                }
            }
            catch
            {
                // swallow
            }
        }

        void Teardown()
        {
            _device!.Uninitialize();
            _device = null;
            _deviceName = null;
            _isConnected = false;
            StatusChanged?.Invoke();
        }

        public void SendFrame(uint id, byte[] data, bool extended)
        {
            if (_device != null && _isConnected)
                _device.WriteFrame(id, data.Length, data, extended);
        }

        public void Dispose()
        {
            _pollTimer.Stop();
            _pollTimer.Dispose();
            if (_device != null)
                _device.Uninitialize();
        }
    }
}
#endif
