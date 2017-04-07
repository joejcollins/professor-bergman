using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dinmore.api.Models
{
    public class PatronSighting : TableEntity
    {
        public PatronSighting(string persistedFaceId, string sightingId)
        {
            this.PartitionKey = persistedFaceId;
            this.RowKey = sightingId;
        }

        public PatronSighting() { }

        public string Exhibit { get; set; }

        public string Device { get; set; }

        public DateTime TimeOfSighting { get; set; }

        public string Gender { get; set; }

        public double Age { get; set; }

        public string PrimaryEmotion { get; set; }
    }
}
