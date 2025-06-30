#if WINDOWS

using System;
using Peak.Can.Basic;
using System;
using System.ComponentModel;
using PCANAppM.Platforms.Windows;

namespace PCANAppM.Services
{

    public interface ICanBusService : IDisposable
    {
        //bool IsConnected { get; }
        //string? DeviceName { get; }
        //event Action? StatusChanged;
        //event Action<PCAN_USB.Packet>? FrameReceived;
        //void SendFrame(uint id, byte[] do data, bool extended);

        event EventHandler<bool> ConnectionStatusChanged;

        bool IsConnected { get; }

        string DeviceName { get; }

        void StartMonitoring();

        void StopMonitoring();

        //TPCANStatus SendFrame(uint id, int dataLength, byte[] data);
        TPCANStatus ReadMessages(Action<TPCANMsg, TPCANTimestamp> onMessageReceived);

    }
}
#endif
