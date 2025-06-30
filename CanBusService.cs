#if WINDOWS
using System;
using Timer = System.Timers.Timer;
using Peak.Can.Basic;
using PCANAppM.Platforms.Windows;  // for PCAN_USB helper
using TPCANHandle = Peak.Can.Basic.TPCANHandle;

namespace PCANAppM.Services
{
    public class CanBusService : ICanBusService
    {
        private readonly Timer _pollTimer;
        private TPCANHandle _currentHandle;
        private bool _isInitialized;
        private bool _isConnected;
        private string _deviceName = string.Empty;

        public event EventHandler<bool> ConnectionStatusChanged = delegate { };
        public bool IsConnected => _isConnected;
        public string DeviceName => _deviceName;

        public CanBusService()
        {
            _pollTimer = new Timer(1000) { AutoReset = true };
            _pollTimer.Elapsed += (_, __) => PollForDevice();
        }

        public void StartMonitoring() => _pollTimer.Start();
        public void StopMonitoring() => _pollTimer.Stop();

        private void PollForDevice()
        {
            var list = PCAN_USB.GetUSBDevices();
            bool found = list != null && list.Count > 0;

            // on first plug-in, initialize
            if (found && !_isInitialized)
            {
                _deviceName    = list[0];
                // DecodePEAKHandle returns UInt16; cast to TPCANHandle
                _currentHandle = (TPCANHandle)PCAN_USB.DecodePEAKHandle(_deviceName);
                var init = PCANBasic.Initialize(
                    _currentHandle,
                    TPCANBaudrate.PCAN_BAUD_250K
                );
                _isInitialized = init == TPCANStatus.PCAN_ERROR_OK;
                if (!_isInitialized)
                    _deviceName = string.Empty;
            }
            // on unplug, tear down
            else if (!found && _isInitialized)
            {
                PCANBasic.Uninitialize(_currentHandle);
                _isInitialized = false;
                _deviceName    = string.Empty;
            }

            // driver-level status
            bool nowUp = false;
            if (_isInitialized)
            {
                var st = PCANBasic.GetStatus(_currentHandle);
                nowUp = st == TPCANStatus.PCAN_ERROR_OK;
            }

            if (nowUp != _isConnected)
            {
                _isConnected = nowUp;
                ConnectionStatusChanged?.Invoke(this, _isConnected);
            }
        }

        public TPCANStatus ReadMessages(Action<TPCANMsg, TPCANTimestamp> onMessage)
        {
            if (!_isInitialized)
                return TPCANStatus.PCAN_ERROR_INITIALIZE;

            TPCANStatus rc;
            do
            {
                rc = PCANBasic.Read(_currentHandle, out var msg, out var ts);
                if (rc == TPCANStatus.PCAN_ERROR_OK)
                    onMessage?.Invoke(msg, ts);
                else if (rc != TPCANStatus.PCAN_ERROR_QRCVEMPTY)
                    return rc;
            }
            while (rc == TPCANStatus.PCAN_ERROR_OK);

            return TPCANStatus.PCAN_ERROR_OK;
        }

        public TPCANStatus SendFrame(uint id, byte[] data, bool extended = false)
        {
            if (!_isInitialized)
                return TPCANStatus.PCAN_ERROR_INITIALIZE;

            var msg = new TPCANMsg
            {
                ID      = id,
                LEN     = (byte)Math.Min(data.Length, 8),
                MSGTYPE = extended
                    ? TPCANMessageType.PCAN_MESSAGE_EXTENDED
                    : TPCANMessageType.PCAN_MESSAGE_STANDARD,
                DATA    = new byte[8]
            };
            Array.Copy(data, msg.DATA, msg.LEN);

            return PCANBasic.Write(_currentHandle, ref msg);
        }

        public void Dispose()
        {
            _pollTimer.Stop();
            _pollTimer.Dispose();
            if (_isInitialized)
                PCANBasic.Uninitialize(_currentHandle);
        }
    }
}

#endif
