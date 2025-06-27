// #if WINDOWS

// using Microsoft.Maui.Dispatching;
// using Peak.Can.Basic;
// using System;
// using System.ComponentModel;
// using System.Threading;
// using System.Threading.Tasks;
// using PCANAppM.Platforms.Windows;

// namespace PCANAppM.Services
// {
//     public class CanBusService : ICanBusService
//     {
//         const int PollMs = 500;
//         const int DebounceNeeded = 6; // Increased for stability

//         readonly IDispatcher _dispatcher;
//         CancellationTokenSource _cts = new();

//         int _presentCount, _absentCount;
//         PCAN_USB? _dev;
//         ushort _handle;

//         // --- Add for receive loop ---
//         CancellationTokenSource? _rxCts;
//         Task? _rxTask;
//         // ---------------------------

//         bool _isConnected;
//         public bool IsConnected
//         {
//             get => _isConnected;
//             private set
//             {
//                 if (_isConnected == value) return;
//                 _isConnected = value;
//                 PropertyChanged?.Invoke(this, new(nameof(IsConnected)));
//             }
//         }

//         public string? DeviceName { get; private set; }
//         public event PropertyChangedEventHandler? PropertyChanged;
//         public event Action<PCAN_USB.Packet>? FrameReceived;

//         public CanBusService(IDispatcher disp)
//         {
//             _dispatcher = disp;
//             StartDeviceMonitor();
//         }

//         private void StartDeviceMonitor()
//         {
//             _cts = new CancellationTokenSource();
//             Task.Run(() => MonitorDeviceLoop(_cts.Token));
//         }

//         private async Task MonitorDeviceLoop(CancellationToken token)
//         {
//             while (!token.IsCancellationRequested)
//             {
//                 var devs = PCAN_USB.GetUSBDevices();
//                 bool present = devs != null && devs.Count > 0;

//                 if (present) { _presentCount++; _absentCount = 0; }
//                 else        { _absentCount++; _presentCount = 0; }

//                 // on stable connect:
//                 if (_presentCount >= DebounceNeeded && _dev == null)
//                     _dispatcher.Dispatch(() => TryInit(devs[0]));

//                 // on stable disconnect:
//                 if (_absentCount >= DebounceNeeded && _dev != null)
//                     _dispatcher.Dispatch(Teardown);

//                 await Task.Delay(PollMs, token);
//             }
//         }

//         void TryInit(string name)
//         {
//             try
//             {
//                 DeviceName = name;
//                 _handle = PCAN_USB.DecodePEAKHandle(name);
//                 var usb = new PCAN_USB();
//                 var st  = usb.InitializeCAN(_handle, "250 kbit/s", false); // false: don't start WinForms read thread

//                 if (st == TPCANStatus.PCAN_ERROR_OK)
//                 {
//                     _dev = usb;
//                     IsConnected = true;
//                     StartReceiveLoop(); // <-- Start background receive loop
//                 }
//                 else
//                 {
//                     usb.Uninitialize();
//                     DeviceName = null;
//                     IsConnected = false;
//                 }
//             }
//             catch
//             {
//                 _dev?.Uninitialize();
//                 _dev = null;
//                 DeviceName = null;
//                 IsConnected = false;
//             }
//         }

//         void Teardown()
//         {
//             StopReceiveLoop(); // <-- Stop background receive loop
//             _dev?.Uninitialize();
//             _dev = null;
//             DeviceName = null;
//             IsConnected = false;
//         }

//         public void SendFrame(uint id, byte[] data, bool extended)
//         {
//             if (_dev != null && IsConnected)
//                 _dev.WriteFrame(id, data.Length, data, extended);
//         }

//         public void Dispose()
//         {
//             _cts.Cancel();
//             StopReceiveLoop();
//             if (_dev != null) _dev.Uninitialize();
//         }

//         // --- CAN Receive Loop Implementation ---
//         private void StartReceiveLoop()
//         {
//             StopReceiveLoop();
//             _rxCts = new CancellationTokenSource();
//             _rxTask = Task.Run(() => ReceiveLoop(_rxCts.Token));
//         }

//         private void StopReceiveLoop()
//         {
//             if (_rxCts != null)
//             {
//                 _rxCts.Cancel();
//                 _rxCts.Dispose();
//                 _rxCts = null;
//             }
//             _rxTask = null;
//         }

//         private async Task ReceiveLoop(CancellationToken token)
//         {
//             if (_dev == null) return;

//             var handle = _handle;
//             while (!token.IsCancellationRequested && _dev != null)
//             {
//                 // Try to read a CAN message
//                 TPCANMsg msg;
//                 TPCANTimestamp ts;
//                 var result = PCANBasic.Read(handle, out msg, out ts);

//                 if (result == TPCANStatus.PCAN_ERROR_OK)
//                 {
//                     // Build a Packet and raise FrameReceived
//                     var packet = new PCAN_USB.Packet
//                     {
//                         Microseconds = Convert.ToUInt64(ts.micros + 1000 * ts.millis + 0x100000000 * 1000 * ts.millis_overflow),
//                         Id = msg.ID,
//                         Length = msg.LEN,
//                         Data = (byte[])msg.DATA.Clone(),
//                         IsExtended = (msg.MSGTYPE & TPCANMessageType.PCAN_MESSAGE_EXTENDED) == TPCANMessageType.PCAN_MESSAGE_EXTENDED
//                     };

//                     // Raise event on UI thread
//                     _dispatcher.Dispatch(() => FrameReceived?.Invoke(packet));
//                 }
//                 else if (result == TPCANStatus.PCAN_ERROR_QRCVEMPTY)
//                 {
//                     // No message, wait a bit
//                     await Task.Delay(10, token);
//                 }
//                 else
//                 {
//                     // Error, optionally handle/log
//                     await Task.Delay(10, token);
//                 }
//             }
//         }
//         // ----------------------------------------
//     }
// }
// #endif

#if WINDOWS

using Microsoft.Maui.Dispatching;
using Peak.Can.Basic;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using PCANAppM.Platforms.Windows;

namespace PCANAppM.Services
{
    public class CanBusService : ICanBusService, IDisposable
    {
        const int PollMs = 500;
        const int DebounceNeeded = 6; // stability threshold

        readonly IDispatcher _dispatcher;
        readonly CancellationTokenSource _cts = new();

        int _presentCount, _absentCount;
        PCAN_USB? _dev;
        ushort    _handle;

        // Receive-loop tokens
        CancellationTokenSource? _rxCts;

        bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected == value) return;
                _isConnected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));
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
            while (!token.IsCancellationRequested)
            {
                var devs = PCAN_USB.GetUSBDevices();
                bool present = devs?.Count > 0;

                if (present && _dev == null)
                {
                    // stable present => initialize
                    _presentCount++; _absentCount = 0;
                    if (_presentCount >= DebounceNeeded)
                        TryInit(devs![0]);
                }
                else if (!present && _dev != null)
                {
                    // stable absent => teardown
                    _absentCount++; _presentCount = 0;
                    if (_absentCount >= DebounceNeeded)
                        Teardown();
                }
                else
                {
                    // reset counters if state unchanged
                    if (present) _absentCount = 0;
                    else         _presentCount = 0;
                }

                await Task.Delay(PollMs, token);
            }
        }

        void TryInit(string name)
        {
            try
            {
                DeviceName = name;
                _handle = PCAN_USB.DecodePEAKHandle(name);
                var usb = new PCAN_USB();
                var st  = usb.InitializeCAN(_handle, "250 kbit/s", false);

                if (st == TPCANStatus.PCAN_ERROR_OK)
                {
                    _dev = usb;
                    IsConnected = true;
                    StartReceiveLoop();
                }
                else
                {
                    usb.Uninitialize();
                    DeviceName = null;
                    IsConnected = false;
                }
            }
            catch
            {
                _dev?.Uninitialize();
                _dev = null;
                DeviceName = null;
                IsConnected = false;
            }
        }

        void Teardown()
        {
            StopReceiveLoop();
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
                        Id            = msg.ID,
                        Length        = msg.LEN,
                        Data          = (byte[])msg.DATA.Clone(),
                        IsExtended    = (msg.MSGTYPE & TPCANMessageType.PCAN_MESSAGE_EXTENDED) == TPCANMessageType.PCAN_MESSAGE_EXTENDED
                    };
                    _dispatcher.Dispatch(() => FrameReceived?.Invoke(packet));
                }
                await Task.Delay(10, token);
            }
        }
        // --------------------------------
    }
}
#endif

