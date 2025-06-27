using System;
using System.Timers;
using Peak.Can.Basic;
using TPCANHandle = System.UInt16;
using TPCANStatus = Peak.Can.Basic.TPCANStatus;

namespace PCANAppM.Services
{
    /// <summary>
    /// Polls the PCAN-USB every second and raises ConnectionStatusChanged
    /// only when the state actually changes.
    /// </summary>
    public class CanBusService : ICanBusService, IDisposable
    {
        private readonly PCANBasic _pcan = new PCANBasic();
        private readonly Timer      _timer;
        private readonly TPCANHandle _handle = PCANBasic.PCAN_USBBUS1;
        private bool _isConnected;

        public event EventHandler<bool> ConnectionStatusChanged;
        public bool IsConnected => _isConnected;

        public CanBusService()
        {
            // poll every 1 second
            _timer = new Timer(1000);
            _timer.Elapsed += (_, __) => CheckStatus();
        }

        public void StartMonitoring() => _timer.Start();
        public void StopMonitoring()  => _timer.Stop();

        private void CheckStatus()
        {
            // Query bus status
            var status = _pcan.GetStatus(_handle);
            bool nowConnected = (status == TPCANStatus.PCAN_ERROR_OK);

            // Only fire if changed
            if (nowConnected != _isConnected)
            {
                _isConnected = nowConnected;
                ConnectionStatusChanged?.Invoke(this, _isConnected);
            }
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _pcan.Uninitialize(_handle);
        }
    }
}
