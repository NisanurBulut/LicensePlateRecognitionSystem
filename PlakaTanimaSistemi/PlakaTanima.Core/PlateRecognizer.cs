using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using akıllıgecissistemleri;

namespace PlakaTanima.Core
{
    /// <summary>
    /// Eski WinForms ekranındaki (OCR.button2_Click) plaka bulma hattı: aynı filtreler, aynı eşikler,
    /// ama arayüzden bağımsız ve her adımın görüntüsünü dışarı veriyor.
    /// </summary>
    public sealed class PlateRecognizer
    {
        public const int StepCount = 13;

        // Eski arayüzdeki PictureBox boyutu. Blob eşikleri (70x40, 50x200) bu ölçeğe göre ayarlı.
        private const int MaxWidth = 480;
        private const int MaxHeight = 360;

        private const float MinAspect = 3f;
        private const float MaxAspect = 5.7f;

        [DllImport("OtsuEsikleme.dll")]
        private static extern void OtsuEsikleme(ref byte pixelDizisi, ref byte esikDeger, int genislik, int yukseklik);

        public PlateRecognitionResult Recognize(Bitmap original, IProgress<PipelineStep> progress = null)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));

            var watch = Stopwatch.StartNew();
            int step = 0;
            void Report(string title, string description, Bitmap image)
            {
                progress?.Report(new PipelineStep(++step, StepCount, title, description, new Bitmap(image)));
            }

            Bitmap source = FitToWorkArea(original);
            Report("Orijinal", "İşlem için " + source.Width + "×" + source.Height + " boyutuna getirilen görüntü", source);

            Bitmap gray = ToGrayscale(source);
            Report("Gri seviye", "Her piksel R, G ve B'nin ortalamasına indirgenir", gray);

            Bitmap median = ExtBitmap.MedianFilter(gray, 3);
            Report("Median filtre", "3×3 medyan ile gürültü bastırılır, kenarlar korunur", median);

            Bitmap sobel = ExtBitmap.Sobel3x3Filter(median, true);
            Report("Sobel kenar", "Yatay ve dikey gradyanla karakter kenarları öne çıkarılır", sobel);

            Bitmap otsu = Otsu(sobel);
            Report("Otsu eşikleme", "Görüntü, otomatik eşikle siyah-beyaza çevrilir", otsu);

            Bitmap erosion = ExtBitmap.DilateAndErodeFilter(otsu, 3, ExtBitmap.MorphologyType.Erosion, true, true, true);
            Report("Erozyon", "3×3 aşındırma ile ince gürültü silinir", erosion);

            Bitmap dilation = ExtBitmap.DilateAndErodeFilter(erosion, 7, ExtBitmap.MorphologyType.Dilation, true, true, true);
            Report("Genişletme", "7×7 genişletme ile karakterler birbirine bağlanır", dilation);

            Bitmap blobs1 = new Bitmap(dilation);
            new BlobsFiltering { CoupledSizeFiltering = true, MinWidth = 70, MinHeight = 40 }.ApplyInPlace(blobs1);
            Report("Blob filtresi", "70×40'tan küçük bölgeler elenir", blobs1);

            Bitmap closing = ExtBitmap.CloseMorphologyFilter(blobs1, 15, true, true, true);
            Report("Kapama", "15×15 kapama ile bölgedeki boşluklar doldurulur", closing);

            Bitmap erosion2 = ExtBitmap.DilateAndErodeFilter(closing, 9, ExtBitmap.MorphologyType.Erosion, true, true, true);
            Report("Son erozyon", "9×9 aşındırma ile bölge sınırları sadeleştirilir", erosion2);

            Bitmap blobs2 = new Bitmap(erosion2);
            new BlobsFiltering { CoupledSizeFiltering = true, MinWidth = 50, MinHeight = 200 }.ApplyInPlace(blobs2);
            Report("Aday bölgeler", "Plaka olabilecek bağlı bölgeler kalır", blobs2);

            // --- Bölgeleri bul ve en/boy oranı plakaya uyanları seç ---
            var blobCounter = new BlobCounter();
            blobCounter.ProcessImage(blobs2);
            Blob[] blobs = blobCounter.GetObjectsInformation();

            var allBoxes = new List<Rectangle>();
            var candidates = new List<PlateCandidate>();
            foreach (Blob blob in blobs)
            {
                List<IntPoint> edgePoints = blobCounter.GetBlobsEdgePoints(blob);
                List<IntPoint> corners = PointsCloud.FindQuadrilateralCorners(edgePoints);
                Rectangle box = BoundingBox(corners);
                allBoxes.Add(Rectangle.FromLTRB(box.Left - 2, box.Top - 2, box.Right + 2, box.Bottom + 2));

                Rectangle plateBox = Rectangle.FromLTRB(box.Left - 5, box.Top - 5, box.Right, box.Bottom + 5);
                var candidate = new PlateCandidate(plateBox);
                if (candidate.AspectRatio >= MinAspect && candidate.AspectRatio <= MaxAspect)
                    candidates.Add(candidate);
            }

            Bitmap regions = new Bitmap(blobs2);
            DrawBoxes(regions, allBoxes, Color.FromArgb(0, 180, 216));      // ana renk #00B4D8
            Report("Bölge sınırları", blobs.Length + " bölge bulundu, mavi çerçeveyle işaretlendi", regions);

            // En büyük adayı plaka say.
            PlateCandidate best = candidates.OrderByDescending(c => c.Area).FirstOrDefault();

            Bitmap annotated = new Bitmap(source);
            DrawBoxes(annotated, candidates.Select(c => c.Bounds), Color.FromArgb(34, 197, 94), 3);   // başarı #22C55E

            Bitmap plateImage = best == null ? null : Crop(source, best.Bounds);
            Report("Plaka", best == null ? "Plakaya benzeyen bir bölge bulunamadı" : "Plaka bölgesi çerçevelendi", annotated);

            watch.Stop();
            return new PlateRecognitionResult(source, annotated, plateImage, candidates, best, blobs.Length, watch.ElapsedMilliseconds);
        }

        /// <summary>Eski <c>resimYukle</c> kuralıyla: pencereden taşacaksa en/boy oranını koruyarak küçültür.</summary>
        private static Bitmap FitToWorkArea(Bitmap original)
        {
            int width = original.Width, height = original.Height;
            if (width >= MaxWidth || height >= MaxHeight)
            {
                float ratio;
                if (width - MaxWidth > height - MaxHeight)
                {
                    ratio = (float)MaxWidth / width;
                    height = (int)(ratio * height);
                    width = MaxWidth;
                }
                else
                {
                    ratio = (float)MaxHeight / height;
                    width = (int)(ratio * width);
                    height = MaxHeight;
                }
            }

            var result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(original, 0, 0, width, height);
            }
            return result;
        }

        private static Bitmap ToGrayscale(Bitmap source)
        {
            var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            byte[] pixels = ReadPixels(source, out int stride);
            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte avg = (byte)((pixels[i] + pixels[i + 1] + pixels[i + 2]) / 3);
                pixels[i] = pixels[i + 1] = pixels[i + 2] = avg;
                pixels[i + 3] = 255;
            }
            WritePixels(result, pixels, stride);
            return result;
        }

        private static Bitmap Otsu(Bitmap source)
        {
            int width = source.Width, height = source.Height;
            byte[] argb = ReadPixels(source, out int stride);

            // Kütüphane tek boyutlu gri diziyle çalışır; gri seviyede R=G=B olduğundan R kanalı yeterli.
            byte[] gray = new byte[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    gray[y * width + x] = argb[y * stride + x * 4 + 2];

            byte threshold = 0;
            OtsuEsikleme(ref gray[0], ref threshold, width, height);

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int offset = y * stride + x * 4;
                    argb[offset] = argb[offset + 1] = argb[offset + 2] = gray[y * width + x];
                    argb[offset + 3] = 255;
                }

            var result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            WritePixels(result, argb, stride);
            return result;
        }

        private static byte[] ReadPixels(Bitmap bitmap, out int stride)
        {
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                stride = data.Stride;
                byte[] buffer = new byte[data.Stride * data.Height];
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
                return buffer;
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        private static void WritePixels(Bitmap bitmap, byte[] buffer, int stride)
        {
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                if (data.Stride == stride)
                {
                    Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
                }
                else
                {
                    for (int y = 0; y < data.Height; y++)
                        Marshal.Copy(buffer, y * stride, data.Scan0 + y * data.Stride, Math.Min(stride, data.Stride));
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        private static Rectangle BoundingBox(IEnumerable<IntPoint> points)
        {
            int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
            foreach (IntPoint p in points)
            {
                left = Math.Min(left, p.X);
                right = Math.Max(right, p.X);
                top = Math.Min(top, p.Y);
                bottom = Math.Max(bottom, p.Y);
            }
            return left > right ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right, bottom);
        }

        private static void DrawBoxes(Bitmap target, IEnumerable<Rectangle> boxes, Color color, float thickness = 2)
        {
            using (var g = Graphics.FromImage(target))
            using (var pen = new Pen(color, thickness))
            {
                foreach (Rectangle box in boxes)
                    g.DrawRectangle(pen, box);
            }
        }

        private static Bitmap Crop(Bitmap source, Rectangle bounds)
        {
            Rectangle area = Rectangle.Intersect(bounds, new Rectangle(0, 0, source.Width, source.Height));
            if (area.Width <= 0 || area.Height <= 0)
                return null;
            return source.Clone(area, PixelFormat.Format32bppArgb);
        }
    }
}
