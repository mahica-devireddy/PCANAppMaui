#if WINDOWS

using System;
using Timer = System.Timers.Timer;
using Peak.Can.Basic;                       // make sure you’ve added the Peak.Can.Basic NuGet/package
using TPCANHandle = System.UInt16;
using TPCANStatus = Peak.Can.Basic.TPCANStatus;

namespace PCANAppM.Services
{
    public class CanBusService : ICanBusService, IDisposable
    {
        private readonly Timer _timer;
        private readonly TPCANHandle _handle = PCANBasic.PCAN_USBBUS1;
        private bool _isConnected;
        private string _deviceName = "";

        public event EventHandler<bool> ConnectionStatusChanged;
        public bool IsConnected => _isConnected;
        public string DeviceName => _deviceName;

        public CanBusService()
        {
            // initialize the channel at 250 kbit/s (or whatever baud you need)
            var initResult = PCANBasic.Initialize(
                _handle,
                TPCANBaudrate.PCAN_BAUD_250K
            );
            // you can check initResult != PCAN_ERROR_OK to log/fail if you want

            _timer = new Timer(1000);
            _timer.Elapsed += (_, __) => CheckStatus();
        }

        public void StartMonitoring() => _timer.Start();
        public void StopMonitoring() => _timer.Stop();

        private void CheckStatus()
        {
            // static call—no instance
            var status = PCANBasic.GetStatus(_handle);
            bool nowConnected = (status == TPCANStatus.PCAN_ERROR_OK);

            if (nowConnected != _isConnected)
            {
                _isConnected = nowConnected;
                ConnectionStatusChanged?.Invoke(this, _isConnected);
            }
        }

        public void Dispose()
        {
            StopMonitoring();
            _timer.Dispose();
            PCANBasic.Uninitialize(_handle);
        }
    }
}
#endif
