using Microsoft.Maui.Dispatching;
using Peak.Can.Basic;
using System;
using System.Timers;

#if WINDOWS
using PCANAppM.Platforms.Windows;
#endif

namespace PCANAppM.Services
{
#if WINDOWS
    public class PcanUsbStatusService
    {
        private static PcanUsbStatusService? _instance;
        public static PcanUsbStatusService Instance => _instance ??= new PcanUsbStatusService();

        private readonly System.Timers.Timer _pollTimer;
        private bool _isDevicePresent;
        private string? _deviceName;
        private PCAN_USB? _pcanUsb;

        public bool IsConnected => _isDevicePresent;
        public string? DeviceName => _deviceName;
        public PCAN_USB? PcanUsb => _pcanUsb;
        public event Action? StatusChanged;

        private PcanUsbStatusService()
        {
            _pollTimer = new System.Timers.Timer(1000) { AutoReset = true };
            _pollTimer.Elapsed += (_, _) => PollStatus();
            _pollTimer.Start();
            PollStatus();
        }

        private void PollStatus()
        {
            var devices = PCAN_USB.GetUSBDevices();
            bool devicePresent = devices != null && devices.Count > 0;
            string? foundDeviceName = devicePresent ? devices[0] : null;

            // Device plugged in
            if (devicePresent)
            {
                // Only initialize if not already done
                if (_pcanUsb == null)
                {
                    _deviceName = foundDeviceName!;
                    _pcanUsb = new PCAN_USB();
                    var handle = PCAN_USB.DecodePEAKHandle(_deviceName);
                    // Try to initialize, but even if it fails, we still consider the device "present"
                    _pcanUsb.InitializeCAN(handle, "250 kbit/s", true);
                }
            }
            // Device unplugged
            else
            {
                if (_pcanUsb != null)
                {
                    _pcanUsb.Uninitialize();
                    _pcanUsb = null;
                }
                _deviceName = null;
            }

            // Only fire event if device presence changed
            if (devicePresent != _isDevicePresent || _deviceName != foundDeviceName)
            {
                _isDevicePresent = devicePresent;
                _deviceName = foundDeviceName;
                StatusChanged?.Invoke();
            }
        }
    }
#endif
}
