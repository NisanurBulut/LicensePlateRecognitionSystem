using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using tesseract;

namespace PlakaTanima.Core
{
    public sealed class PlateText
    {
        public PlateText(string text, string raw, bool isValidFormat)
        {
            Text = text;
            Raw = raw;
            IsValidFormat = isValidFormat;
        }

        /// <summary>Okunan plaka; biçim doğrulandıysa "35 AF 8952" gibi boşluklu.</summary>
        public string Text { get; }
        /// <summary>OCR'ın temizlenmiş ham çıktısı.</summary>
        public string Raw { get; }
        /// <summary>İl kodu + harf grubu + rakam grubu biçimine uyuyor mu.</summary>
        public bool IsValidFormat { get; }
    }

    /// <summary>
    /// Kırpılmış plaka görüntüsündeki karakterleri okur (Tesseract 3.01 sarmalayıcısı).
    /// Veri dosyası yoksa ya da motor başlatılamazsa <see cref="IsAvailable"/> false olur; uygulama çalışmaya devam eder.
    /// </summary>
    public sealed class PlateTextReader : IDisposable
    {
        private const string Language = "eng";
        private const string Whitelist = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        // 01-81 il kodu, 1-3 harf, 2-4 rakam.
        private static readonly Regex TurkishPlate =
            new Regex("(0[1-9]|[1-7][0-9]|8[01])([A-Z]{1,3})([0-9]{2,4})", RegexOptions.Compiled);

        private readonly object _gate = new object();
        private TesseractProcessor _engine;

        public PlateTextReader(string tessdataDirectory)
        {
            Initialize(tessdataDirectory);
        }

        public bool IsAvailable { get; private set; }
        public string UnavailableReason { get; private set; } = "Başlatılmadı";

        [HandleProcessCorruptedStateExceptions]
        private void Initialize(string tessdataDirectory)
        {
            string dataFile = Path.Combine(tessdataDirectory, Language + ".traineddata");
            if (!File.Exists(dataFile))
            {
                UnavailableReason = "tessdata\\" + Language + ".traineddata bulunamadı";
                return;
            }

            try
            {
                // 3.01 motoru dosya yolunu "klasör\" biçiminde bekler.
                string path = tessdataDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                _engine = CreateEngine(path);
                if (_engine == null)
                {
                    UnavailableReason = "OCR motoru veri dosyasını açamadı";
                    return;
                }

                IsAvailable = true;
                UnavailableReason = null;
            }
            catch (Exception ex) when (ex is AccessViolationException || ex is BadImageFormatException ||
                                        ex is FileLoadException || ex is FileNotFoundException ||
                                        ex is TypeInitializationException || ex is DllNotFoundException)
            {
                // Uyumsuz veri dosyası motorda bellek hatasına yol açabilir; OCR'ı kapatıp devam et.
                _engine = null;
                UnavailableReason = "OCR motoru başlatılamadı (" + ex.GetType().Name + ")";
            }
        }

        // Karma kipli DLL ilk burada yüklenir; hata oluşursa çağıran try bloğu yakalasın diye ayrı metot.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static TesseractProcessor CreateEngine(string dataPath)
        {
            var engine = new TesseractProcessor();
            if (!engine.Init(dataPath, Language, 0))
                return null;
            engine.SetVariable("tessedit_char_whitelist", Whitelist);
            return engine;
        }

        /// <summary>Kırpılmış plaka görüntüsünden metni okur; OCR kullanılamıyorsa null döner.</summary>
        [HandleProcessCorruptedStateExceptions]
        public PlateText Read(Bitmap plate)
        {
            if (!IsAvailable || plate == null)
                return null;

            try
            {
                string raw;
                using (Bitmap input = Prepare(plate))
                {
                    lock (_gate)
                    {
                        _engine.Clear();
                        _engine.ClearAdaptiveClassifier();
                        raw = _engine.Apply(input);
                    }
                }
                return Interpret(raw);
            }
            catch (AccessViolationException)
            {
                IsAvailable = false;
                UnavailableReason = "OCR motoru okuma sırasında hata verdi";
                return null;
            }
        }

        /// <summary>Plakayı OCR'ın sevdiği boyuta büyütür, çevresine beyaz boşluk ekler.</summary>
        private static Bitmap Prepare(Bitmap plate)
        {
            const int targetHeight = 120;
            const int margin = 14;
            int width = Math.Max(1, (int)Math.Round(plate.Width * (double)targetHeight / plate.Height));

            var result = new Bitmap(width + 2 * margin, targetHeight + 2 * margin, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.Clear(Color.White);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(plate, margin, margin, width, targetHeight);
            }
            return result;
        }

        internal static PlateText Interpret(string raw)
        {
            var cleaned = new StringBuilder();
            foreach (char c in (raw ?? string.Empty).ToUpperInvariant())
                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                    cleaned.Append(c);

            string clean = cleaned.ToString();
            Match best = null;
            foreach (Match m in TurkishPlate.Matches(clean))
                if (best == null || m.Length > best.Length)
                    best = m;

            if (best == null)
                return new PlateText(clean, clean, false);

            string formatted = best.Groups[1].Value + " " + best.Groups[2].Value + " " + best.Groups[3].Value;
            return new PlateText(formatted, clean, true);
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_engine == null) return;
                try { _engine.End(); }
                catch (Exception) { /* kapanışta motor hatası önemsiz */ }
                _engine = null;
                IsAvailable = false;
            }
        }
    }
}
