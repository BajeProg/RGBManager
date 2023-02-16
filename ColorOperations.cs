using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace RGB_Manager
{
    public class ColorOperations
    {
        private const int _buffetLength = 16;
        private Queue<int> _buffer;

        public ColorOperations()
        {
            _buffer = new Queue<int>();
            for (int i = 0; i < _buffetLength; i++) _buffer.Enqueue(0);
        }
        public static OpenRGB.NET.Models.Color Lerp(OpenRGB.NET.Models.Color startColor, OpenRGB.NET.Models.Color finishColor, float persend)
        {
            if (persend > 1) persend = 1;
            return new OpenRGB.NET.Models.Color(
                (byte)(startColor.R + (finishColor.R - startColor.R) * persend),
                (byte)(startColor.G + (finishColor.G - startColor.G) * persend),
                (byte)(startColor.B + (finishColor.B - startColor.B) * persend)
                );
        }
        public OpenRGB.NET.Models.Color PeekColorChange(int value)
        {
            _buffer.Dequeue();
            Random random = new Random();
            foreach (int i in _buffer)
                if (i > value || i == 0) 
                {
                    _buffer.Enqueue(value);
                    return new OpenRGB.NET.Models.Color(); 
                }
            _buffer.Enqueue(value);
            return ColorNormalization(new OpenRGB.NET.Models.Color((byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255)));
        }
        public static OpenRGB.NET.Models.Color HEXtoRGB(string HEX)
        {
            byte R = 0, G = 0, B = 0;

            if (HEX[0] <= '9') R += (byte)(HEX[0] * 16);
            else R += (byte)((HEX[0] - '7') * 16);
            if (HEX[1] <= '9') R += (byte)HEX[1];
            else R += (byte)(HEX[1] - '7');

            if (HEX[2] <= '9') G += (byte)(HEX[2] * 16);
            else G += (byte)((HEX[2] - '7') * 16);
            if (HEX[3] <= '9') G += (byte)HEX[3];
            else G += (byte)(HEX[3] - '7');

            if (HEX[4] <= '9') B += (byte)(HEX[4] * 16);
            else B += (byte)((HEX[4] - '7') * 16);
            if (HEX[5] <= '9') B += (byte)HEX[5];
            else B += (byte)(HEX[5] - '7');

            return new OpenRGB.NET.Models.Color(R, G, B);
        }
        public static string RGBtoHEX(byte r, byte g, byte b)
        {
            return DecimalToHexadecimal(r) + DecimalToHexadecimal(g) + DecimalToHexadecimal(b);
        }
        private static string DecimalToHexadecimal(byte decNum)
        {
            string ans = "";
            int div = decNum / 16, mod = decNum % 16;
            if (div < 10) ans += div.ToString();
            else
                switch (div)
                {
                    case 10: ans += "A"; break;
                    case 11: ans += "B"; break;
                    case 12: ans += "C"; break;
                    case 13: ans += "D"; break;
                    case 14: ans += "E"; break;
                    case 15: ans += "F"; break;
                }
            if (mod < 10) ans += mod.ToString();
            else
                switch (mod)
                {
                    case 10: ans += "A"; break;
                    case 11: ans += "B"; break;
                    case 12: ans += "C"; break;
                    case 13: ans += "D"; break;
                    case 14: ans += "E"; break;
                    case 15: ans += "F"; break;
                }
            return ans;
        }
        private OpenRGB.NET.Models.Color ColorNormalization(OpenRGB.NET.Models.Color color)
        {
            float coefficient = 255f / Math.Max(color.R, Math.Max(color.G, color.B));
            return new OpenRGB.NET.Models.Color((byte)(color.R * coefficient), (byte)(color.G * coefficient), (byte)(color.B * coefficient));
        }
        public static OpenRGB.NET.Models.Color ColorContrastNormalization(Color color)
        {
            byte[] _color = new byte[3];
            _color[0] = color.R;
            _color[1] = color.G;
            _color[2] = color.B;
            bool falg = true;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (_color[i] > _color[j]) break;
                    if (j == 2) falg = false;
                }
                if (falg) continue;
                _color[i] /= 2;
                break;
            }
            return new OpenRGB.NET.Models.Color(_color[0], _color[1], _color[2]);
        }

    }
}
