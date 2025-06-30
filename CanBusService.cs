#if WINDOWS

using System;
using System.Timers;
using Peak.Can.Basic;

namespace PCANAppM.Services
{
    public class CanBusService : ICanBusService, IDisposable
    {
        private readonly Timer _timer;
        private const TPCANHandle Channel = PCANBasic.PCAN_USBBUS1;
        private bool _isInitialized;
        private bool _isConnected;

        public event EventHandler<bool> ConnectionStatusChanged = delegate { };
        public bool IsConnected => _isConnected;
        public string DeviceName => _isConnected ? Channel.ToString() : string.Empty;

        public CanBusService()
        {
            // poll every second looking for the dongle
            _timer = new Timer(1000);
            _timer.AutoReset = true;
            _timer.Elapsed += (_, __) => CheckStatusAndInit();
        }

        public void StartMonitoring()
        {
            _timer.Start();
        }

        public void StopMonitoring()
        {
            _timer.Stop();
            TearDownChannel();
        }

        private void CheckStatusAndInit()
        {
            // first, ask the driver if the channel is alive
            TPCANStatus status = _isInitialized
                ? PCANBasic.GetStatus(Channel)
                : TPCANStatus.PCAN_ERROR_INITIALIZE;

            bool nowPresent = status == TPCANStatus.PCAN_ERROR_OK;

            if (nowPresent && !_isInitialized)
            {
                // device just appeared → try to initialize
                var initResult = PCANBasic.Initialize(Channel, TPCANBaudrate.PCAN_BAUD_250K);
                _isInitialized = (initResult == TPCANStatus.PCAN_ERROR_OK);
                nowPresent = _isInitialized;
            }
            else if (!nowPresent && _isInitialized)
            {
                // device just vanished → uninitialize
                TearDownChannel();
            }

            // only fire event when connection state flips
            if (nowPresent != _isConnected)
            {
                _isConnected = nowPresent;
                ConnectionStatusChanged?.Invoke(this, _isConnected);
            }
        }

        private void TearDownChannel()
        {
            if (_isInitialized)
            {
                PCANBasic.Uninitialize(Channel);
                _isInitialized = false;
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
            _timer.Stop();
            _timer.Dispose();
            TearDownChannel();
        }
    }
}
#endif
