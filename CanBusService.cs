public class CanBusService : ICanBusService, INotifyPropertyChanged, IDisposable
{
    const int PollMs = 500;
    const int DebounceForUnplug = 3;

    readonly Timer _poll;
    readonly IDispatcher _dispatcher;

    PCAN_USB? _dev;
    ushort    _h;
    int       _absentCount;

    bool _isConn;
    public bool IsConnected { /* same as before */ }
    public string? DeviceName { get; private set; }
    public event Action<PCAN_USB.Packet>? FrameReceived;
    public event PropertyChangedEventHandler? PropertyChanged;

    public CanBusService(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
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
            _absentCount = 0;
            if (_dev == null)
                TryInit(devs[0]);
        }
        else
        {
            _absentCount++;
            if (_absentCount >= DebounceForUnplug && _dev != null)
                Teardown();
        }
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
                _dev.MessageReceived += OnPlatformMessageReceived;
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

    void OnPlatformMessageReceived(PCAN_USB.Packet pkt)
    {
        // won’t fire after Teardown(), because we remove this handler there:
        _dispatcher.Dispatch(() => FrameReceived?.Invoke(pkt));
    }

    void Teardown()
    {
        if (_dev != null)
        {
            _dev.MessageReceived -= OnPlatformMessageReceived;
            _dev.Uninitialize();
        }
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
        if (_dev != null)
            _dev.Uninitialize();
    }
}
