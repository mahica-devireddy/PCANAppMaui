#if WINDOWS

using System;
using System.Timers;
using Peak.Can.Basic;
using TPCANHandle = Peak.Can.Basic.TPCANHandle;
using TPCANStatus = Peak.Can.Basic.TPCANStatus;
using TPCANBaudrate = Peak.Can.Basic.TPCANBaudrate;
using PCANAppM.Platforms.Windows;

namespace PCANAppM.Services
{
    public class CanBusService : ICanBusService, IDisposable
    {
        private const TPCANHandle Channel = PCANBasic.PCAN_USBBUS1;
        private readonly Timer _timer;
        private bool _isInitialized;
        private bool _isConnected;
        private string _deviceName = "";

        public event EventHandler<bool> ConnectionStatusChanged = delegate { };
        public bool IsConnected => _isConnected;
        public string DeviceName => _deviceName;

        public CanBusService()
        {
            // 1Hz poll to detect plug-in, then watch driver status
            _timer = new Timer(1000) { AutoReset = true };
            _timer.Elapsed += (_, __) => CheckStatus();
        }

        public void StartMonitoring()
        {
            _timer.Start();
        }

        public void StopMonitoring()
        {
            _timer.Stop();

            if (_isInitialized)
            {
                PCANBasic.Uninitialize(Channel);
                _isInitialized = false;
            }
        }

        private void CheckStatus()
        {
            // Step 1: if not yet initialized, look for the USB device
            if (!_isInitialized)
            {
                var devs = PCAN_USB.GetUSBDevices();
                if (devs != null && devs.Count > 0)
                {
                    // remember name once
                    _deviceName = devs[0];

                    // try to bring up the channel at 250k
                    var init = PCANBasic.Initialize(Channel, TPCANBaudrate.PCAN_BAUD_250K);
                    if (init == TPCANStatus.PCAN_ERROR_OK)
                        _isInitialized = true;
                }
            }

            // Step 2: if initialized, ask the driver if link is still good
            bool nowConnected = false;
            if (_isInitialized)
            {
                var st = PCANBasic.GetStatus(Channel);
                nowConnected = (st == TPCANStatus.PCAN_ERROR_OK);
            }

            // Step 3: fire event only when real change happens
            if (nowConnected != _isConnected)
            {
                _isConnected = nowConnected;
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
                status = PCANBasic.Read(Channel, out var msg, out var ts);
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
        }
    }
}
#endif
