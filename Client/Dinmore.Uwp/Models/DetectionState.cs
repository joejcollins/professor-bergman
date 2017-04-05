using System;

namespace Dinmore.Uwp.Models
{
    public class DetectionState
    {
        public byte[] LastFrame { get; set; }

        /// <summary>
        /// Tracks the last time we asked the API anything so we don't get too chatty.
        /// </summary>
        public DateTimeOffset LastImageApiPush { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Holds the current state machine value for the detection of faces.
        /// </summary>
        public DetectionStates State { get; set; }
    }
}
