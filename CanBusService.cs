using Peak.Can.Basic;
using System;
using System.Timers;
using Microsoft.Maui.Dispatching;

namespace PCANAppM.Services
{
    public class CanBusService : ICanBusService, IDisposable
    {
        const int PollIntervalMs = 500;
        readonly Timer _pollTimer;
        PCAN_USB?      _device;
        ushort         _handle;

        public bool   IsConnected { get; private set; }
        public string? DeviceName { get; private set; }
        public event Action<PCAN_USB.Packet>? FrameReceived;

        public CanBusService(IDispatcher dispatcher)
        {
            // Poll for device presence
            _pollTimer = new Timer(PollIntervalMs) { AutoReset = true };
            _pollTimer.Elapsed += (_, _) => CheckAndInit(dispatcher);
            _pollTimer.Start();
        }

        void CheckAndInit(IDispatcher dispatcher)
        {
            var devices = PCAN_USB.GetUSBDevices();
            bool present = devices.Count > 0;

            // Only initialize once, when we first see the hardware
            if (present && !IsConnected)
            {
                try
                {
                    DeviceName = devices[0];
                    _handle    = PCAN_USB.DecodePEAKHandle(DeviceName);
                    _device    = new PCAN_USB();
                    var status = _device.InitializeCAN(_handle, "250 kbit/s", true);
                    if (status == TPCANStatus.PCAN_ERROR_OK)
                    {
                        // Route incoming frames onto the UI thread
                        _device.MessageReceived += pkt =>
                            dispatcher.Dispatch(() => FrameReceived?.Invoke(pkt));
                        IsConnected = true;
                    }
                    else
                    {
                        // initialization failed—clean up immediately
                        _device.Dispose();
                        _device = null;
                    }
                }
                catch
                {
                    _device?.Dispose();
                    _device = null;
                }
            }

            // <no unplug-handling branch at all>
        }

        public void SendFrame(uint id, byte[] data, bool extended)
        {
            if (IsConnected && _device is not null)
                _device.WriteFrame(id, data.Length, data, extended);
        }

        public void Dispose()
        {
            // stop polling
            _pollTimer.Stop();
            _pollTimer.Dispose();

            // if you still want to release on app exit, do it here:
            _device?.Dispose();
            _device = null;
        }
    }
}
