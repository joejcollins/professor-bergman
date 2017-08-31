using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dinmore.api.Models
{
    public class PatronStorageTableEntity : TableEntity
    {
        public PatronStorageTableEntity(string persistedFaceId, string sightingId)
        {
            this.PartitionKey = persistedFaceId;
            this.RowKey = sightingId;
        }

        public PatronStorageTableEntity() { }

        public string Exhibit { get; set; }

        public string Device { get; set; }

        public DateTime TimeOfSighting { get; set; }

        public string Gender { get; set; }

        public double Age { get; set; }

        public string PrimaryEmotion { get; set; }

        public double Smile { get; set; }

        public string Glasses { get; set; }

        public double FaceMatchConfidence { get; set; }
    }
}
