using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dinmore.api.Models
{
    public class EmotionScores
    {
        public double anger { get; set; }

        public double contempt { get; set; }

        public double disgust { get; set; }

        public double fear { get; set; }

        public double happiness { get; set; }

        public double neutral { get; set; }

        public double sadness { get; set; }

        public double surprise { get; set; }
    }
}
