using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Peak.Can.Basic;
using PCANAppM.Services;

#if WINDOWS
using PCANAppM.Platforms.Windows;
#endif

namespace PCANAppM;

public partial class BAS : ContentPage
{
    private ObservableCollection<CanMessageViewModel> _canMessages = new();
#if WINDOWS
    private PCAN_USB? _pcanUsb;
    private CanIdUpdateService? _canIdUpdater;
    private ushort _currentHandle;
    private bool _isStarted = false;
    private bool _bChanging  = false;
#endif

    private string? _currentCanId = null;

    public BAS()
    {
        InitializeComponent();
        CanMessagesView.ItemsSource = _canMessages;

#if WINDOWS
        foreach (var dev in PCAN_USB.GetUSBDevices())
            DevicePicker.Items.Add(dev);

        foreach (var rate in PCAN_USB.CANBaudRates)
            BaudRatePicker.Items.Add(rate);

        if (BaudRatePicker.Items.Contains("250 kbit/s"))
            BaudRatePicker.SelectedIndex = BaudRatePicker.Items.IndexOf("250 kbit/s");

        StartStopButton.IsEnabled = DevicePicker.Items.Count > 0 && BaudRatePicker.SelectedIndex >= 0;
        SendButton.IsEnabled      = false;
#endif
    }

#if WINDOWS
    private void SubscribeToPcanUsbEvents()
    {
        if (_pcanUsb == null) return;
        _pcanUsb.MessageReceived += OnCanMessageReceived;
        _pcanUsb.Feedback        += OnPcanFeedback;
    }

    private void OnStartStopClicked(object sender, EventArgs e)
    {
        if (!_isStarted)
        {
            if (DevicePicker.SelectedIndex < 0 || BaudRatePicker.SelectedIndex < 0)
            {
                _canMessages.Insert(0, new CanMessageViewModel { Data = "Select device and baud rate." });
                return;
            }

            var handle = PCAN_USB.DecodePEAKHandle(DevicePicker.SelectedItem.ToString());
            var baud   = BaudRatePicker.SelectedItem.ToString();

            _pcanUsb = new PCAN_USB();
            SubscribeToPcanUsbEvents();

            var status = _pcanUsb.InitializeCAN(handle, baud, true);
            if (status == TPCANStatus.PCAN_ERROR_OK)
            {
                _currentHandle = handle;
                _isStarted     = true;
                StartStopButton.Text = "Stop";
                SendButton.IsEnabled  = true;
                _canMessages.Insert(0, new CanMessageViewModel { Data = "CAN started." });

                // instantiate the shared service
                _canIdUpdater = new CanIdUpdateService(
                    _pcanUsb,
                    vm => _canMessages.Insert(0, vm)
                );
            }
            else
            {
                _canMessages.Insert(0, new CanMessageViewModel { Data = $"Init failed: {_pcanUsb.PeakCANStatusErrorString(status)}" });
            }
        }
        else
        {
            _pcanUsb?.Uninitialize();
            _isStarted = false;
            StartStopButton.Text = "Start";
            SendButton.IsEnabled  = false;
            _canMessages.Insert(0, new CanMessageViewModel { Data = "CAN stopped." });
        }
    }

    private async void OnConfirmCanIdButtonClicked(object sender, EventArgs e)
    {
        var newCanId = NewCanIdEntry.Text?
            .Trim()
            .ToUpperInvariant()
            .PadLeft(2, '0');

        if (string.IsNullOrEmpty(newCanId) || newCanId.Length != 2 ||
            !int.TryParse(newCanId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            await DisplayAlert("Invalid Input", "Please enter a valid 2-digit hex CAN ID.", "OK");
            return;
        }

        bool sure = await DisplayAlert("Confirm", $"Change CAN ID to {newCanId}?", "Yes", "No");
        if (!sure) return;

        _canIdUpdater?.UpdateAngleSensorCanId(_currentCanId ?? "00", newCanId);

        _currentCanId = newCanId;
        UpdateLatestCanIdLabel(newCanId);
    }
#endif

    private void UpdateLatestCanIdLabel(string id)
    {
        LatestCanIdLabel.Text = $"Latest CAN ID: {id}";
    }

    private void OnCanIdEntryChanged(object sender, TextChangedEventArgs e)
    {
#if WINDOWS
        if (_bChanging) return;
        _bChanging = true;
        if (uint.TryParse(CanIdEntry.Text, out var dec))
            CanIdHexEntry.Text = dec.ToString("X");
        else
            CanIdHexEntry.Text = string.Empty;
        _bChanging = false;
#endif
    }

    private void OnCanIdHexEntryChanged(object sender, TextChangedEventArgs e)
    {
#if WINDOWS
        if (_bChanging) return;
        _bChanging = true;
        if (uint.TryParse(CanIdHexEntry.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
            CanIdEntry.Text = hex.ToString();
        else
            CanIdEntry.Text = string.Empty;
        _bChanging = false;
#endif
    }

    private void OnCanMessageReceived(PCAN_USB.Packet packet)
    {
#if WINDOWS
        var idHex = $"0x{packet.Id:X}";
        var last2 = idHex.Substring(idHex.Length - 2);
        _currentCanId = last2;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _canMessages.Insert(0, new CanMessageViewModel
            {
                Direction = "Rx",
                Id        = idHex,
                Data      = string.Join(" ", packet.Data.Take(packet.Length).Select(b => b.ToString("X2")))
            });
            if (_canMessages.Count > 100)
                _canMessages.RemoveAt(100);
        });
#endif
    }

    private void OnPcanFeedback(string message)
    {
#if WINDOWS
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _canMessages.Insert(0, new CanMessageViewModel { Data = message });
            if (_canMessages.Count > 100)
                _canMessages.RemoveAt(100);
        });
#endif
    }
}

public class CanMessageViewModel
{
    public string Direction { get; set; } = "";
    public string Id        { get; set; } = "";
    public string Data      { get; set; } = "";
}
