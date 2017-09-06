using System;

namespace Dinmore.Domain
{
    public class Device
    {
        public Guid Id { get; set; }

        public string DeviceLabel { get; set; }

        public string Exhibit { get; set; }

        public string Venue { get; set; }

        public bool Interactive { get; set; }

        public bool VerbaliseSystemInformationOnBoot { get; set; }

        public bool SoundOn { get; set; }

        public bool ResetOnBoot { get; set; }

        public string VoicePackageUrl { get; set; }

        public string QnAKnowledgeBaseId { get; set; }

        public byte[] VoicePackage { get; set; }
    }
}
