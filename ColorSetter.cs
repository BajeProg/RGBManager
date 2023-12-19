using OpenRGB.NET.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RGB_Manager
{
    abstract class ColorSetter
    {
        public abstract void SetColor(Color color);
        public abstract void TurningOff();
        public abstract void SetColor(Color color, int timeInMilliseconds);
        public abstract Task SetScreenColorForLeds();
        public abstract Task RainbowColorByTime();
    }
}
