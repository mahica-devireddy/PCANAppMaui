using Peak.Can.Basic;
using System;

namespace PCANAppM.Platforms.Windows
{
    public static class PCanService
    {
        static PCanService()
        {
            // 1) Find the USB device
            var devices = PCAN_USB.GetUSBDevices();
            if (devices.Count == 0)
                throw new InvalidOperationException("No PCAN USB devices found.");

            // 2) Initialize it exactly once
            var handle = PCAN_USB.DecodePEAKHandle(devices[0]);
            Instance = new PCAN_USB();
            var status  = Instance.InitializeCAN(handle, "250 kbit/s", true);
            if (status != TPCANStatus.PCAN_ERROR_OK)
                throw new InvalidOperationException(Instance.PeakCANStatusErrorString(status));

            // 3) Expose the raw events
            Instance.MessageReceived += pkt => MessageReceived?.Invoke(pkt);
            Instance.Feedback        += msg => Feedback?.Invoke(msg);

            IsStarted = true;
        }

        public static PCAN_USB Instance { get; }
        public static bool     IsStarted { get; }

        /// <summary>Fires on every incoming CAN frame.</summary>
        public static event Action<PCAN_USB.Packet>? MessageReceived;
        /// <summary>Fires on library feedback (errors, logs, etc.).</summary>
        public static event Action<string>?         Feedback;
    }
}
