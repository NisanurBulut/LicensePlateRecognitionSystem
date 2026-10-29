using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using PlakaTanima.Wpf.ViewModels;

namespace PlakaTanima.Wpf
{
    public partial class MainWindow : Window
    {
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

        public static string Sample1 => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Samples", "1.jpg");
        public static string Sample2 => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Samples", "8.jpg");

        private readonly MainViewModel _viewModel = new MainViewModel();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;

            _viewModel.OpenRequested += (s, e) => PickImage();
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.Steps.CollectionChanged += OnStepsChanged;

            Closed += (s, e) => _viewModel.Shutdown();

            // "Birlikte aç" / komut satırı: PlakaTanima.exe foto.jpg
            Loaded += (s, e) =>
            {
                string[] args = Environment.GetCommandLineArgs();
                if (args.Length > 1 && File.Exists(args[1]))
                    _viewModel.LoadImage(args[1]);
            };
        }

        private void PickImage()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Araç fotoğrafı seç",
                Filter = "Görseller|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Tüm dosyalar|*.*"
            };
            if (dialog.ShowDialog(this) == true)
                _viewModel.LoadImage(dialog.FileName);
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            e.Effects = FirstImagePath(e) != null ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
            base.OnDragOver(e);
        }

        protected override void OnDrop(DragEventArgs e)
        {
            string path = FirstImagePath(e);
            if (path != null)
                _viewModel.LoadImage(path);
            e.Handled = true;
            base.OnDrop(e);
        }

        private static string FirstImagePath(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return null;
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            return files?.FirstOrDefault(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
        }

        // Yeni adım kartı gelince listeyi sağa kaydır.
        private void OnStepsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && StepList.Items.Count > 0)
                StepList.ScrollIntoView(StepList.Items[StepList.Items.Count - 1]);
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsBusy))
                SetScanning(_viewModel.IsBusy);
        }

        private void SetScanning(bool on)
        {
            if (!on)
            {
                ScanTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, null);
                ScanLine.Visibility = Visibility.Collapsed;
                return;
            }

            ScanLine.Visibility = Visibility.Visible;
            var sweep = new DoubleAnimation(0, Math.Max(ImageHost.ActualHeight - 3, 1), TimeSpan.FromSeconds(1.1))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            ScanTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, sweep);
        }

    }

    /// <summary>Değer null ise gizler.</summary>
    public sealed class NullToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value == null ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Sayı 0 ise görünür yapar (boş liste ipucu için).</summary>
    public sealed class ZeroToVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is int count && count == 0 ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
