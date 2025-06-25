using Peak.Can.Basic;
using System;

public static class PCanBusManager
{
    static PCanBusManager()
    {
        // 1) Find & open the USB device exactly once
        var devices = PCAN_USB.GetUSBDevices();
        if (devices.Count == 0)
            throw new InvalidOperationException("No PCAN USB devices found.");

        var handle = PCAN_USB.DecodePEAKHandle(devices[0]);
        Instance = new PCAN_USB();
        var status = Instance.InitializeCAN(handle, "250 kbit/s", true);
        if (status != TPCANStatus.PCAN_ERROR_OK)
            throw new InvalidOperationException(Instance.PeakCANStatusErrorString(status));

        // 2) Wire up raw events
        Instance.MessageReceived += packet => MessageReceived?.Invoke(packet);
        Instance.Feedback        += msg    => Feedback?.Invoke(msg);

        IsStarted = true;
    }

    /// <summary> The one-and-only PCAN_USB instance. </summary>
    public static PCAN_USB Instance { get; }

    /// <summary> Have we successfully initialized the bus? </summary>
    public static bool IsStarted { get; }

    /// <summary> Fires on *every* incoming CAN frame. </summary>
    public static event Action<PCAN_USB.Packet>? MessageReceived;

    /// <summary> Fires on any library messages (errors, logs, etc). </summary>
    public static event Action<string>? Feedback;
}
