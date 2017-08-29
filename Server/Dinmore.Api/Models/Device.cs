using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dinmore.Api.Models
{
    public class Device
    {
        public Guid Id { get; set; }

        public string Label { get; set; }

        public string Exhibit { get; set; }

        public string Venue { get; set; }
    }
}
