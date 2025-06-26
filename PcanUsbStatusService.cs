#if WINDOWS
using Peak.Can.Basic;
using PCANAppM.Platforms.Windows;
using System;
using System.Timers;

namespace PCANAppM.Services
{
    public class PcanUsbStatusService
    {
        private static PcanUsbStatusService? _instance;
        public static PcanUsbStatusService Instance => _instance ??= new PcanUsbStatusService();

        readonly Timer    _pollTimer;
        bool              _isConnected;
        string?           _deviceName;
        PCAN_USB?         _pcanUsb;

        public string    BaudRate    { get; } = "250 kbit/s";
        public bool      IsConnected => _isConnected;
        public string?   DeviceName  => _deviceName;
        public PCAN_USB? PcanUsb     => _pcanUsb;
        public event    Action?     StatusChanged;

        private PcanUsbStatusService()
        {
            _pollTimer = new Timer(500) { AutoReset = true };
            _pollTimer.Elapsed += (_,__) => PollStatus();
            _pollTimer.Start();
            PollStatus();  // initial
        }

        private void PollStatus()
        {
            var devs      = PCAN_USB.GetUSBDevices();
            bool nowCon   = devs.Count > 0;
            string? nowNm = nowCon ? devs[0] : null;

            // ─── plug-in event ───
            if (nowCon && _pcanUsb == null)
            {
                var handle = PCAN_USB.DecodePEAKHandle(nowNm!);
                var usb    = new PCAN_USB();
                var st     = usb.InitializeCAN(handle, BaudRate, true);

                if (st == TPCANStatus.PCAN_ERROR_OK)
                {
                    _pcanUsb     = usb;
                    _deviceName  = nowNm;
                    _isConnected = true;
                    StatusChanged?.Invoke();
                }
                else
                {
                    usb.Uninitialize();
                }

                return;
            }

            // ─── unplug event ───
            if (!nowCon && _pcanUsb != null)
            {
                _pcanUsb.Uninitialize();
                _pcanUsb    = null;
                _deviceName = null;
                _isConnected = false;
                StatusChanged?.Invoke();
                return;
            }

            // ─── neither plugged nor just unplugged: do nothing ───
        }
    }
}
#endif
