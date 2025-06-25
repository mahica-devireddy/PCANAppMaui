namespace PCANAppM.Services
{
#if WINDOWS
    public class CanBusService : ICanBusService, INotifyPropertyChanged, IDisposable
    {
        const int HealthPollMs = 500;

        readonly Timer      _healthTimer;
        readonly IDispatcher _dispatcher;

        PCAN_USB? _dev;
        ushort    _handle;

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

        public event Action<PCAN_USB.Packet>? FrameReceived;
        public event PropertyChangedEventHandler? PropertyChanged;

        public CanBusService(IDispatcher dispatcher)
        {
            _dispatcher   = dispatcher;
            _healthTimer  = new Timer(HealthPollMs) { AutoReset = true };
            _healthTimer.Elapsed += (_,__) => HealthCheck();
            _healthTimer.Start();
        }

        void HealthCheck()
        {
            // 1) If not yet initialized, look for the stick and init once
            if (_dev == null)
            {
                var devices = PCAN_USB.GetUSBDevices();
                if (devices.Count > 0)
                {
                    DeviceName = devices[0];
                    _handle    = PCAN_USB.DecodePEAKHandle(DeviceName);
                    _dev       = new PCAN_USB();
                    var status = _dev.InitializeCAN(_handle, "250 kbit/s", true);
                    if (status == TPCANStatus.PCAN_ERROR_OK)
                    {
                        _dev.MessageReceived += pkt =>
                            _dispatcher.Dispatch(() => FrameReceived?.Invoke(pkt));
                        IsConnected = true;
                    }
                    else
                    {
                        // initialization failed: clean up
                        _dev.Uninitialize();
                        _dev = null;
                    }
                }
                return;
            }

            // 2) If already initialized, just check channel condition for live status
            var ret = PCANBasic.GetValue(
                _handle,
                TPCANParameter.PCAN_CHANNEL_CONDITION,
                out uint condition,
                sizeof(uint)
            );
            bool alive = ret == TPCANStatus.PCAN_ERROR_OK
                      && (condition & PCANBasic.PCAN_CHANNEL_AVAILABLE)
                         == PCANBasic.PCAN_CHANNEL_AVAILABLE;
            IsConnected = alive;
        }

        public void SendFrame(uint id, byte[] data, bool extended)
        {
            if (_dev != null && IsConnected)
                _dev.WriteFrame(id, data.Length, data, extended);
        }

        public void Dispose()
        {
            _healthTimer.Stop();
            _healthTimer.Dispose();
            if (_dev != null)
                _dev.Uninitialize();
        }
    }
    #endif
}
