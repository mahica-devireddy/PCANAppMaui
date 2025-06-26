#if WINDOWS

using Microsoft.Maui.Dispatching;
using Peak.Can.Basic;
using System;
using System.Timers;
using PCANAppM.Platforms.Windows;

namespace PCANAppM.Services
{
    public class PcanUsbStatusService
    {
        private static PcanUsbStatusService? _instance;
        public static PcanUsbStatusService Instance => _instance ??= new PcanUsbStatusService();

        readonly Timer _pollTimer;
        bool           _isConnected;
        string?        _deviceName;
        PCAN_USB?      _pcanUsb;

        /// <summary>True when we have an initialized PCAN_USB.</summary>
        public bool IsConnected => _isConnected;
        /// <summary>The channel string from first‐found device, or null.</summary>
        public string? DeviceName => _deviceName;
        /// <summary>The initialized PCAN_USB instance, or null.</summary>
        public PCAN_USB? PcanUsb  => _pcanUsb;

        /// <summary>Baud rate to use on init.</summary>
        public string BaudRate { get; } = "250 kbit/s";

        /// <summary>Fires once whenever IsConnected flips true/false.</summary>
        public event Action? StatusChanged;

        private PcanUsbStatusService()
        {
            _pollTimer = new Timer(500) { AutoReset = true };
            _pollTimer.Elapsed += (_,__) => PollStatus();
            _pollTimer.Start();
            PollStatus();  // immediate first check
        }

        private void PollStatus()
        {
            // If we haven't yet opened the bus, look for a plug by enumeration
            if (_pcanUsb == null)
            {
                var devs = PCAN_USB.GetUSBDevices();
                if (devs.Count > 0)
                {
                    // first‐time plug
                    _deviceName = devs[0];
                    var handle  = PCAN_USB.DecodePEAKHandle(_deviceName);
                    _pcanUsb    = new PCAN_USB();
                    var initSt  = _pcanUsb.InitializeCAN(handle, BaudRate, true);
                    if (initSt != TPCANStatus.PCAN_ERROR_OK)
                    {
                        // init failure → clean up
                        _pcanUsb    = null;
                        _deviceName = null;
                    }
                }
            }
            else
            {
                // Already have a handle: poll channel condition instead of re‐enumerating
                var handle = _pcanUsb.PeakCANHandle;
                var res    = PCANBasic.GetValue(
                    handle,
                    TPCANParameter.PCAN_CHANNEL_CONDITION,
                    out uint condition,
                    sizeof(uint)
                );

                bool stillHere = res == TPCANStatus.PCAN_ERROR_OK
                             && (condition & PCANBasic.PCAN_CHANNEL_AVAILABLE)
                                == PCANBasic.PCAN_CHANNEL_AVAILABLE;

                if (!stillHere)
                {
                    // sustained unplug: tear down
                    _pcanUsb.Uninitialize();
                    _pcanUsb    = null;
                    _deviceName = null;
                }
            }

            // Only fire StatusChanged when the actual boolean flips
            bool nowConn = _pcanUsb != null;
            if (nowConn != _isConnected)
            {
                _isConnected = nowConn;
                StatusChanged?.Invoke();
            }
        }
    }
}
#endif
