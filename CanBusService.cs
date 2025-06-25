using System;
using System.ComponentModel;
using System.Timers;
using Microsoft.Maui.Dispatching;
using PCANAppM.Platforms.Windows;   // ← your PCAN_USB
using Peak.Can.Basic;

namespace PCANAppM.Services
{
    public class CanBusService : ICanBusService, INotifyPropertyChanged, IDisposable
    {
        const int PollIntervalMs = 500;
        readonly Timer _pollTimer;
        readonly IDispatcher _dispatcher;

        PCAN_USB? _device;
        ushort    _handle;

        bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));
                }
            }
        }

        public string? DeviceName { get; private set; }

        public event Action<PCAN_USB.Packet>? FrameReceived;
        public event PropertyChangedEventHandler? PropertyChanged;

        public CanBusService(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _pollTimer = new Timer(PollIntervalMs) { AutoReset = true };
            _pollTimer.Elapsed += (s,e) => CheckDevice();
            _pollTimer.Start();
        }

        void CheckDevice()
        {
            var devices = PCAN_USB.GetUSBDevices();
            bool present = devices.Count > 0;

            // 1) If unplugged now but was connected before => tear down
            if (!present && _device != null)
            {
                _device.Uninitialize();
                _device = null;
                DeviceName = null;
                IsConnected = false;
            }

            // 2) If plugged in now and not yet initialized => init
            if (present && _device == null)
            {
                try
                {
                    DeviceName = devices[0];
                    _handle    = PCAN_USB.DecodePEAKHandle(DeviceName);
                    _device    = new PCAN_USB();
                    // true=>enable background reading + MessageReceived events
                    var status = _device.InitializeCAN(_handle, "250 kbit/s", true);

                    if (status == TPCANStatus.PCAN_ERROR_OK)
                    {
                        // forward incoming packets on UI thread
                        _device.MessageReceived += pkt =>
                            _dispatcher.Dispatch(() => FrameReceived?.Invoke(pkt));
                        IsConnected = true;
                    }
                    else
                    {
                        // init failed: clean up
                        _device.Uninitialize();
                        _device = null;
                    }
                }
                catch
                {
                    _device?.Uninitialize();
                    _device = null;
                }
            }
        }

        public void SendFrame(uint id, byte[] data, bool extended)
        {
            if (IsConnected && _device != null)
                _device.WriteFrame(id, data.Length, data, extended);
        }

        public void Dispose()
        {
            _pollTimer.Stop();
            _pollTimer.Dispose();
            if (_device != null)
            {
                _device.Uninitialize();
                _device = null;
            }
        }
    }
}
