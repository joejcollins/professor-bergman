namespace Dinmore.Uwp.Models
{
    /// <summary>
    /// Values for identifying and controlling the current step of detection.
    /// </summary>
    public enum DetectionStates
    {
        /// <summary>
        /// Camera is off and app is either starting up or shutting down.
        /// </summary>
        Idle,

        /// <summary>
        /// Camera is starting up.
        /// </summary>
        Startup,

        /// <summary>
        /// Webcam is running and looking for faces.
        /// </summary>
        WaitingForFaces,

        /// <summary>
        /// Webcam has detected faces, now we want to wait for them to disappear
        /// </summary>
        WaitingForFacesToDisappear,

        /// <summary>
        /// At least one face has been detected by the IoT device.
        /// </summary>
        FaceDetectedOnDevice,

        ApiResponseReceived,

        /// <summary>
        /// Call to API has been made and now have some results to look at.
        /// </summary>
        InterpretingApiResults,
    }
}
