/*
 * 
 */
using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// 
/// </summary>
namespace Dinmore.Domain
{
    /// <summary>
    /// 
    /// </summary>
    public class Device
    {
        public Guid Id { get; set; }


        public string DeviceLabel { get; set; }

        public string Exhibit { get; set; }

        public string Venue { get; set; }

        [Display(Name = "Is Listening?", 
            Description = "Is listening to the visitor speaking?")]
        public bool Interactive { get; set; }

        public bool VerbaliseSystemInformationOnBoot { get; set; }

        public bool SoundOn { get; set; }

        [Display(Name = "Reset on start up?", 
            Description = "Will the device get new settings from the server on start up?")]
        public bool ResetOnBoot { get; set; }

        public string VoicePackageUrl { get; set; }

        public string QnAKnowledgeBaseId { get; set; }

        public byte[] VoicePackage { get; set; }
    }
}
