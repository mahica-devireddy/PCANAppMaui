#if WINDOWS

using Microsoft.Maui.Dispatching;
using Peak.Can.Basic;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using PCANAppM.Platforms.Windows;
using Windows.Security.Cryptography.Core;

namespace PCANAppM.Services
{
    public class CanBusService : ICanBusService, IDisposable
    {
        const int PollMs = 500;

        readonly IDispatcher _dispatcher;
        readonly CancellationTokenSource _cts = new();

        PCAN_USB? _dev;
        ushort _handle;

        // Receive-loop tokens
        CancellationTokenSource? _rxCts;

        bool _isConnected;

        public event Action? StatusChanged; 
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected == value) return;
                _isConnected = value;
                StatusChanged?.Invoke(); 
                OnPropertyChanged(nameof(IsConnected));
            }
        }

        public string? DeviceName { get; private set; }
        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<PCAN_USB.Packet>? FrameReceived;

        public CanBusService(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            // start monitoring loop
            _ = MonitorDeviceLoop(_cts.Token);
        }

        async Task MonitorDeviceLoop(CancellationToken token)
        {
            bool lastPresent = false; 

            while (!token.IsCancellationRequested)
            {
                var devs = PCAN_USB.GetUSBDevices();
                bool present = devs?.Count > 0;

                if (present && !lastPresent)
                {
                    TryInit(devs![0]);
                    System.Diagnostics.Debug.WriteLine("PCAN Initialized");
                }
                else if (!present && lastPresent)
                {
                    Teardown();
                    System.Diagnostics.Debug.WriteLine("PCAN Uninitialized");
                }
                lastPresent = present;

                await Task.Delay(500, token);
            }
        }

        void TryInit(string name)
        {
            DeviceName = name;
            _handle = PCAN_USB.DecodePEAKHandle(name);

            var usb = new PCAN_USB();
            var status = usb.InitializeCAN(_handle, "250 kbit/s", true); 

            if (status ==TPCANStatus.PCAN_ERROR_OK)
            {
                _dev = usb; 
                IsConnected = true;

                usb.MessageReceived += pkt => 
                    _dispatcher.Dispatch(() => FrameReceived?.Invoke(pkt));
            }
            else
            {
                usb.Uninitialize();
                DeviceName = null;
                IsConnected = false;
            }
        }

        void Teardown()
        {
            _dev?.Uninitialize();
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
            _cts.Cancel();
            StopReceiveLoop();
            if (_dev != null)
                _dev.Uninitialize();
        }

        // --- Background receive loop ---
        void StartReceiveLoop()
        {
            StopReceiveLoop();
            _rxCts = new CancellationTokenSource();
            _ = ReceiveLoop(_rxCts.Token);
        }

        void StopReceiveLoop()
        {
            if (_rxCts != null)
            {
                _rxCts.Cancel();
                _rxCts.Dispose();
                _rxCts = null;
            }
        }

        async Task ReceiveLoop(CancellationToken token)
        {
            if (_dev == null) return;
            while (!token.IsCancellationRequested && _dev != null)
            {
                var result = PCANBasic.Read(_handle, out TPCANMsg msg, out TPCANTimestamp ts);
                if (result == TPCANStatus.PCAN_ERROR_OK)
                {
                    var packet = new PCAN_USB.Packet
                    {
                        Microseconds = (ulong)(ts.micros + 1000 * ts.millis + (ulong)ts.millis_overflow * 0x100000000),
                        Id = msg.ID,
                        Length = msg.LEN,
                        Data = (byte[])msg.DATA.Clone(),
                        IsExtended = (msg.MSGTYPE & TPCANMessageType.PCAN_MESSAGE_EXTENDED) == TPCANMessageType.PCAN_MESSAGE_EXTENDED
                    };
                    _dispatcher.Dispatch(() => FrameReceived?.Invoke(packet));
                }
                await Task.Delay(10, token);
            }
        }
        // --------------------------------

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
#endif
