using Peak.Can.Basic;
using System;

namespace PCANAppM.Services
{
    public interface ICanBusService
    {
        /// <summary>True when the PCAN USB is plugged in AND initialized.</summary>
        bool IsConnected { get; }

        /// <summary>The device string from GetUSBDevices()[0], or null if none.</summary>
        string? DeviceName { get; }

        /// <summary>Raised on every incoming CAN frame (on the UI thread).</summary>
        event Action<PCAN_USB.Packet>? FrameReceived;

        /// <summary>Send a CAN frame on the shared bus.</summary>
        /// <param name="id">CAN ID.</param>
        /// <param name="data">Payload bytes.</param>
        /// <param name="extended">True for extended ID (> 0x7FF).</param>
        void SendFrame(uint id, byte[] data, bool extended);
    }
}
