using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dinmore.WebApp.Models
{
    public class AppSettings
    {
        public string ApiRoot { get; set; }
        public string StorePatronContainerName { get; set; }
        public string StoreDeviceContainerName { get; set; }
        public string StoreDevicePartitionKey { get; set; }
    }
}
