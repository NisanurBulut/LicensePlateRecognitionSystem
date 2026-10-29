using System.Windows.Media.Imaging;
using PlakaTanima.Core;

namespace PlakaTanima.Wpf.ViewModels
{
    public sealed class StepCardViewModel
    {
        public StepCardViewModel(PipelineStep step)
        {
            Index = step.Index;
            Title = step.Title;
            Description = step.Description;
            Image = step.Image.ToBitmapSource();
        }

        public int Index { get; }
        public string Title { get; }
        public string Description { get; }
        public BitmapSource Image { get; }
    }
}
