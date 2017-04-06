using Windows.Graphics.Imaging;

namespace Dinmore.Uwp.Models
{
    /// <summary>
    /// Used for remembering what we sent to the API so that we can reconstruct co-ordinates and
    /// plot rectangles on screen.
    /// </summary>
    public class ApiRequestParameters
    {
        public byte[] Image { get; internal set; }

        public BitmapBounds ImageBounds { get; internal set; }

        public int OriginalImageHeight { get; internal set; }

        public int OriginalImageWidth { get; internal set; }
    }
}
