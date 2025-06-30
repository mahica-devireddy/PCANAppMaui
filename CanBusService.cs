#if WINDOWS

using System;
using System.Timers;
using Peak.Can.Basic;
using PCANAppM.Services;  // adjust your namespace as needed

namespace PCANAppM.Services
{
    public class CanBusService : ICanBusService, IDisposable
    {
        private readonly Timer _timer;
        private const TPCANHandle Handle = PCANBasic.PCAN_USBBUS1;
        private bool _isInitialized;
        private bool _isConnected;

        public event EventHandler<bool> ConnectionStatusChanged = delegate { };
        public bool IsConnected => _isConnected;
        public string DeviceName => _isConnected ? Handle.ToString() : string.Empty;

        public CanBusService()
        {
            _timer = new Timer(1000);
            _timer.AutoReset = true;
            _timer.Elapsed += (_, __) => CheckStatus();
        }

        public void StartMonitoring()
        {
            if (!_isInitialized)
            {
                // initialize the channel at 250 kbit/s
                var initResult = PCANBasic.Initialize(
                    Handle,
                    TPCANBaudrate.PCAN_BAUD_250K
                );

                if (initResult != TPCANStatus.PCAN_ERROR_OK)
                    throw new InvalidOperationException($"PCAN init failed: {initResult}");

                _isInitialized = true;
            }

            _timer.Start();
        }

        public void StopMonitoring()
        {
            _timer.Stop();

            if (_isInitialized)
            {
                PCANBasic.Uninitialize(Handle);
                _isInitialized = false;
            }
        }

        private void CheckStatus()
        {
            if (!_isInitialized)
                return;

            // ask the driver for current channel status
            var status = PCANBasic.GetStatus(Handle);
            bool nowConnected = (status == TPCANStatus.PCAN_ERROR_OK);

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
                status = PCANBasic.Read(Handle, out var msg, out var ts);

                if (status == TPCANStatus.PCAN_ERROR_OK)
                    onMessageReceived?.Invoke(msg, ts);
                else if (status != TPCANStatus.PCAN_ERROR_QRCVEMPTY)
                    return status;

            } while (status == TPCANStatus.PCAN_ERROR_OK);

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
