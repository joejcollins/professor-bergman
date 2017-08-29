using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dinmore.Api.Models
{
    public class DeviceTE : TableEntity
    {
        public DeviceTE(string deviceId, string venue)
        {
            this.PartitionKey = venue;
            this.RowKey = deviceId;
        }

        public string Exhibit { get; set; }

        public string Device { get; set; }

        public string Venue { get; set; }
    }
}
