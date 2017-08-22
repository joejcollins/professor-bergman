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

        public Nullable<DateTime> Time { get; set; }

        public string Device { get; set; }

        public string Exhibit { get; set; }

        public string CurrentFaceListId { get; set; }

        public bool IsInList { get; set; }

        public Nullable<double> FaceMatchConfidence { get; set; }
    }
}
