using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dinmore.Api.Models
{
    public class DeviceStorageTableEntity : TableEntity
    {
        public DeviceStorageTableEntity(string deviceId, string partitionKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = deviceId;
        }

        public DeviceStorageTableEntity()
        {
        }

        public string DeviceLabel { get; set; }

        public string Exhibit { get; set; }

        public string Venue { get; set; }

        public bool Interactive { get; set; }

        public bool VerbaliseSystemInformationOnBoot { get; set; }

        public bool SoundOn { get; set; }

        public bool ResetOnBoot { get; set; }

        public string VoicePackageUrl { get; set; }

        public string QnAKnowledgeBaseId { get; set; }
    }
}
