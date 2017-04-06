using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dinmore.api.Models
{
    public class Patron
    {
        public string FaceId { get; set; }

        public string PersistedFaceId { get; set; }

        public FaceRectangle FaceRectangle { get; set; }

        public FaceLandmarks FaceLandmarks { get; set; }

        public FaceAttributes FaceAttributes { get; set; }

        public string PrimaryEmotion { get; set; }

        public Nullable<DateTime> TimeLastSeen { get; set; }

        public string DeviceLastSeen { get; set; }
    }
}
