using System;
using System.Collections.Generic;
using System.Timers;
using Microsoft.Maui.Controls;
using Timer = System.Timers.Timer;
using System.Threading.Tasks;   

#if WINDOWS
using PCANAppMaui.Platforms.Windows;
#endif

namespace PCANAppMaui
{
    public partial class MainPage : ContentPage
    {
        private Timer deviceCheckTimer;
        private string lastStatus = "";

        public MainPage()
        {
            InitializeComponent();
            StartDeviceCheckTimer();
        }

        private void StartDeviceCheckTimer()
        {
            deviceCheckTimer = new Timer(1000); // 1 second interval  
            deviceCheckTimer.Elapsed += DeviceCheckTimer_Elapsed;
            deviceCheckTimer.AutoReset = true;
            deviceCheckTimer.Start();
        }

        private void DeviceCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            UpdateDeviceStatus();
        }

        private void UpdateDeviceStatus()
        {
            // Only run on Windows  
#if WINDOWS
        var devices = PCAN_USB.GetUSBDevices();
        string status;
        string imageSource;
        if (devices != null && devices.Count > 0)
        {
            status = $"{devices[0]} Device Connected";
            imageSource = "green_check.jpg"; // Use your connected image
        }
        else
        {
            status = "PCAN Device NOT Connected.";
            imageSource = "red_ex.jpg"; // Use your disconnected image
        }

        if (status != lastStatus)
        {
            lastStatus = status;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = status;
                if (status.Contains("Device Connected")) 
                {
                    StatusImage1.Source = imageSource;
                } else if (status.Contains("NOT Connected")) 
                {
                    StatusImage1.Source = imageSource;
                }
    });
}
#endif
        }
    }
}

