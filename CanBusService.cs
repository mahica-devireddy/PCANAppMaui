#if WINDOWS

using System;
using System.Text;
using Timer = System.Timers.Timer;
using Peak.Can.Basic;
using TPCANHandle = Peak.Can.Basic.TPCANHandle;
using TPCANStatus = Peak.Can.Basic.TPCANStatus;
using TPCANBaudrate = Peak.Can.Basic.TPCANBaudrate;
using PCANAppM.Platforms.Windows;

namespace PCANAppM.Services
{
    public class CanBusService : ICanBusService, IDisposable
    {
        private readonly Timer _timer;
        private bool _isConnected;
        private bool _isInitialized;
        private string _deviceName = "";

        public event EventHandler<bool> ConnectionStatusChanged = delegate { };
        public bool IsConnected => _isConnected;
        public string DeviceName => _deviceName;

        public CanBusService()
        {
            _timer = new Timer(1000);
            _timer.AutoReset = true;
            _timer.Elapsed += (_, __) => CheckStatus();
        }

        public void StartMonitoring() => _timer.Start();
        public void StopMonitoring() => _timer.Stop();

        private void CheckStatus()
        {
            var devices = PCAN_USB.GetUSBDevices();
            bool nowPresent = devices != null && devices.Count > 0;

            // 1) If it just appeared, initialize at 250 kbit/s
            if (nowPresent && !_isInitialized)
            {
                var initResult = PCANBasic.Initialize(
                    PCANBasic.PCAN_USBBUS1,
                    TPCANBaudrate.PCAN_BAUD_250K
                );

                _isInitialized = (initResult == TPCANStatus.PCAN_ERROR_OK);
                if (!_isInitialized)
                {
                    // failed to init — treat as not present
                    nowPresent = false;
                }
            }
            // 2) If it just disappeared, uninitialize
            else if (!nowPresent && _isInitialized)
            {
                PCANBasic.Uninitialize(PCANBasic.PCAN_USBBUS1);
                _isInitialized = false;
            }

            // 3) Fire event only on actual connection‐state change
            if (nowPresent != _isConnected)
            {
                _isConnected = nowPresent;
                _deviceName = _isConnected ? devices[0] : string.Empty;
                ConnectionStatusChanged?.Invoke(this, _isConnected);
            }
        }

        public TPCANStatus ReadMessages(Action<TPCANMsg, TPCANTimestamp> onMessageReceived)
        {
            if (!_isInitialized)
                return TPCANStatus.PCAN_ERROR_INITIALIZE;

            TPCANStatus status;
            do
            {
                status = PCANBasic.Read(
                    PCANBasic.PCAN_USBBUS1,
                    out var msg,
                    out var ts
                );

                if (status == TPCANStatus.PCAN_ERROR_OK)
                    onMessageReceived?.Invoke(msg, ts);
                else if (status != TPCANStatus.PCAN_ERROR_QRCVEMPTY)
                    return status;
            }
            while (status == TPCANStatus.PCAN_ERROR_OK);

            return TPCANStatus.PCAN_ERROR_OK;
        }

        public void Dispose()
        {
            StopMonitoring();
            _timer.Dispose();

            if (_isInitialized)
                PCANBasic.Uninitialize(PCANBasic.PCAN_USBBUS1);
        }
    }
}
#endif
