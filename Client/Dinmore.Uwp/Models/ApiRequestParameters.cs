using System.Collections.Generic;
using Windows.Media.FaceAnalysis;

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
        public IList<DetectedFace> Faces { get; internal set; }
    }
}
