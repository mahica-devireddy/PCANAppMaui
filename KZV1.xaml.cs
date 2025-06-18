using System;
using System.Globalization;
using Peak.Can.Basic;
using PCANAppM.Services;

#if WINDOWS
using PCANAppM.Platforms.Windows;
#endif

namespace PCANAppM;

public partial class KZV : ContentPage
{
#if WINDOWS
    private PCAN_USB? _pcanUsb;
    private CanIdUpdateService? _canIdUpdater;
    private ushort _currentHandle;
    private bool _isStarted = false;
#endif

    private string? _currentCanId = null;

    public KZV()
    {
        InitializeComponent();
        InitializeCAN();
    }

#if WINDOWS
    private void InitializeCAN()
    {
        var devices = PCAN_USB.GetUSBDevices();
        if (devices != null && devices.Count > 0)
        {
            var handle = PCAN_USB.DecodePEAKHandle(devices[0]);
            var baud = "250 kbit/s";

            _pcanUsb = new PCAN_USB();
            SubscribeToPcanUsbEvents();

            var status = _pcanUsb.InitializeCAN(handle, baud, true);
            if (status == TPCANStatus.PCAN_ERROR_OK)
            {
                _currentHandle = handle;
                _isStarted = true;

                _canIdUpdater = new CanIdUpdateService(
                    _pcanUsb,
                    vm => Console.WriteLine($"[CAN] {vm.Id}: {vm.Data}")
                );

                _currentCanId = "--";
                UpdateLatestCanIdLabel(_currentCanId);
            }
        }
    }

    private void SubscribeToPcanUsbEvents()
    {
        if (_pcanUsb == null) return;
        _pcanUsb.MessageReceived += OnCanMessageReceived;
        _pcanUsb.Feedback        += OnPcanFeedback;
    }

    private void OnSetCanIdClicked(object sender, EventArgs e)
    {
        InitialKzvView.IsVisible = false;
        SetCanIdView.IsVisible = true;
        ConfirmCanIdView.IsVisible = false;
    }

    private void OnSetClicked(object sender, EventArgs e)
    {
        string? newId = NewCanIdEntry.Text?.Trim().ToUpperInvariant();

        if (string.IsNullOrEmpty(newId) ||
            !int.TryParse(newId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            DisplayAlert("Invalid Input", "Please enter a valid hex CAN ID.", "OK");
            return;
        }

        SetCanIdView.IsVisible = false;
        ConfirmText.Text = $"Set The CAN ID to {newId}";
        ConfirmCanIdView.IsVisible = true;
    }

    private void OnConfirmClicked(object sender, EventArgs e)
    {
        _currentCanId = NewCanIdEntry.Text?.Trim().ToUpperInvariant();
        UpdateLatestCanIdLabel(_currentCanId);

#if WINDOWS
        _canIdUpdater?.UpdateKzValveCanId("00", _currentCanId ?? "00");
#endif

        ConfirmCanIdView.IsVisible = false;
        InitialKzvView.IsVisible = true;
    }

    private void OnCancelConfirmClicked(object sender, EventArgs e)
    {
        ConfirmCanIdView.IsVisible = false;
        InitialKzvView.IsVisible = true;
    }

    private void UpdateLatestCanIdLabel(string id)
    {
        LatestCanIdLabel.Text = $"Current CAN ID: {id}";
    }

    private void OnCanMessageReceived(PCAN_USB.Packet packet)
    {
        var idHex = $"0x{packet.Id:X}";
        var last2 = idHex.Substring(idHex.Length - 2);
        _currentCanId = last2;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateLatestCanIdLabel(_currentCanId);
        });
    }

    private void OnPcanFeedback(string message)
    {
        Console.WriteLine($"[PCAN Feedback] {message}");
    }
#endif
}

public class CanIdUpdateService
{
    private readonly PCAN_USB _pcanUsb;
    private readonly Action<CanMessageViewModel> _logger;

    public CanIdUpdateService(PCAN_USB pcanUsb, Action<CanMessageViewModel> logger)
    {
        _pcanUsb = pcanUsb;
        _logger = logger;
    }

    public void UpdateKzValveCanId(string oldId, string newId)
    {
        _logger(new CanMessageViewModel
        {
            Direction = "Info",
            Id = $"0x{oldId}",
            Data = $"CAN ID changed to 0x{newId}"
        });
    }
}

public class CanMessageViewModel
{
    public string Direction { get; set; } = "";
    public string Id        { get; set; } = "";
    public string Data      { get; set; } = "";
}
