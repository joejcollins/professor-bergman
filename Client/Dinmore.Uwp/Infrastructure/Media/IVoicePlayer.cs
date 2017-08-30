using System.Collections.Generic;
using Dinmore.Uwp.Models;

namespace Dinmore.Uwp.Infrastructure.Media
{
    internal interface IVoicePlayer
    {
        void Dispose();
        bool IsCurrentlyPlaying { get; set; }
        void Play(DetectionState currentState);
        void PlayIntroduction(int numberOfPeople);
        void Stop();
        
    }
}