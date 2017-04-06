using System.Collections.Generic;
using System.Linq;
using Windows.Graphics.Imaging;
using Windows.Media.FaceAnalysis;

namespace Dinmore.Uwp
{
    public class BoundingBoxCreator
    {
        public BitmapBounds BoundingBoxForFaces(IList<DetectedFace> faces, int pixelWidth, int pixelHeight)
        {
            var bounds = new BitmapBounds
            {
                X = faces.Min(x => x.FaceBox.X),
                Y = faces.Min(y => y.FaceBox.Y),
            };
            bounds.Height = faces.Max(y => y.FaceBox.Y + y.FaceBox.Height) - bounds.Y;
            bounds.Width = faces.Max(x => x.FaceBox.X + x.FaceBox.Width) - bounds.X;

            var expanded = new BitmapBounds();
            expanded.X = bounds.X - (bounds.Width / 2);
            expanded.Y = bounds.Y - (bounds.Height / 2);
            expanded.Width = bounds.Width * 2;
            expanded.Height = bounds.Height * 2;

            if (expanded.X < 0)
            {
                expanded.X = 0;
            }
            if (expanded.Y < 0)
            {
                expanded.Y = 0;
            }
            if (expanded.X + expanded.Width > pixelWidth)
            {
                expanded.Width = (uint)pixelWidth - expanded.X;
            }
            if (expanded.Y + expanded.Height > pixelHeight)
            {
                expanded.Height = (uint)pixelHeight - expanded.Y;
            }

            return expanded;
        }
    }
}
