using Microsoft.Win32;
using OpenRGB.NET;
using OpenRGB.NET.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RGB_Manager
{
    internal class OpenRGBAPI : ColorSetter
    {
        private static Color lastColor = null;
        private static OpenRGBClient client;

        public OpenRGBAPI()
        {
            InitializeOpenRGB();
        }
        private void InitializeOpenRGB()
        {
            try
            {
                Process[] list = Process.GetProcesses();
                foreach (Process pr in list)
                {
                    if (pr.ProcessName == "OpenRGB")
                    {
                        client = new OpenRGBClient();
                        return;
                    }
                }
                Process process = new Process();
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\RGB Manager"))
                {
                    if (key != null)
                    {
                        process.StartInfo.Arguments = $"--startminimized --server --mode static --color {key?.GetValue("Last color")}";
                        lastColor = ColorOperations.HEXtoRGB((string)key?.GetValue("Last color"));
                    }
                    else
                        process.StartInfo.Arguments = $"--startminimized --server --mode static";
                }
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\RGB Manager"))
                {
                    if (String.IsNullOrEmpty((string)key?.GetValue("OpenRGBPath")))
                        process.StartInfo.FileName = @"C:\Program Files (x86)\OpenRGB Windows 64-bit\OpenRGB.exe";
                    else
                        process.StartInfo.FileName = (string)key?.GetValue("OpenRGBPath");
                }
                try
                {
                    process.Start();
                }
                catch
                {

                    if (MessageBox.Show("У вас установлен OpenRGB? Мы не смогли найти его на вашем устройстве.", "OpenRGB missing", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        Process.Start("explorer", "https://openrgb.org/");
                        Application.Current.Shutdown();
                        return;
                    }
                    else
                    {
                        OpenFileDialog dialog = new OpenFileDialog();
                        dialog.Filter = "exe files (*.exe)|*.exe|All files (*.*)|*.*";
                        dialog.InitialDirectory = @"C:\Program Files";
                        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\RGB Manager"))
                            if ((bool)dialog?.ShowDialog())
                            {
                                process.StartInfo.FileName = dialog.FileName;
                                key?.SetValue("OpenRGBPath", dialog.FileName);
                            }
                        process.Start();
                    }
                }
                while (true)
                {
                    try
                    {
                        System.Threading.Thread.Sleep(1000);
                        client = new OpenRGBClient();
                        Device[] devices = client?.GetAllControllerData();
                        if (devices.Length == 0) continue;
                        break;
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        public override void SetColor(Color color)
        {
            if (client == null) return;
            try
            {
                Device[] devices = client.GetAllControllerData();
                for (int i = 0; i < devices.Length; i++)
                {
                    var leds = Enumerable.Range(0, devices[i].Colors.Length)
                        .Select(_ => color)
                        .ToArray();
                    client.UpdateLeds(i, leds);
                }
            }
            catch { }
            lastColor = color;
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\RGB Manager"))
                key?.SetValue("Last color", ColorOperations.RGBtoHEX(color.R, color.G, color.B));
        }
        public override void SetColor(Color color, int timeInMilliseconds)
        {
            if(lastColor != null && lastColor != color)
            {
                DateTime start = DateTime.Now;
                int time = 0;
                while (time <= timeInMilliseconds)
                {
                    time = (int)(DateTime.Now - start).TotalMilliseconds;
                    try
                    {
                        Device[] devices = client.GetAllControllerData();
                        for (int i = 0; i < devices.Length; i++)
                        {
                            var leds = Enumerable.Range(0, devices[i].Colors.Length)
                                .Select(_ => ColorOperations.Lerp(lastColor, color, time / (timeInMilliseconds * 1.0f)))
                                .ToArray();
                            client.UpdateLeds(i, leds);
                        }
                    }
                    catch { }
                }
            }
            lastColor = color;
        }

        public async override Task SetScreenColorForLeds()
        {
            await Task.Run(async () =>
            {
                try
                {
                    using (var bitmap = ImageCreator.MakeScreenshot())
                    {
                        List<System.Drawing.Color> colors = new List<System.Drawing.Color>();
                        for (int i = 0; i < bitmap.Width; i += 5)
                        {
                            for (int j = 0; j < bitmap.Height; j += 10)
                            {
                                var color = bitmap.GetPixel(i, j);
                                if (color.ToArgb() != System.Drawing.Color.Black.ToArgb())
                                    colors.Add(color);
                            }
                        }
                        colors.Sort((System.Drawing.Color x, System.Drawing.Color y) => {
                            float indx = x.GetBrightness() * x.GetSaturation() + x.GetHue();
                            float indy = y.GetBrightness() * y.GetSaturation() + y.GetHue();
                            if (indx > indy) return 1;
                            else if (indx < indy) return -1;
                            else return 0;
                        });
                        var c = colors.Count != 0 ? colors[colors.Count / 2] : System.Drawing.Color.Black;
                        SetColor(ColorOperations.ColorContrastNormalization(System.Windows.Media.Color.FromRgb(c.R, c.G, c.B)), 500);
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            });
        }
        public async override Task ChangeColorByTime()
        {
            await Task.Run(async () =>
            {
                Color color = null;
                if (lastColor == null) lastColor = new Color(255);
                float hue = System.Drawing.Color.FromArgb(lastColor.R, lastColor.G, lastColor.B).GetHue() + 1f;
                int Hi = (int)(hue / 60) % 6;
                int Vinc = (int)Math.Round(hue % 60 * 10 / 6);
                int Vdec = 100 - Vinc;

                switch (Hi)
                {
                    case 0: color = new Color(255, (byte)(Vinc * 255 / 100)); break;
                    case 1: color = new Color((byte)(Vdec * 255 / 100), 255); break;
                    case 2: color = new Color(0, 255, (byte)(Vinc * 255 / 100)); break;
                    case 3: color = new Color(0, (byte)(Vdec * 255 / 100), 255); break;
                    case 4: color = new Color((byte)(Vinc * 255 / 100), 0, 255); break;
                    case 5: color = new Color(255, 0, (byte)(Vdec * 255 / 100)); break;
                }

                if (color == null) return;
                SetColor(color);
            });
        }
    }
}
