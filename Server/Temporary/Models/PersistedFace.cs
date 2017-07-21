using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dinmore.api.Models
{
    public class PersistedFace
    {
        public string persistedFaceId { get; set; }
        public double confidence { get; set; }
    }
}
