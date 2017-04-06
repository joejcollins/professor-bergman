namespace Dinmore.Uwp.Models
{
    public class Face
    {
        public string faceId { get; set; }
        public FaceRectangle faceRectangle { get; set; }
        public FaceLandmarks faceLandmarks { get; set; }
        public FaceAttributes faceAttributes { get; set; }
    }
}
