using Peak.Can.Basic;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

#if WINDOWS
using PCANAppM.Platforms.Windows;
#endif

namespace PCANAppM;

public class ChangeIdService
{
#if WINDOWS
    // Device/baud picker population
    public void PopulateDeviceAndBaudPickers(Picker devicePicker, Picker baudRatePicker)
    {
        devicePicker.Items.Clear();
        foreach (var dev in PCAN_USB.GetUSBDevices())
            devicePicker.Items.Add(dev);

        baudRatePicker.Items.Clear();
        foreach (var rate in PCAN_USB.CANBaudRates)
            baudRatePicker.Items.Add(rate);

        if (baudRatePicker.Items.Contains("250 kbit/s"))
            baudRatePicker.SelectedIndex = baudRatePicker.Items.IndexOf("250 kbit/s");
    }

    // Initial button state
    public void SetInitialButtonStates(Picker devicePicker, Picker baudRatePicker, Button startStopButton, Button sendButton)
    {
        startStopButton.IsEnabled = devicePicker.Items.Count > 0 && baudRatePicker.SelectedIndex >= 0;
        sendButton.IsEnabled = false;
    }

    // Start/Stop CAN logic
    public void OnStartStopClicked(
        Picker devicePicker, Picker baudRatePicker, Button startStopButton, Button sendButton,
        ObservableCollection<CanMessageViewModel> canMessages,
        ref PCAN_USB? pcanUsb, ref ushort currentHandle, ref bool isStarted,
        Action<PCAN_USB.Packet> onMessageReceived, Action<string> onFeedback)
    {
        if (!isStarted)
        {
            if (devicePicker.SelectedIndex < 0 || baudRatePicker.SelectedIndex < 0)
            {
                canMessages.Insert(0, new CanMessageViewModel { Direction = "", Id = "", Data = "Select device and baud rate." });
                return;
            }
            var handle = PCAN_USB.DecodePEAKHandle(devicePicker.SelectedItem.ToString());
            var baud = baudRatePicker.SelectedItem.ToString();

            pcanUsb = new PCAN_USB();
            pcanUsb.MessageReceived += onMessageReceived;
            pcanUsb.Feedback += onFeedback;

            var status = pcanUsb.InitializeCAN(handle, baud, true);
            if (status == TPCANStatus.PCAN_ERROR_OK)
            {
                currentHandle = handle;
                isStarted = true;
                startStopButton.Text = "Stop";
                sendButton.IsEnabled = true;
                canMessages.Insert(0, new CanMessageViewModel { Direction = "", Id = "", Data = "CAN started." });
            }
            else
            {
                canMessages.Insert(0, new CanMessageViewModel { Direction = "", Id = "", Data = $"Init failed: {pcanUsb.PeakCANStatusErrorString(status)}" });
            }
        }
        else
        {
            pcanUsb?.Uninitialize();
            isStarted = false;
            startStopButton.Text = "Start";
            sendButton.IsEnabled = false;
            canMessages.Insert(0, new CanMessageViewModel { Direction = "", Id = "", Data = "CAN stopped." });
        }
    }

    // Send CAN message (generic)
    public void SendCanMessage(
        PCAN_USB? pcanUsb, bool isStarted,
        ObservableCollection<CanMessageViewModel> canMessages,
        string canIdHex, byte[] data, int dataLen)
    {
        if (pcanUsb == null || !isStarted)
            return;

        uint canId = uint.Parse(canIdHex, NumberStyles.HexNumber);
        byte[] paddedData = data.Length < dataLen
            ? data.Concat(Enumerable.Repeat((byte)0x00, dataLen - data.Length)).ToArray()
            : data;

        var status = pcanUsb.WriteFrame(canId, dataLen, paddedData, canId > 0x7FF);

        canMessages.Insert(0, new CanMessageViewModel
        {
            Direction = "Tx",
            Id = $"0x{canId:X}",
            Data = string.Join(" ", paddedData.Take(dataLen).Select(b => b.ToString("X2")))
        });
    }

    // Send CAN message (from UI)
    public void OnSendClicked(
        Entry canIdEntry, Entry dataEntry, Entry lengthEntry,
        ObservableCollection<CanMessageViewModel> canMessages,
        PCAN_USB? pcanUsb, bool isStarted)
    {
        if (pcanUsb == null || !isStarted)
            return;

        if (!uint.TryParse(canIdEntry.Text, out var canId))
        {
            canMessages.Insert(0, new CanMessageViewModel { Direction = "", Id = "", Data = "Invalid CAN ID." });
            return;
        }

        var dataParts = dataEntry.Text?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (dataParts == null)
        {
            canMessages.Insert(0, new CanMessageViewModel { Direction = "", Id = "", Data = "No data." });
            return;
        }
        var data = new byte[8];
        int len = 0;
        foreach (var part in dataParts)
        {
            if (len >= 8) break;
            if (byte.TryParse(part, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
                data[len++] = b;
            else
            {
                canMessages.Insert(0, new CanMessageViewModel { Direction = "", Id = "", Data = $"Invalid data byte: {part}" });
                return;
            }
        }
        if (!int.TryParse(lengthEntry.Text, out var dataLen) || dataLen < 0 || dataLen > 8)
            dataLen = len;

        var status = pcanUsb.WriteFrame(canId, dataLen, data, canId > 0x7FF);
        if (status == TPCANStatus.PCAN_ERROR_OK)
        {
            canMessages.Insert(0, new CanMessageViewModel
            {
                Direction = "Tx",
                Id = $"0x{canId:X}",
                Data = string.Join(" ", data.Take(dataLen).Select(b => b.ToString("X2")))
            });
        }
        else
        {
            canMessages.Insert(0, new CanMessageViewModel { Direction = "", Id = "", Data = $"Send failed: {pcanUsb.PeakCANStatusErrorString(status)}" });
        }
    }

    // Receive CAN message
    public void OnCanMessageReceived(
        PCAN_USB.Packet packet, 
        ObservableCollection<CanMessageViewModel> canMessages,
        Action<string> updateLatestCanIdLabel,
        Action<string> setCurrentCanId)
    {
        var idHex = $"0x{packet.Id:X}";
        string lastTwo = idHex.Length >= 2 ? idHex.Substring(idHex.Length - 2) : idHex;
        setCurrentCanId(lastTwo);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            canMessages.Insert(0, new CanMessageViewModel
            {
                Direction = "Rx",
                Id = idHex,
                Data = string.Join(" ", packet.Data.Take(packet.Length).Select(b => b.ToString("X2")))
            });
            updateLatestCanIdLabel(lastTwo);
            if (canMessages.Count > 100)
                canMessages.RemoveAt(canMessages.Count - 1);
        });
    }

    // Feedback
    public void OnPcanFeedback(
        string message,
        ObservableCollection<CanMessageViewModel> canMessages)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            canMessages.Insert(0, new CanMessageViewModel
            {
                Direction = "",
                Id = "",
                Data = message
            });
            if (canMessages.Count > 100)
                canMessages.RemoveAt(canMessages.Count - 1);
        });
    }

    // CAN ID entry/hex conversion
    public void OnCanIdEntryChanged(string? canIdText, Entry canIdHexEntry, ref bool bChanging)
    {
        if (bChanging) return;
        bChanging = true;
        if (uint.TryParse(canIdText, out var dec))
            canIdHexEntry.Text = dec.ToString("X");
        else
            canIdHexEntry.Text = string.Empty;
        bChanging = false;
    }

    public void OnCanIdHexEntryChanged(string? canIdHexText, Entry canIdEntry, ref bool bChanging)
    {
        if (bChanging) return;
        bChanging = true;
        if (uint.TryParse(canIdHexText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
            canIdEntry.Text = hex.ToString();
        else
            canIdEntry.Text = string.Empty;
        bChanging = false;
    }

    // Show update CAN ID dialog
    public async Task ShowUpdateCanIdDialog(Entry newCanIdEntry, Button confirmCanIdButton, Func<string, string, Task> displayAlert)
    {
        newCanIdEntry.Text = string.Empty;
        newCanIdEntry.IsVisible = true;
        confirmCanIdButton.IsVisible = true;
        await displayAlert("Update CAN ID", "Enter the new CAN ID (last two hex digits) and press Confirm.");
    }

    // CAN ID change workflow (calls device-specific delegate)
    public async Task ChangeCanIdAsync(
        Func<string?> getCurrentCanId,
        Func<string?> getNewCanId,
        Func<string, string, Task> sendCanIdChangeMessagesAsync,
        Action<string> updateLatestCanIdLabel,
        Action hideInputControls,
        Func<string, string, Task<bool>> showConfirmDialogAsync,
        Func<string, Task> showErrorDialogAsync,
        Action<string> setCurrentCanId)
    {
        var newCanId = getNewCanId()?.Trim();
        var currentCanId = getCurrentCanId();

        // Validation
        if (string.IsNullOrEmpty(newCanId) || newCanId.Length > 2 || !int.TryParse(newCanId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            await showErrorDialogAsync("Please enter a valid 2-digit hex CAN ID.");
            return;
        }

        string newId = newCanId.ToUpper().PadLeft(2, '0');
        string currentId = currentCanId ?? "00";

        // Confirm
        bool sure = await showConfirmDialogAsync($"Are you sure you want to change the CAN ID to {newId}?", "Yes");
        if (!sure)
            return;

        // Hide input controls
        hideInputControls();

        // Device-specific message sequence
        await sendCanIdChangeMessagesAsync(currentId, newId);

        // Update state/UI
        setCurrentCanId(newId);
        updateLatestCanIdLabel(newId);
    }
#endif
}

public class CanMessageViewModel
{
    public string Direction { get; set; } = ""; // "Rx" or "Tx"
    public string Id { get; set; } = "";
    public string Data { get; set; } = "";
}
