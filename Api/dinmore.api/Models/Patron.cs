using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dinmore.api.Models
{
    public class Patron
    {
        public string FaceId { get; set; }

        public FaceRectangle FaceRectangle { get; set; }

        public int Age { get; set; }

        public string Gender { get; set; }

        public Scores EmotionScores { get; set; }

        public string PrimaryEmotion { get; set; }
    }
}
