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
        public  static PcanUsbStatusService  Instance => _instance ??= new PcanUsbStatusService();

        readonly Timer  _pollTimer;
        bool            _isConnected;
        string?         _deviceName;
        PCAN_USB?       _pcanUsb;

        public string   BaudRate   { get; } = "250 kbit/s";
        public bool     IsConnected => _isConnected;
        public string?  DeviceName  => _deviceName;
        public PCAN_USB? PcanUsb     => _pcanUsb;
        public event    Action? StatusChanged;

        private PcanUsbStatusService()
        {
            _pollTimer = new Timer(500) { AutoReset = true };
            _pollTimer.Elapsed += (_,__) => PollStatus();
            _pollTimer.Start();
            PollStatus();
        }

        private void PollStatus()
        {
            bool nowConnected;
            string? nowName;

            // ─── PHASE 1: never initialized? use enumeration to find & init ───
            if (_pcanUsb == null)
            {
                var devs = PCAN_USB.GetUSBDevices();
                if (devs.Count > 0)
                {
                    nowName = devs[0];
                    var handle = PCAN_USB.DecodePEAKHandle(nowName);
                    var usb    = new PCAN_USB();
                    var st     = usb.InitializeCAN(handle, BaudRate, true);

                    if (st == TPCANStatus.PCAN_ERROR_OK)
                    {
                        _pcanUsb     = usb;
                        nowConnected = true;
                        _deviceName  = nowName;
                    }
                    else
                    {
                        // init failure
                        usb.Uninitialize();
                        nowConnected = false;
                        nowName      = null;
                    }
                }
                else
                {
                    nowConnected = false;
                    nowName      = null;
                }
            }
            // ─── PHASE 2: already have a handle? poll its "condition" flag ───
            else
            {
                var handle = _pcanUsb.PeakCANHandle;
                var res    = PCANBasic.GetValue(
                                 handle,
                                 TPCANParameter.PCAN_CHANNEL_CONDITION,
                                 out uint condition,
                                 sizeof(uint)
                             );

                bool alive = res == TPCANStatus.PCAN_ERROR_OK
                          && (condition & PCANBasic.PCAN_CHANNEL_AVAILABLE)
                             == PCANBasic.PCAN_CHANNEL_AVAILABLE;

                if (!alive)
                {
                    // sustained unplug
                    _pcanUsb.Uninitialize();
                    _pcanUsb     = null;
                    nowConnected = false;
                    nowName      = null;
                }
                else
                {
                    // still present
                    nowConnected = true;
                    nowName      = _deviceName;
                }
            }

            // ─── fire only on real flip ───
            if (nowConnected != _isConnected)
            {
                _isConnected = nowConnected;
                _deviceName  = nowName;
                StatusChanged?.Invoke();
            }
        }
    }
}
#endif
