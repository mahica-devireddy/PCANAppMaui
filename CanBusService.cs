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
    const TPCANHandle handle = PCANBasic.PCAN_USBBUS1;
    TPCANStatus status;

    do
    {
        status = PCANBasic.Read(handle, out var msg, out var ts);

        switch (status)
        {
            case TPCANStatus.PCAN_ERROR_OK:
                onMessageReceived?.Invoke(msg, ts);
                break;

            case TPCANStatus.PCAN_ERROR_QRCVEMPTY:
                // no more messages in queue
                break;

            default:
                // some other error occurred — consider logging
                // e.g. Debug.WriteLine($"PCAN Read error: {status}");
                return status;
        }
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
