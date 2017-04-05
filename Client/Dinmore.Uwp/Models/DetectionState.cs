namespace Dinmore.Uwp.Models
{
    public class DetectionState
    {
        public byte[] LastFrame { get; set; }

        /// <summary>
        /// Holds the current state machine value for the detection of faces.
        /// </summary>
        public DetectionStates State { get; set; }
    }
}
