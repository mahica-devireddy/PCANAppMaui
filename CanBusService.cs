#if WINDOWS

using System;
using System.Text;
using Timer = System.Timers.Timer;
using Peak.Can.Basic;
using TPCANHandle = System.UInt16;
using TPCANStatus = Peak.Can.Basic.TPCANStatus;
using TPCANParameter = Peak.Can.Basic.TPCANParameter;
using PCANAppM.Platforms.Windows;

namespace PCANAppM.Services
{
    public class CanBusService : ICanBusService, IDisposable
    {
        private readonly Timer _timer;
        private bool _isConnected;
        private string _deviceName = "";

        public event EventHandler<bool> ConnectionStatusChanged = delegate { };
        public bool IsConnected => _isConnected;
        public string DeviceName => _deviceName;

        public CanBusService()
        {
            _timer = new Timer(1000);
            _timer.Elapsed += (_, __) => CheckStatus();
        }

        public void StartMonitoring() => _timer.Start();
        public void StopMonitoring() => _timer.Stop();

        private void CheckStatus()
        {
            var devices = PCAN_USB.GetUSBDevices();
            bool nowConnected = devices != null && devices.Count > 0;

            if (nowConnected != _isConnected)
            {
                _isConnected = nowConnected;
                _deviceName = _isConnected ? devices[0] : "";
                ConnectionStatusChanged?.Invoke(this, _isConnected);
            }
        } 

        public TPCANStatus ReadMessages(Action<TPCANMsg, TPCANTimestamp> onMessageReceived)
        {
            TPCANMsg canMsg;
            TPCANTimestamp canTimestamp;
            TPCANHandle handle = PCANBasic.PCAN_USBBUS1;
            TPCANStatus status;

            // Read all messages in the receive queue
            do
            {
                status = PCANBasic.Read(handle, out canMsg, out canTimestamp);
                if (status != TPCANStatus.PCAN_ERROR_QRCVEMPTY && status != TPCANStatus.PCAN_ERROR_OK)
                {
                    // Optionally handle other errors here
                    if (status == TPCANStatus.PCAN_ERROR_ILLOPERATION)
                        break;
                }
                if (status == TPCANStatus.PCAN_ERROR_OK)
                {
                    onMessageReceived?.Invoke(canMsg, canTimestamp);
                }
            }
            while (handle > 0 && (status != TPCANStatus.PCAN_ERROR_QRCVEMPTY));

            return status;
        }

        public void Dispose()
        {
            StopMonitoring();
            _timer.Dispose();
        }
    }
}
#endif
