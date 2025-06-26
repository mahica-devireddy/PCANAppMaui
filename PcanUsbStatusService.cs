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
        private bool _isConnected;
        private string? _deviceName;
        private PCAN_USB? _pcanUsb;

        // Add this property for the baud rate
        public string BaudRate { get; } = "250 kbit/s";

        public bool IsConnected => _isConnected;
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
            bool connected = devices != null && devices.Count > 0;
            string? name = connected ? devices[0] : null;

            if (connected && _pcanUsb == null)
            {
                _pcanUsb = new PCAN_USB();
            }

            if (connected != _isConnected || name != _deviceName)
            {
                _isConnected = connected;
                _deviceName = name;
                StatusChanged?.Invoke();
            }
        }
    }
#endif
}
