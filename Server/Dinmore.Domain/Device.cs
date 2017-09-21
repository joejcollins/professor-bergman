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
        [Display(Name = "ID",
            Description = "Unique identifier for the device, this can be changed by presenting a new QR code")]
        public Guid Id { get; set; }

        [Display(Name = "Device Label",
           Description = "Where on the exhibit is device mounted?")]
        public string DeviceLabel { get; set; }

        [Display(Name = "Exhibit",
            Description = "Which exhibit is the device mounted on?")]
        public string Exhibit { get; set; }

        [Display(Name = "Venue",
            Description = "The name of the museum, gallery or location of the exhibit")]
        public string Venue { get; set; }

        [Display(Name = "Is Listening?", 
            Description = "Is listening to the visitor speaking?")]
        public bool Interactive { get; set; }

        [Display(Name = "Speak System Info on Start?",
            Description = "Verbalise the system information on start up?")]
        public bool VerbaliseSystemInformationOnBoot { get; set; }

        [Display(Name = "Is Sound On?",
            Description = "Is the sound on so the exhibit can speak to the visitor?")]
        public bool SoundOn { get; set; }

        [Display(Name = "Reset on Start?", 
            Description = "Will the device get new settings from the server on start up?")]
        public bool ResetOnBoot { get; set; }

        [Display(Name = "Voice Package Url",
            Description = "Web address of the zip file containing the voices")]
        public string VoicePackageUrl { get; set; }

        public string QnAKnowledgeBaseId { get; set; }

        public byte[] VoicePackage { get; set; }
    }
}
