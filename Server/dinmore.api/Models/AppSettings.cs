using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dinmore.api.Models
{
    public class AppSettings
    {
        public string EmotionApiBaseUrl { get; set; }
        public string EmotionApiKey { get; set; }
        public string FaceApiDetectBaseUrl { get; set; }
        public string FaceApiKey { get; set; }

        public string FaceApiFaceListsPersistedFacesBaseUrl { get; set; }
        public string FaceApiFindSimilarBaseUrl { get; set; }
        public string FaceApiCreateFaceListBaseUrl { get; set; }

        public string TableStorageConnectionString { get; set; }
    }
}
