using System;

namespace Dinmore.Uwp.Models
{
    public class StatusMessage
    {
        public StatusMessage(string what, StatusSeverity severity)
        {
            this.Severity = severity;
            this.What = what;
            this.When = DateTimeOffset.UtcNow;
        }

        public StatusSeverity Severity { get; set; }
        public string What { get; set; }
        public DateTimeOffset When { get; set; }
    }
}
