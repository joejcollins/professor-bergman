using System;

namespace Dinmore.Uwp.Models
{
    public class StatusMessage
    {
        public StatusMessage(string what)
        {
            this.What = what;
            this.When = DateTimeOffset.UtcNow;
        }

        public string What { get; set; }
        public DateTimeOffset When { get; set; }
    }
}
