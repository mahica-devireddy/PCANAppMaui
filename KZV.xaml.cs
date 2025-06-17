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

public partial class KZV : ContentPage
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

    public KZV()
    {
        InitializeComponent();
        CanMessagesView.ItemsSource = _canMessages;

#if WINDOWS
        foreach (var dev in PCAN_USB.GetUSBDevices())
            DevicePicker1.Items.Add(dev);

        foreach (var rate in PCAN_USB.CANBaudRates)
            BaudRatePicker1.Items.Add(rate);

        if (BaudRatePicker1.Items.Contains("250 kbit/s"))
            BaudRatePicker1.SelectedIndex = BaudRatePicker1.Items.IndexOf("250 kbit/s");

        StartStopButton1.IsEnabled = DevicePicker1.Items.Count > 0 && BaudRatePicker1.SelectedIndex >= 0;
        SendButton1.IsEnabled      = false;
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
            if (DevicePicker1.SelectedIndex < 0 || BaudRatePicker1.SelectedIndex < 0)
            {
                _canMessages.Insert(0, new CanMessageViewModel { Data = "Select device and baud rate." });
                return;
            }

            var handle = PCAN_USB.DecodePEAKHandle(DevicePicker1.SelectedItem.ToString());
            var baud   = BaudRatePicker1.SelectedItem.ToString();

            _pcanUsb = new PCAN_USB();
            SubscribeToPcanUsbEvents();

            var status = _pcanUsb.InitializeCAN(handle, baud, true);
            if (status == TPCANStatus.PCAN_ERROR_OK)
            {
                _currentHandle = handle;
                _isStarted     = true;
                StartStopButton1.Text = "Stop";
                SendButton1.IsEnabled  = true;
                _canMessages.Insert(0, new CanMessageViewModel { Data = "CAN started." });

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
            StartStopButton1.Text = "Start";
            SendButton1.IsEnabled  = false;
            _canMessages.Insert(0, new CanMessageViewModel { Data = "CAN stopped." });
        }
    }
    private void OnSendClicked(object s,EventArgs e)
    {
            if(_pcanUsb==null||!_isStarted)return;
            if(!uint.TryParse(CanIdEntry1.Text,out var cid)){_canMessages.Insert(0,new CanMessageViewModel{Data="Invalid CAN ID."});return;}
            var pr=DataEntry1.Text?.Split(' ',StringSplitOptions.RemoveEmptyEntries);if(pr==null){_canMessages.Insert(0,new CanMessageViewModel{Data="No data."});return;}
            var d=new byte[8];int l=0;foreach(var p in pr){if(l>=8)break;if(byte.TryParse(p,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out var b))d[l++]=b;else{_canMessages.Insert(0,new CanMessageViewModel{Data=$"Invalid byte: {p}"});return;}}
            if(!int.TryParse(LengthEntry1.Text,out var dl)||dl<0||dl>8)dl=l;
            _pcanUsb.WriteFrame(cid,dl,d,cid>0x7FF);
            _canMessages.Insert(0,new CanMessageViewModel{Direction="Tx",Id=$"0x{cid:X}",Data=string.Join(" ",d.Take(dl).Select(b=>b.ToString("X2")))});
    }

    private async void OnConfirmCanIdButtonClicked(object sender, EventArgs e)
    {
        var newCanId = NewCanIdEntry1.Text?
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

        _canIdUpdater?.UpdateKzValveCanId(_currentCanId ?? "00", newCanId);

        _currentCanId = newCanId;
        UpdateLatestCanIdLabel1(newCanId);
    }
#endif

    private void UpdateLatestCanIdLabel1(string id)
    {
        LatestCanIdLabel1.Text = $"Latest CAN ID: {id}";
    }

    private void OnCanIdEntryChanged(object sender, TextChangedEventArgs e)
    {
#if WINDOWS
        if (_bChanging) return;
        _bChanging = true;
        if (uint.TryParse(CanIdEntry1.Text, out var dec))
            CanIdHexEntry1.Text = dec.ToString("X");
        else
            CanIdHexEntry1.Text = string.Empty;
        _bChanging = false;
#endif
    }

    private void OnCanIdHexEntryChanged(object sender, TextChangedEventArgs e)
    {
#if WINDOWS
        if (_bChanging) return;
        _bChanging = true;
        if (uint.TryParse(CanIdHexEntry1.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
            CanIdEntry1.Text = hex.ToString();
        else
            CanIdEntry1.Text = string.Empty;
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
