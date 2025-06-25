using PCANAppM;
using Peak.Can.Basic;
using System;

#if WINDOWS
using PCANAppM.Platforms.Windows;
#endif

namespace PCANAppM
{
#if WINDOWS
    public static class PCanService
    {
        public static PCAN_USB? Instance { get; private set; }
        public static bool IsStarted { get; private set; }
        public static string? DeviceName { get; private set; }

        /// <summary>Fires on every incoming CAN frame.</summary>
        public static event Action<PCAN_USB.Packet>? MessageReceived;
        /// <summary>Fires on library feedback (errors, logs, etc.).</summary>
        public static event Action<string>? Feedback;

        public static bool TryInitialize()
        {
            try
            {
                var devices = PCAN_USB.GetUSBDevices();
                if (devices.Count == 0)
                    return false;

                DeviceName = devices[0];
                var handle = PCAN_USB.DecodePEAKHandle(devices[0]);
                Instance = new PCAN_USB();
                var status = Instance.InitializeCAN(handle, "250 kbit/s", true);
                if (status != TPCANStatus.PCAN_ERROR_OK)
                    return false;

                Instance.MessageReceived += pkt => MessageReceived?.Invoke(pkt);
                Instance.Feedback += msg => Feedback?.Invoke(msg);

                IsStarted = true;
                return true;
            }
            catch
            {
                // Optionally log or handle error
                IsStarted = false;
                return false;
            }
        }
    }
#endif
}


