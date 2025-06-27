#if WINDOWS

using System;
using Peak.Can.Basic;
using System;
using System.ComponentModel;
using PCANAppM.Platforms.Windows;

namespace PCANAppM.Services
{

    public interface ICanBusService : INotifyPropertyChanged, IDisposable
    {
        bool IsConnected { get; }
        string? DeviceName { get; }
        event Action? StatusChanged;
        event Action<PCAN_USB.Packet>? FrameReceived;
        void SendFrame(uint id, byte[] data, bool extended);
    }
}
#endif
