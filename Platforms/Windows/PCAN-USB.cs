#if WINDOWS

using LocalizationResourceManager.Maui;
using Peak.Can.Basic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace PCANAppM.Platforms.Windows
{
    using PCANHandle = UInt16;

    public class PCAN_USB
    {
        // Events for feedback and received messages
        public event Action<string>? Feedback;
        public event Action<Packet>? MessageReceived;

        static readonly PCANHandle[] handlesArray = new PCANHandle[]
        {
            PCANBasic.PCAN_USBBUS1, PCANBasic.PCAN_USBBUS2, PCANBasic.PCAN_USBBUS3, PCANBasic.PCAN_USBBUS4,
            PCANBasic.PCAN_USBBUS5, PCANBasic.PCAN_USBBUS6, PCANBasic.PCAN_USBBUS7, PCANBasic.PCAN_USBBUS8,
            PCANBasic.PCAN_USBBUS9, PCANBasic.PCAN_USBBUS10, PCANBasic.PCAN_USBBUS11, PCANBasic.PCAN_USBBUS12,
            PCANBasic.PCAN_USBBUS13, PCANBasic.PCAN_USBBUS14, PCANBasic.PCAN_USBBUS15, PCANBasic.PCAN_USBBUS16
        };

        public PCANHandle PeakCANHandle { get; set; } = 0;
        public TPCANStatus LastOperationStatus = TPCANStatus.PCAN_ERROR_UNKNOWN;
        public List<Packet> Packets { get; set; } = new();
        public bool OverwriteLastPacket { get; set; } = true;
        public bool WatchForPackets { get; set; } = false;
        public List<Packet> WatchPackets { get; set; } = new();
        public Packet? DiffPacket { get; set; } = null;

        private Thread? _readThread;
        private bool _rxMessages = false;

        public static List<string> GetUSBDevices()
        {
            TPCANStatus dllRet = TPCANStatus.PCAN_ERROR_UNKNOWN;
            UInt32 iBuffer;
            List<string>? PCANDevices = null;
            bool isFD;

            for (int i = 0; i < handlesArray.Length; i++)
            {
                dllRet = PCANBasic.GetValue(handlesArray[i], TPCANParameter.PCAN_CHANNEL_CONDITION, out iBuffer, sizeof(UInt32));
                if ((dllRet == TPCANStatus.PCAN_ERROR_OK) &&
                    ((iBuffer & PCANBasic.PCAN_CHANNEL_AVAILABLE) == PCANBasic.PCAN_CHANNEL_AVAILABLE ||
                     (iBuffer & PCANBasic.PCAN_CHANNEL_PCANVIEW) == PCANBasic.PCAN_CHANNEL_PCANVIEW))
                {
                    dllRet = PCANBasic.GetValue(handlesArray[i], TPCANParameter.PCAN_CHANNEL_FEATURES, out iBuffer, sizeof(UInt32));
                    isFD = (dllRet == TPCANStatus.PCAN_ERROR_OK) && ((iBuffer & PCANBasic.FEATURE_FD_CAPABLE) == PCANBasic.FEATURE_FD_CAPABLE);
                    PCANDevices ??= new List<string>();
                    PCANDevices.Add(FormatChannelName(handlesArray[i], isFD));
                }
            }
            return PCANDevices ?? new List<string>();
        }

        private static string FormatChannelName(PCANHandle handle, bool isFD)
        {
            TPCANDevice devDevice;
            byte byChannel;

            if (handle < 0x100)
            {
                devDevice = (TPCANDevice)(handle >> 4);
                byChannel = (byte)(handle & 0xF);
            }
            else
            {
                devDevice = (TPCANDevice)(handle >> 8);
                byChannel = (byte)(handle & 0xFF);
            }
            return isFD
                ? $"{devDevice}:FD {byChannel} ({handle:X2}h)"
                : $"{devDevice} {byChannel} ({handle:X2}h)";
        }

        public static PCANHandle DecodePEAKHandle(string PEAKListHandle)
        {
            if (!string.IsNullOrWhiteSpace(PEAKListHandle))
            {
                var idx = PEAKListHandle.IndexOf('(');
                if (idx >= 0 && PEAKListHandle.Length >= idx + 4)
                {
                    var hex = PEAKListHandle.Substring(idx + 1, 3).Replace("h", "").Trim();
                    if (UInt16.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var handle))
                        return handle;
                }
            }
            return 0;
        }

        public static bool IsIdentifyOn(PCANHandle handle)
        {
            TPCANStatus ret = PCANBasic.GetValue(handle, TPCANParameter.PCAN_CHANNEL_IDENTIFYING, out UInt32 iBuffer, sizeof(UInt32));
            return ret == TPCANStatus.PCAN_ERROR_OK && (iBuffer & PCANBasic.PCAN_PARAMETER_ON) == PCANBasic.PCAN_PARAMETER_ON;
        }

        public static bool SetIdentify(PCANHandle handle, bool on)
        {
            UInt32 iBuffer = (uint)(on ? PCANBasic.PCAN_PARAMETER_ON : PCANBasic.PCAN_PARAMETER_OFF);
            TPCANStatus ret = PCANBasic.SetValue(handle, TPCANParameter.PCAN_CHANNEL_IDENTIFYING, ref iBuffer, sizeof(UInt32));
            return ret == TPCANStatus.PCAN_ERROR_OK;
        }

        public static string[] CANBaudRates = {
            "5 kbit/s", "10 kbit/s", "20 kbit/s", "33.333 kbit/s", "47.619 kbit/s", "50 kbit/s", "83.333 kbit/s",
            "95.238 kbit/s", "100 kbit/s", "125 kbit/s", "250 kbit/s", "500 kbit/s", "800 kbit/s", "1 Mbit/s"
        };

        private static TPCANBaudrate CANBaudRateToPeakCANBaudRate(string rate) => rate switch
        {
            "1 Mbit/s" => TPCANBaudrate.PCAN_BAUD_1M,
            "800 kbit/s" => TPCANBaudrate.PCAN_BAUD_800K,
            "500 kbit/s" => TPCANBaudrate.PCAN_BAUD_500K,
            "250 kbit/s" => TPCANBaudrate.PCAN_BAUD_250K,
            "125 kbit/s" => TPCANBaudrate.PCAN_BAUD_125K,
            "100 kbit/s" => TPCANBaudrate.PCAN_BAUD_100K,
            "95.238 kbit/s" => TPCANBaudrate.PCAN_BAUD_95K,
            "83.333 kbit/s" => TPCANBaudrate.PCAN_BAUD_83K,
            "50 kbit/s" => TPCANBaudrate.PCAN_BAUD_50K,
            "47.619 kbit/s" => TPCANBaudrate.PCAN_BAUD_47K,
            "33.333 kbit/s" => TPCANBaudrate.PCAN_BAUD_33K,
            "20 kbit/s" => TPCANBaudrate.PCAN_BAUD_20K,
            "10 kbit/s" => TPCANBaudrate.PCAN_BAUD_10K,
            _ => TPCANBaudrate.PCAN_BAUD_5K
        };

        private void RaiseFeedback(string message) => Feedback?.Invoke(message);

        public TPCANStatus InitializeCAN(PCANHandle handle, string baudRate, bool enableRead = false)
        {
            LastOperationStatus = PCANBasic.Initialize(handle, CANBaudRateToPeakCANBaudRate(baudRate), (TPCANType)0, 0, 0);
            RaiseFeedback(LastOperationStatus == TPCANStatus.PCAN_ERROR_OK
                ? "Initialized."
                : $"Initialization failed: {PeakCANStatusErrorString(LastOperationStatus)}");

            if (LastOperationStatus == TPCANStatus.PCAN_ERROR_OK)
            {
                PeakCANHandle = handle;
                if (enableRead)
                    StartReading();
            }

            return LastOperationStatus;
        }

        public TPCANStatus Uninitialize()
        {
            StopReading();
            var ret = PCANBasic.Uninitialize(PeakCANHandle);
            RaiseFeedback("Uninitialized.");
            return ret;
        }

        public TPCANStatus WriteFrame(UInt32 id, int dataLength, byte[] data, bool isExtended = false)
        {
            // Ensure data is exactly 8 bytes
            var paddedData = new byte[8];
            Array.Copy(data, paddedData, Math.Min(data.Length, 8));

            var msg = new TPCANMsg
            {
                MSGTYPE = isExtended ? TPCANMessageType.PCAN_MESSAGE_EXTENDED : TPCANMessageType.PCAN_MESSAGE_STANDARD,
                ID = id,
                LEN = (byte)dataLength,
                DATA = paddedData
            };
            LastOperationStatus = PCANBasic.Write(PeakCANHandle, ref msg);
            if (LastOperationStatus != TPCANStatus.PCAN_ERROR_OK)
                RaiseFeedback(PeakCANStatusErrorString(LastOperationStatus));
            return LastOperationStatus;
        }

        public string PeakCANStatusErrorString(TPCANStatus error)
        {
            var strTemp = new StringBuilder(256);
            return PCANBasic.GetErrorText(error, 0, strTemp) != TPCANStatus.PCAN_ERROR_OK
                ? $"An error occurred. Error-code's text ({error:X}) couldn't be retrieved"
                : strTemp.ToString();
        }

        // --- CAN Reading Logic ---

        public void StartReading()
        {
            StopReading();
            _rxMessages = true;
            _readThread = new Thread(ReadLoop) { IsBackground = true };
            _readThread.Start();
        }

        public void StopReading()
        {
            _rxMessages = false;
            if (_readThread != null)
            {
                _readThread.Join(200);
                _readThread = null;
            }
        }

        private void ReadLoop()
        {
            while (_rxMessages)
            {
                bool messageRead = false; // Initialize the variable to avoid CS0165
                do
                {
                    TPCANMsg canMsg;
                    TPCANTimestamp canTimestamp;
                    var result = PCANBasic.Read(PeakCANHandle, out canMsg, out canTimestamp);

                    if (result == TPCANStatus.PCAN_ERROR_OK)
                    {
                        messageRead = true;
                        ulong micros = (ulong)canTimestamp.micros + 1000UL * canTimestamp.millis + 0x100000000UL * 1000UL * canTimestamp.millis_overflow;
                        var packet = new Packet
                        {
                            Microseconds = micros,
                            Id = canMsg.ID,
                            Length = canMsg.LEN,
                            Data = (byte[])canMsg.DATA.Clone(),
                            IsExtended = (canMsg.MSGTYPE & TPCANMessageType.PCAN_MESSAGE_EXTENDED) == TPCANMessageType.PCAN_MESSAGE_EXTENDED
                        };

                        if (OverwriteLastPacket)
                        {
                            var existing = Packets.Find(x => x.Id == packet.Id && x.IsExtended == packet.IsExtended);
                            if (existing != null)
                            {
                                Packets.Remove(existing);
                            }
                        }
                        Packets.Add(packet);

                        MessageReceived?.Invoke(packet);

                        if (WatchForPackets && FoundPacket(packet))
                        {
                            RaiseFeedback($"Found {PacketToString(packet)} {DateTime.Now.ToLocalTime()}");
                            if (DiffPacket != null)
                            {
                                ulong diff = packet.Microseconds - DiffPacket.Microseconds;
                                RaiseFeedback($"Diff: {(Convert.ToDouble(diff) / 1000000.0d):F6} {DateTime.Now.ToLocalTime()}");
                            }
                            else
                            {
                                RaiseFeedback("First Packet");
                            }
                            DiffPacket = Packet.Clone(packet);
                        }
                    }
                    else if (result == TPCANStatus.PCAN_ERROR_QRCVEMPTY)
                    {
                        messageRead = false;
                    }
                    else if (result == TPCANStatus.PCAN_ERROR_ILLOPERATION)
                    {
                        RaiseFeedback("Illegal operation error during CAN read.");
                        return;
                    }
                } while (messageRead && _rxMessages);

                Thread.Sleep(10);
            }
        }

        public void SetWatchPackets(List<Packet> packets)
        {
            WatchPackets = packets;
        }

        public bool FoundPacket(Packet currentPacket)
        {
            foreach (var packet in WatchPackets)
            {
                if (packet.Id == currentPacket.Id && packet.Length == currentPacket.Length && packet.IsExtended == currentPacket.IsExtended)
                {
                    bool match = true;
                    for (int i = 0; i < packet.Length; i++)
                    {
                        if (packet.Data[i] != currentPacket.Data[i])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                        return true;
                }
            }
            return false;
        }

        public static string PacketToString(Packet packet)
        {
            var sb = new StringBuilder();
            sb.Append((packet.Microseconds / 1000000.0d).ToString("F6", CultureInfo.InvariantCulture));
            sb.Append(' ');
            sb.Append(packet.Id);
            if (packet.IsExtended)
                sb.Append(" (EXT)");
            sb.Append(' ');
            sb.Append(packet.Length);
            sb.Append(' ');
            for (int i = 0; i < packet.Length; i++)
            {
                sb.Append(" " + packet.Data[i].ToString("X2"));
            }
            return sb.ToString();
        }

        // --- Packet class ---
        public class Packet
        {
            public ulong Microseconds { get; set; }
            public uint Id { get; set; }
            public byte Length { get; set; }
            public byte[] Data { get; set; } = Array.Empty<byte>();
            public bool IsExtended { get; set; }

            public static Packet Clone(Packet src)
            {
                return new Packet
                {
                    Microseconds = src.Microseconds,
                    Id = src.Id,
                    Length = src.Length,
                    Data = (byte[])src.Data.Clone(),
                    IsExtended = src.IsExtended
                };
            }
        }
    }
}
#endif
