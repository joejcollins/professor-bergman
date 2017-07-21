using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dinmore.api.Models
{
    public class Face
    {
        public string faceId { get; set; }
        public FaceRectangle faceRectangle { get; set; }
        public FaceLandmarks faceLandmarks { get; set; }
        public FaceAttributes faceAttributes { get; set; }
    }
}
