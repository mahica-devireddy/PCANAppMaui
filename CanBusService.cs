#if WINDOWS

using System;
using System.Text;
using Timer = System.Timers.Timer;
using Peak.Can.Basic;                       // make sure you’ve added the Peak.Can.Basic NuGet/package
using TPCANHandle   = System.UInt16;
using TPCANStatus   = Peak.Can.Basic.TPCANStatus;
using TPCANParameter = Peak.Can.Basic.TPCANParameter;

namespace PCANAppM.Services
{
    public class CanBusService : ICanBusService, IDisposable
    {
        private readonly Timer      _timer;
        private readonly TPCANHandle _handle     = PCANBasic.PCAN_USBBUS1;
        private bool                 _isConnected;
        private string               _deviceName = "";

        public event EventHandler<bool> ConnectionStatusChanged;
        public bool   IsConnected  => _isConnected;
        public string DeviceName   => _deviceName;

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
        public void StopMonitoring()  => _timer.Stop();

        private void CheckStatus()
        {
            // static call—no instance
            var status       = PCANBasic.GetStatus(_handle);
            bool nowConnected = (status == TPCANStatus.PCAN_ERROR_OK);

            if (nowConnected != _isConnected)
            {
                _isConnected = nowConnected;

                if (_isConnected)
                {
                    // --- NEW: read the hardware name on connect ---
                    var sb = new StringBuilder(PCANBasic.MAX_LENGTH_HARDWARE_NAME);
                    var res = PCANBasic.GetValue(
                        _handle,
                        TPCANParameter.PCAN_HARDWARE_NAME,
                        sb,
                        PCANBasic.MAX_LENGTH_HARDWARE_NAME
                    );
                    _deviceName = (res == TPCANStatus.PCAN_ERROR_OK) 
                                ? sb.ToString() 
                                : "Unknown PCAN USB";
                }
                else
                {
                    _deviceName = "";
                }
                // --------------------------------------------------

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
