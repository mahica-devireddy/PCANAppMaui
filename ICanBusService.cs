// in Services/ICanBusService.cs
using Peak.Can.Basic;
using System;

namespace PCANAppM.Services
{
#if WINDOWS
    public interface ICanBusService
    {
        /// <summary>True when the PCAN USB is plugged in AND initialized.</summary>
        bool IsConnected { get; }

        /// <summary>The device string from GetUSBDevices()[0], or null if none.</summary>
        string? DeviceName { get; }

        /// <summary>Fires whenever IsConnected changes (debounced).</summary>
        event Action? StatusChanged;

        /// <summary>Fires on every incoming CAN frame (on the UI thread).</summary>
        event Action<PCAN_USB.Packet>? FrameReceived;

        /// <summary>Send a CAN frame on the shared bus.</summary>
        void SendFrame(uint id, byte[] data, bool extended);
    }
#endif
}
