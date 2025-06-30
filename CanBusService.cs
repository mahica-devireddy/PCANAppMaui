#if WINDOWS
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Timers;
using PCANAppM.Platforms.Windows;

namespace PCANAppM.Services
{
    public class CanBusService : IDisposable
    {
        private PCAN_USB _pcan = new PCAN_USB();
        private List<string> _lastDeviceList = new();
        private System.Timers.Timer? _devicePollTimer;
        private int _errorCount = 10;

        // Events for UI binding
        public event Action<string>? Feedback;
        public event Action<PCAN_USB.Packet>? MessageReceived;
        public event Action? DeviceListChanged;
        public event Action<string>? ErrorPrompt;
        public event Action? LoggingStarted;
        public event Action? LoggingStopped;

        // State
        public List<string> AvailableDevices { get; private set; } = new();
        public List<PCAN_USB.Packet> ReceivedPackets => _pcan.Packets;
        public bool IsConnected => _pcan != null && _pcan.PeakCANHandle != 0;
        public string DeviceName { get; private set; } = string.Empty;
        public string[] CANBaudRates => PCAN_USB.CANBaudRates;

        public CanBusService()
        {
            // Feedback and message events
            _pcan.Feedback += msg => Feedback?.Invoke(msg);
            _pcan.MessageReceived += pkt => MessageReceived?.Invoke(pkt);

            // Start polling for device changes
            _devicePollTimer = new System.Timers.Timer(1000);
            _devicePollTimer.Elapsed += DevicePollTimer_Elapsed;
            _devicePollTimer.AutoReset = true;
            _devicePollTimer.Start();

            // Initial device scan
            ScanDevices();
        }

        private void DevicePollTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            ScanDevices();
        }

        private void ScanDevices()
        {
            var currentDevices = PCAN_USB.GetUSBDevices();
            bool changed = false;

            if (_lastDeviceList.Count != currentDevices.Count)
            {
                changed = true;
            }
            else
            {
                for (int i = 0; i < _lastDeviceList.Count; i++)
                {
                    if (_lastDeviceList[i] != currentDevices[i])
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (changed)
            {
                _lastDeviceList = new List<string>(currentDevices);
                AvailableDevices = new List<string>(currentDevices);
                DeviceListChanged?.Invoke();

                if (AvailableDevices.Count == 0)
                {
                    if ((_errorCount++ % 10) == 0)
                        ErrorPrompt?.Invoke("Plug in a PEAK PCAN USB Adapter");
                }
                else
                {
                    _errorCount = 0;
                }
            }
        }

        public bool Initialize(string deviceName, string baudRate, bool enableRead = false)
        {
            var handle = PCAN_USB.DecodePEAKHandle(deviceName);
            if (handle == 0)
                return false;

            _pcan.Uninitialize();
            _pcan = new PCAN_USB();
            _pcan.Feedback += msg => Feedback?.Invoke(msg);
            _pcan.MessageReceived += pkt => MessageReceived?.Invoke(pkt);

            DeviceName = deviceName;
            _pcan.PeakCANHandle = handle;
            var status = _pcan.InitializeCAN(handle, baudRate, enableRead);
            return status == Peak.Can.Basic.TPCANStatus.PCAN_ERROR_OK;
        }

        public void Uninitialize()
        {
            _pcan.Uninitialize();
            DeviceName = string.Empty;
        }

        public bool SetIdentify(string deviceName, bool on)
        {
            var handle = PCAN_USB.DecodePEAKHandle(deviceName);
            if (handle > 0)
                return PCAN_USB.SetIdentify(handle, on);
            return false;
        }

        public Peak.Can.Basic.TPCANStatus SendFrame(uint id, int dataLength, byte[] data, bool isExtended = false)
        {
            return _pcan.WriteFrame(id, dataLength, data, isExtended);
        }

        public void StartReading() => _pcan.StartReading();
        public void StopReading() => _pcan.StopReading();

        public void SetOverwriteLastPacket(bool overwrite)
        {
            _pcan.OverwriteLastPacket = overwrite;
        }

        public void ClearPackets()
        {
            _pcan.Packets = new List<PCAN_USB.Packet>();
        }

        // Logging
        //public bool StartLogging(string directory, bool multiFile, bool dateFiles, uint traceSize)
        //{
        //    var result = _pcan.StartLogging(directory, multiFile, dateFiles, traceSize);
        //    if (result)
        //        LoggingStarted?.Invoke();
        //    return result;
        //}

        //public bool StopLogging()
        //{
        //    var result = _pcan.StopLogging();
        //    if (result)
        //        LoggingStopped?.Invoke();
        //    return result;
        //}

        public void Dispose()
        {
            _devicePollTimer?.Stop();
            _devicePollTimer?.Dispose();
            _pcan.Uninitialize();
        }
    }
}
#endif
