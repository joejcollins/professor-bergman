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

        public string Exhibit { get; set; }

        public string DeviceLabel { get; set; }

        public string Venue { get; set; }
    }
}
