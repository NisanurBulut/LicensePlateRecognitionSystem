using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PlakaTanima.Core;

namespace PlakaTanima.Wpf.ViewModels
{
    public enum DetectionState
    {
        Idle,
        Running,
        Found,
        NotFound,
        Failed
    }

    public sealed class MainViewModel : ObservableObject
    {
        private Bitmap _source;
        private BitmapSource _originalImage;
        private BitmapSource _annotatedImage;
        private BitmapSource _displayImage;
        private BitmapSource _plateImage;
        private StepCardViewModel _selectedStep;
        private DetectionState _state = DetectionState.Idle;
        private string _viewLabel = "";
        private string _fileName = "";
        private string _progressText = "";
        private double _progress;
        private string _message = "";
        private string _location = "—";
        private string _size = "—";
        private string _ratio = "—";
        private string _duration = "—";
        private string _regions = "—";
        private string _plateText = "";
        private bool _plateTextValid;
        private string _ocrNote = "";
        private PlateTextReader _reader;

        public MainViewModel()
        {
            OpenCommand = new RelayCommand(_ => OpenRequested?.Invoke(this, EventArgs.Empty), _ => !IsBusy);
            DetectCommand = new RelayCommand(async _ => await DetectAsync(), _ => HasImage && !IsBusy);
            SampleCommand = new RelayCommand(p => LoadImage((string)p), p => !IsBusy && p is string path && File.Exists(path));
            ShowResultCommand = new RelayCommand(_ => SelectedStep = null, _ => SelectedStep != null);
        }

        /// <summary>Görünümden dosya seçtirmek için.</summary>
        public event EventHandler OpenRequested;

        public ObservableCollection<StepCardViewModel> Steps { get; } = new ObservableCollection<StepCardViewModel>();

        public ICommand OpenCommand { get; }
        public ICommand DetectCommand { get; }
        public ICommand SampleCommand { get; }
        public ICommand ShowResultCommand { get; }

        public bool HasImage => _source != null;
        public bool IsBusy => State == DetectionState.Running;

        public DetectionState State
        {
            get => _state;
            private set
            {
                if (!Set(ref _state, value)) return;
                Raise(nameof(IsBusy));
                Raise(nameof(HasPlate));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool HasPlate => State == DetectionState.Found;

        public BitmapSource DisplayImage { get => _displayImage; private set => Set(ref _displayImage, value); }
        public BitmapSource PlateImage { get => _plateImage; private set => Set(ref _plateImage, value); }
        public string ViewLabel { get => _viewLabel; private set => Set(ref _viewLabel, value); }
        public string FileName { get => _fileName; private set => Set(ref _fileName, value); }
        public string ProgressText { get => _progressText; private set => Set(ref _progressText, value); }
        public double Progress { get => _progress; private set => Set(ref _progress, value); }
        public string Message { get => _message; private set => Set(ref _message, value); }
        public string Location { get => _location; private set => Set(ref _location, value); }
        public string Size { get => _size; private set => Set(ref _size, value); }
        public string Ratio { get => _ratio; private set => Set(ref _ratio, value); }
        public string Duration { get => _duration; private set => Set(ref _duration, value); }
        public string Regions { get => _regions; private set => Set(ref _regions, value); }

        /// <summary>Okunan plaka metni; okunamadıysa boş.</summary>
        public string RecognizedText { get => _plateText; private set => Set(ref _plateText, value); }
        /// <summary>Metin Türkiye plaka biçimine uyuyor mu.</summary>
        public bool PlateTextValid { get => _plateTextValid; private set => Set(ref _plateTextValid, value); }
        /// <summary>OCR kullanılamıyorsa ya da okuyamadıysa nedeni.</summary>
        public string OcrNote { get => _ocrNote; private set => Set(ref _ocrNote, value); }

        public StepCardViewModel SelectedStep
        {
            get => _selectedStep;
            set
            {
                if (!Set(ref _selectedStep, value)) return;
                if (value != null)
                {
                    DisplayImage = value.Image;
                    ViewLabel = value.Index + ". " + value.Title;
                }
                else
                {
                    ShowResultView();
                }
            }
        }

        /// <summary>Pencere kapanırken OCR motorunu serbest bırakır.</summary>
        public void Shutdown()
        {
            _reader?.Dispose();
            _reader = null;
        }

        public void LoadImage(string path)
        {
            if (IsBusy) return;

            try
            {
                // Dosyayı kilitlememek için kopyasını tut.
                using (var loaded = new Bitmap(path))
                    _source = new Bitmap(loaded);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is IOException || ex is OutOfMemoryException)
            {
                State = DetectionState.Failed;
                Message = "Bu dosya bir görsel olarak açılamadı.";
                return;
            }

            _originalImage = _source.ToBitmapSource();
            _annotatedImage = null;
            FileName = Path.GetFileName(path);
            Steps.Clear();
            SelectedStep = null;
            PlateImage = null;
            ResetStats();
            Progress = 0;
            ProgressText = "";
            State = DetectionState.Idle;
            Message = "Görsel hazır. “Plakayı Tespit Et” ile başlayın.";
            DisplayImage = _originalImage;
            ViewLabel = "Orijinal";
            Raise(nameof(HasImage));
        }

        private async Task DetectAsync()
        {
            Steps.Clear();
            SelectedStep = null;
            PlateImage = null;
            _annotatedImage = null;
            ResetStats();
            Progress = 0;
            ProgressText = "Başlıyor…";
            Message = "Görüntü işleniyor…";
            State = DetectionState.Running;

            // Bu thread'de oluşturulduğu için geri çağrılar arayüz thread'inde çalışır.
            var progress = new Progress<PipelineStep>(OnStep);
            var work = new Bitmap(_source);

            try
            {
                PlateText reading = null;
                PlateRecognitionResult result = await Task.Run(() =>
                {
                    PlateRecognitionResult found = new PlateRecognizer().Recognize(work, progress);
                    if (found.PlateImage != null)
                    {
                        if (_reader == null)
                            _reader = new PlateTextReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata"));
                        reading = _reader.Read(found.PlateImage);
                    }
                    return found;
                });
                ShowResult(result, reading);
            }
            catch (Exception ex)
            {
                State = DetectionState.Failed;
                Message = "İşlem sırasında hata oluştu: " + ex.Message;
                ProgressText = "";
            }
            finally
            {
                work.Dispose();
            }
        }

        private void OnStep(PipelineStep step)
        {
            var card = new StepCardViewModel(step);
            step.Image.Dispose();
            Steps.Add(card);

            Progress = 100.0 * step.Index / step.Total;
            ProgressText = "Adım " + step.Index + "/" + step.Total + " · " + step.Title;

            // Kullanıcı bir adım seçmediyse ana görüntü işlem hattını canlı izlesin.
            if (SelectedStep == null)
            {
                DisplayImage = card.Image;
                ViewLabel = card.Index + ". " + card.Title;
            }
        }

        private void ShowResult(PlateRecognitionResult result, PlateText reading)
        {
            _annotatedImage = result.Annotated.ToBitmapSource();
            Progress = 100;
            Duration = result.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) + " ms";
            Regions = result.BlobCount.ToString(CultureInfo.InvariantCulture);

            if (result.Found)
            {
                var b = result.Best.Bounds;
                PlateImage = result.PlateImage?.ToBitmapSource();
                Location = "x " + b.X + ", y " + b.Y;
                Size = b.Width + " × " + b.Height + " px";
                Ratio = result.Best.AspectRatio.ToString("0.00", CultureInfo.InvariantCulture) + " : 1";
                Message = result.Candidates.Count > 1
                    ? result.Candidates.Count + " aday arasından en büyüğü seçildi."
                    : "Plaka bölgesi tespit edildi.";
                State = DetectionState.Found;

                if (reading != null && reading.Text.Length > 0)
                {
                    RecognizedText = reading.Text;
                    PlateTextValid = reading.IsValidFormat;
                    OcrNote = reading.IsValidFormat ? "" : "Okunan metin plaka biçimine uymuyor, kontrol edin.";
                }
                else
                {
                    OcrNote = _reader != null && !_reader.IsAvailable
                        ? "Karakter okuma kullanılamıyor: " + _reader.UnavailableReason
                        : "Plaka bulundu ama karakterler okunamadı.";
                }
            }
            else
            {
                Message = "Plakaya benzeyen bir bölge bulunamadı. Plakası net görünen bir fotoğraf deneyin.";
                State = DetectionState.NotFound;
            }

            ProgressText = "Tamamlandı";
            ShowResultView();

            result.Source.Dispose();
            result.Annotated.Dispose();
            result.PlateImage?.Dispose();
        }

        private void ShowResultView()
        {
            if (_annotatedImage != null)
            {
                DisplayImage = _annotatedImage;
                ViewLabel = "Sonuç";
            }
            else if (_originalImage != null)
            {
                DisplayImage = _originalImage;
                ViewLabel = "Orijinal";
            }
        }

        private void ResetStats()
        {
            Location = Size = Ratio = Duration = Regions = "—";
            RecognizedText = "";
            PlateTextValid = false;
            OcrNote = "";
        }
    }
}
