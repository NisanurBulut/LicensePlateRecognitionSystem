using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace PlakaTanima.Wpf
{
    internal static class BitmapConversion
    {
        /// <summary>System.Drawing.Bitmap → donmuş (her thread'den okunabilir) BitmapSource.</summary>
        public static BitmapSource ToBitmapSource(this Bitmap bitmap)
        {
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Bmp);
                stream.Position = 0;
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
        }
    }
}
