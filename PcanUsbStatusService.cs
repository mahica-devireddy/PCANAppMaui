#if WINDOWS
using System;
using System.Timers;
using Peak.Can.Basic;
using PCANAppM.Platforms.Windows;

namespace PCANAppM.Services
{
    public class PcanUsbStatusService
    {
        private static PcanUsbStatusService? _instance;
        public static PcanUsbStatusService Instance => _instance ??= new PcanUsbStatusService();

        // poll every half-second
        readonly Timer    _pollTimer;
        bool              _isDevicePresent;
        string?           _deviceName;
        ushort            _handle;
        PCAN_USB?         _pcanUsb;

        /// <summary>True if physically present right now</summary>
        public bool IsConnected   => _isDevicePresent;
        /// <summary>The first-found device name, or null</summary>
        public string? DeviceName => _deviceName;

        /// <summary>Raised whenever IsConnected flips</summary>
        public event Action? StatusChanged;

        private PcanUsbStatusService()
        {
            _pollTimer = new Timer(500) { AutoReset = true };
            _pollTimer.Elapsed += (_,_) => PollStatus();
            _pollTimer.Start();

            // do an immediate check
            PollStatus();
        }

        private void PollStatus()
        {
            // 1) If never initialized, look for the stick once
            if (_pcanUsb == null)
            {
                var list = PCAN_USB.GetUSBDevices();
                if (list != null && list.Count > 0)
                {
                    _deviceName = list[0];
                    _handle     = PCAN_USB.DecodePEAKHandle(_deviceName);
                    _pcanUsb    = new PCAN_USB();
                    // we don't care if it fails—you still consider "present"
                    _pcanUsb.InitializeCAN(_handle, "250 kbit/s", true);
                    UpdateStatus(true);
                    return;
                }
                // not found yet
                UpdateStatus(false);
                return;
            }

            // 2) Already opened: poll the channel condition flag
            var result = PCANBasic.GetValue(
                _handle,
                TPCANParameter.PCAN_CHANNEL_CONDITION,
                out uint condition,
                sizeof(uint)
            );

            bool alive = result == TPCANStatus.PCAN_ERROR_OK
                      && (condition & PCANBasic.PCAN_CHANNEL_AVAILABLE)
                         == PCANBasic.PCAN_CHANNEL_AVAILABLE;

            UpdateStatus(alive);
        }

        private void UpdateStatus(bool present)
        {
            if (present != _isDevicePresent)
            {
                _isDevicePresent = present;
                StatusChanged?.Invoke();
            }
        }
    }
}
#endif
