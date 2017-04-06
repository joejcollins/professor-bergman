using Windows.Graphics.Imaging;

namespace Dinmore.Uwp.Models
{
    /// <summary>
    /// Used for remembering what we sent to the API so that we can reconstruct co-ordinates and
    /// plot rectangles on screen.
    /// </summary>
    public class ApiRequestParameters
    {
        /// <summary>
        /// The extracted section of the original camera image that has the faces in it. We send this to the API.
        /// </summary>
        public byte[] Image { get; internal set; }

        /// <summary>
        /// Where on the original camera image we extracted the merged face rectangle from.
        /// </summary>
        public BitmapBounds ImageBounds { get; internal set; }

        /// <summary>
        /// The original camera image height, used to translate the API reported position to the image position.
        /// </summary>
        public int OriginalImageHeight { get; internal set; }

        /// <summary>
        /// The original camera image width, used to translate the API reported position to the image position.
        /// </summary>
        public int OriginalImageWidth { get; internal set; }
    }
}
