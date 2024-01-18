using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Management;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;

namespace RGB_Manager
{
    public partial class MainWindow : Window
    {
        private ColorSetter colorSetter;
        private readonly Forms.NotifyIcon _trayIcon;

        public MainWindow()
        {
            InitializeComponent();
            _trayIcon = new Forms.NotifyIcon();
            colorSetter = new OpenRGBAPI();
            InitializeTrayIcon();
            InitializeRegistryKeys();
            //InitializeShotdownWatcher();

            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "-silent": Hide(); _trayIcon.Visible = true; break;
                }
            }
        }

        private void InitializeShotdownWatcher()
        {
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_ComputerShutdownEvent");
            ManagementEventWatcher watcher = new ManagementEventWatcher(query);
            watcher.EventArrived += Watcher_EventArrived;
            watcher.Start();
        }

        private void InitializeTrayIcon()
        {
            _trayIcon.Icon = new System.Drawing.Icon(@"D:\RGB Manager\RGB Manager\Icon.ico");
            _trayIcon.Text = "RGB Manager";
            _trayIcon.ContextMenuStrip = new Forms.ContextMenuStrip();
            Forms.ToolStripButton toolStripOpenButton = new Forms.ToolStripButton("Open");
            toolStripOpenButton.Click += onTrayOpenClicked;
            _trayIcon.ContextMenuStrip.Items.Add(toolStripOpenButton);
            Forms.ToolStripButton toolStripCloseButton = new Forms.ToolStripButton("Close");
            toolStripCloseButton.Click += onTrayCloseClicked;
            _trayIcon.ContextMenuStrip.Items.Add(toolStripCloseButton);
            Forms.ToolStripButton toolStripOffButton = new Forms.ToolStripButton("Off");
            toolStripOffButton.Click += (a, e) => { colorSetter.TurningOff(); };
            _trayIcon.ContextMenuStrip.Items.Add(toolStripOffButton);
        }

        private void InitializeRegistryKeys()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\RGB Manager"))
            {
                if ((int?)key?.GetValue("Close OpenRGB") == 1) CloseOpenRGBCheckBox.IsChecked = true;
                else CloseOpenRGBCheckBox.IsChecked = false;

                if ((int?)key?.GetValue("Tray") == 1) TrayIconWhenClosedCheckBox.IsChecked = true;
                else TrayIconWhenClosedCheckBox.IsChecked = false;

                if ((int?)key?.GetValue("Turning off") == 1) TurningOffWhenClosedCheckBox.IsChecked = true;
                else TurningOffWhenClosedCheckBox.IsChecked = false;
            }
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"))
            {
                foreach(string keyName in key.GetValueNames())
                    if (keyName == "RGBManager") StartWhenWindowsStartedCheckBox.IsChecked = true;
                    else StartWhenWindowsStartedCheckBox.IsChecked = false;
            }
        }

        private void onTrayCloseClicked(object sender, EventArgs e)
        {
            Close();
        }

        private void onTrayOpenClicked(object sender, EventArgs e)
        {
            Show();
            _trayIcon.Visible = false;
        }

        private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            colorSetter.SetColor(new OpenRGB.NET.Models.Color(e.NewValue.Value.R, e.NewValue.Value.G, e.NewValue.Value.B));
        }

        private void Mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //string modeName = ((TextBlock)e.AddedItems[0]).Text;
            string modeName = (string)((ComboBoxItem)e.AddedItems[0]).Content;
            Modes.Mode? mode = null;
            foreach(var item in Enum.GetValues(typeof(Modes.Mode)))
            {
                if (modeName == Enum.GetName(typeof(Modes.Mode), item))
                {
                    mode = (Modes.Mode)item;
                    Modes.SetMode((Modes.Mode)mode);
                    break;
                }
            }
            if (mode == null) return;
            switch (mode)
            {
                case Modes.Mode.Static:
                    ColorPicker.Visibility = Visibility.Visible;
                    RainbowModeSpeedSP.Visibility = Visibility.Collapsed;
                    break;
                case Modes.Mode.Screen:
                    ColorPicker.Visibility = Visibility.Collapsed;
                    RainbowModeSpeedSP.Visibility = Visibility.Collapsed;
                    break;
                case Modes.Mode.Music:
                    ColorPicker.Visibility = Visibility.Collapsed;
                    RainbowModeSpeedSP.Visibility = Visibility.Collapsed;
                    break;
                case Modes.Mode.Rainbow:
                    ColorPicker.Visibility = Visibility.Collapsed;
                    RainbowModeSpeedSP.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _trayIcon.Dispose();
            Modes.UnsetAll();
            using (RegistryKey key = Registry.CurrentUser?.OpenSubKey(@"Software\RGB Manager"))
                if (key != null && (int?)key?.GetValue("Close OpenRGB") == 1)
                {
                    Process[] list = Process.GetProcesses();
                    foreach (Process pr in list)
                        if (pr.ProcessName == "OpenRGB") pr.Kill();
                }
        }

        private void CloseOpenRGBCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\RGB Manager"))
                key?.SetValue("Close OpenRGB", 1);
        }

        private void CloseOpenRGBCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\RGB Manager"))
                key?.SetValue("Close OpenRGB", 0);
        }

        private void StartWhenWindowsStartedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (Environment.CurrentDirectory == Environment.SystemDirectory) return;
                key?.SetValue("RGBManager", Environment.CurrentDirectory + @"\RGB Manager.exe -silent");
            }
        }

        private void StartWhenWindowsStartedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"))
                key?.DeleteValue("RGBManager");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if ((bool)!TrayIconWhenClosedCheckBox.IsChecked || _trayIcon.Visible) return;
            e.Cancel = true;
            Hide();
            _trayIcon.Visible = true;
        }

        private void TrayIconWhenClosedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\RGB Manager"))
                key?.SetValue("Tray", 1);
        }

        private void TrayIconWhenClosedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\RGB Manager"))
                key?.SetValue("Tray", 0);
        }

        private void RainbowModeSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(RainbowModeSpeedText == null) return;
            RainbowModeSpeedText.Text = ((int)(RainbowModeSpeed.Maximum + 1 - e.NewValue)).ToString();
        }

        private void TurningOffWhenClosedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\RGB Manager"))
                key?.SetValue("Turning off", 1);
        }

        private void TurningOffWhenClosedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\RGB Manager"))
                key?.SetValue("Turning off", 0);
        }

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if(!TurningOffWhenClosedCheckBox.IsChecked.Value) return;
            switch (e.Mode)
            {
                case PowerModes.Suspend: colorSetter.TurningOff(); break;
                case PowerModes.Resume: colorSetter.TurningOn(); break;
            }
        }

        private void Watcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            colorSetter.TurningOff();
        }
    }
}
