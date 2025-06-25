using Peak.Can.Basic;
using System;
using System.Timers;
using Microsoft.Maui.Dispatching;

namespace PCANAppM.Services
{
    public class CanBusService : ICanBusService, IDisposable
    {
        const int PollIntervalMs = 500;
        readonly Timer      _pollTimer;
        PCAN_USB?           _device;
        ushort              _handle;

        public bool   IsConnected { get; private set; }
        public string? DeviceName { get; private set; }
        public event Action<PCAN_USB.Packet>? FrameReceived;

        public CanBusService(IDispatcher dispatcher)
        {
            // Poll for hot-plug every half second
            _pollTimer = new Timer(PollIntervalMs) { AutoReset = true };
            _pollTimer.Elapsed += (_, _) => CheckDevice(dispatcher);
            _pollTimer.Start();
        }

        void CheckDevice(IDispatcher dispatcher)
        {
            var devices = PCAN_USB.GetUSBDevices();
            bool present = devices.Count > 0;

            // If unplugged, tear down
            if (!present && IsConnected)
            {
                Teardown();
            }

            // If newly plugged, initialize once
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
                        // Route incoming frames to the UI thread
                        _device.MessageReceived += pkt =>
                            dispatcher.Dispatch(() => FrameReceived?.Invoke(pkt));
                        IsConnected = true;
                    }
                    else
                    {
                        Teardown();
                    }
                }
                catch
                {
                    Teardown();
                }
            }
        }

        void Teardown()
        {
            IsConnected = false;
            _device?.Dispose();
            _device = null;
        }

        public void SendFrame(uint id, byte[] data, bool extended)
        {
            if (IsConnected && _device is not null)
            {
                _device.WriteFrame(id, data.Length, data, extended);
            }
        }

        public void Dispose()
        {
            _pollTimer.Stop();
            _pollTimer.Dispose();
            Teardown();
        }
    }
}
