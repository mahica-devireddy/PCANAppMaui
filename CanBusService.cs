#if WINDOWS

using System;
using System.Timers;
using Peak.Can.Basic;
using PCANAppM.Platforms.Windows;   // for PCAN_USB
using PCANAppM.Services;            // your ICanBusService interface

namespace PCANAppM.Services
{
    public class CanBusService : ICanBusService, IDisposable
    {
        private readonly Timer _pollTimer;
        private PCANHandle _currentHandle;
        private bool _isInitialized;
        private bool _isConnected;
        private string _deviceName = "";

        public event EventHandler<bool> ConnectionStatusChanged = delegate { };
        public bool IsConnected => _isConnected;
        public string DeviceName   => _deviceName;

        public CanBusService()
        {
            _pollTimer = new Timer(1000) { AutoReset = true };
            _pollTimer.Elapsed += (_,__) => PollForDevice();
        }

        public void StartMonitoring() => _pollTimer.Start();
        public void StopMonitoring()  => _pollTimer.Stop();

        private void PollForDevice()
        {
            // 1) Enumerate any plugged-in PCAN-USB devices
            var list = PCAN_USB.GetUSBDevices();
            bool found = list != null && list.Count > 0;

            // 2) If we see it and haven’t yet initialized, do so:
            if (found && !_isInitialized)
            {
                _deviceName   = list[0];
                _currentHandle = PCAN_USB.DecodePEAKHandle(_deviceName);

                var init = PCANBasic.Initialize(
                    _currentHandle,
                    TPCANBaudrate.PCAN_BAUD_250K
                );

                _isInitialized = (init == TPCANStatus.PCAN_ERROR_OK);
                if (!_isInitialized)
                    _deviceName = "";
            }
            // 3) If it’s gone and we were initialized, tear down:
            else if (!found && _isInitialized)
            {
                PCANBasic.Uninitialize(_currentHandle);
                _isInitialized = false;
                _deviceName    = "";
            }

            // 4) Ask the driver if the channel is “really” up:
            bool nowUp = false;
            if (_isInitialized)
            {
                var st = PCANBasic.GetStatus(_currentHandle);
                nowUp = (st == TPCANStatus.PCAN_ERROR_OK);
            }

            // 5) Fire event only on true state-changes:
            if (nowUp != _isConnected)
            {
                _isConnected = nowUp;
                ConnectionStatusChanged?.Invoke(this, _isConnected);
            }
        }

        /// <summary>
        /// Read all pending CAN frames and invoke callback for each.
        /// </summary>
        public TPCANStatus ReadMessages(Action<TPCANMsg, TPCANTimestamp> onMessage)
        {
            if (!_isInitialized)
                return TPCANStatus.PCAN_ERROR_NOT_INITIALIZED;

            TPCANStatus rc;
            do
            {
                rc = PCANBasic.Read(
                    _currentHandle,
                    out var msg,
                    out var ts
                );

                if (rc == TPCANStatus.PCAN_ERROR_OK)
                    onMessage?.Invoke(msg, ts);
                else if (rc != TPCANStatus.PCAN_ERROR_QRCVEMPTY)
                    return rc;
            }
            while (rc == TPCANStatus.PCAN_ERROR_OK);

            return TPCANStatus.PCAN_ERROR_OK;
        }

        /// <summary>
        /// Send a single CAN frame.
        /// </summary>
        public TPCANStatus SendFrame(uint id, byte[] data, bool extended = false)
        {
            if (!_isInitialized)
                return TPCANStatus.PCAN_ERROR_NOT_INITIALIZED;

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
            StopMonitoring();
            _pollTimer.Dispose();

            if (_isInitialized)
                PCANBasic.Uninitialize(_currentHandle);
        }
    }
}
#endif
