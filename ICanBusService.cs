#if WINDOWS

using System;
using Peak.Can.Basic;

namespace PCANAppM.Services
{
    public interface ICanBusService : IDisposable
    {
        /// <summary>
        /// Fires when the hardware connection state changes (plug in / unplug).
        /// </summary>
        event EventHandler<bool> ConnectionStatusChanged;

        /// <summary>
        /// True when the bus is open and the device is present.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// The formatted name of the current USB device (or empty).
        /// </summary>
        string DeviceName { get; }

        /// <summary>
        /// Begin polling for device presence and auto‐initialize.
        /// </summary>
        void StartMonitoring();

        /// <summary>
        /// Stop polling and uninitialize if necessary.
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// Read all queued CAN messages; invokes callback for each.
        /// </summary>
        TPCANStatus ReadMessages(Action<TPCANMsg, TPCANTimestamp> onMessageReceived);

        /// <summary>
        /// Send a single CAN frame.
        /// </summary>
        TPCANStatus SendFrame(uint id, byte[] data, bool extended = false);
    }
}
#endif
