using System.Drawing;
using System.Windows.Forms;

namespace RGB_Manager
{
    class ImageCreator
    {
        public static Bitmap MakeScreenshot()
        {
            Rectangle bounds = Screen.GetBounds(System.Drawing.Point.Empty);
            var bitmap = new Bitmap(bounds.Width, bounds.Height);
            using (var gr = Graphics.FromImage(bitmap))
                gr.CopyFromScreen(new System.Drawing.Point(0, 0), new System.Drawing.Point(0, 0), bounds.Size);
            return bitmap;
        }
    }
}
