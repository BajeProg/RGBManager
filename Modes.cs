using NAudio.Wave;
using System;
using System.Threading.Tasks;

namespace RGB_Manager
{
    internal class Modes
    {
        private static Mode currentMode = Mode.Static;
        private static WasapiLoopbackCapture wave = new WasapiLoopbackCapture();
        private static ColorOperations ColorOperations = new ColorOperations();
        private static ColorSetter colorSetter = new OpenRGBAPI();

        private delegate Task CompeteModeHandler();
        private static CompeteModeHandler CompeteMode;
        public delegate void ModeChangedEventHandler(Mode mode);
        public static event ModeChangedEventHandler ModeChanged;

        public enum Mode
        {
            Static,
            Screen,
            Music,
            Rainbow
        }

        public static void SetMode(Mode mode)
        {
            UnsetAll();
            currentMode = mode;
            if(ModeChanged != null) ModeChanged(mode);
            switch (mode)
            {
                case Mode.Screen:
                    CompeteMode = colorSetter.SetScreenColorForLeds;
                    break;
                case Mode.Music:
                    wave.DataAvailable += SetColorByMusicWawe;
                    CompeteMode = null;
                    break;
                case Mode.Rainbow:
                    CompeteMode = colorSetter.ChangeColorByTime;
                    break;
                case Mode.Static:
                    CompeteMode = null;
                    break;
            }
            Complete(mode);
        }

        public static void UnsetAll()
        {
            wave.DataAvailable -= SetColorByMusicWawe;
            wave.StopRecording();
        }

        private static async void Complete(Mode mode)
        {
            if (CompeteMode == null) return;
            if (mode != currentMode) return;
            await CompeteMode();
            await Task.Delay(100);
            Complete(mode);
        }

        private static void SetColorByMusicWawe(object sender, WaveInEventArgs e)
        {
            WaveBuffer buffer = new WaveBuffer(e.Buffer);
            try
            {
                OpenRGB.NET.Models.Color color = new OpenRGB.NET.Models.Color();
                try
                {
                    float max = 0;
                    for (int index = 0; index < e.BytesRecorded / 4; index++)
                    {
                        float sample = buffer.FloatBuffer[index];

                        // absolute value 
                        if (sample < 0) sample = -sample;
                        // is this the max value?
                        if (sample > max) max = sample;
                    }
                    //color = ColorOperations.RainbowColor((int)(max * 1530));
                    color = ColorOperations.PeekColorChange((int)(max * 100));
                }
                catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message); }

                if (color.R > 0 || color.G > 0 || color.B > 0) colorSetter.SetColor(color);
            }
            catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message); wave.StopRecording(); }
        }

    }
}
