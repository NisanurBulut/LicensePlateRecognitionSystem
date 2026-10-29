using System.Collections.Generic;
using System.Drawing;

namespace PlakaTanima.Core
{
    /// <summary>İşlem hattında tamamlanan bir adım ve o adımın ürettiği görüntü.</summary>
    public sealed class PipelineStep
    {
        public PipelineStep(int index, int total, string title, string description, Bitmap image)
        {
            Index = index;
            Total = total;
            Title = title;
            Description = description;
            Image = image;
        }

        public int Index { get; }
        public int Total { get; }
        public string Title { get; }
        public string Description { get; }

        /// <summary>Alıcıya aittir; işlem hattı bu bitmap'i bir daha kullanmaz.</summary>
        public Bitmap Image { get; }
    }

    /// <summary>En/boy oranı plakaya uyan bir bölge.</summary>
    public sealed class PlateCandidate
    {
        public PlateCandidate(Rectangle bounds)
        {
            Bounds = bounds;
        }

        public Rectangle Bounds { get; }
        public int Area => Bounds.Width * Bounds.Height;
        public float AspectRatio => Bounds.Height == 0 ? 0 : (float)Bounds.Width / Bounds.Height;
    }

    public sealed class PlateRecognitionResult
    {
        public PlateRecognitionResult(Bitmap source, Bitmap annotated, Bitmap plateImage,
            IReadOnlyList<PlateCandidate> candidates, PlateCandidate best, int blobCount, long elapsedMilliseconds)
        {
            Source = source;
            Annotated = annotated;
            PlateImage = plateImage;
            Candidates = candidates;
            Best = best;
            BlobCount = blobCount;
            ElapsedMilliseconds = elapsedMilliseconds;
        }

        /// <summary>İşlemde kullanılan (pencereye sığacak şekilde küçültülmüş) görüntü.</summary>
        public Bitmap Source { get; }
        /// <summary>Plaka adaylarının çerçevelendiği görüntü.</summary>
        public Bitmap Annotated { get; }
        /// <summary>Seçilen plakanın kırpılmış hali; plaka bulunamadıysa null.</summary>
        public Bitmap PlateImage { get; }
        public IReadOnlyList<PlateCandidate> Candidates { get; }
        public PlateCandidate Best { get; }
        public int BlobCount { get; }
        public long ElapsedMilliseconds { get; }
        public bool Found => Best != null;
    }
}
