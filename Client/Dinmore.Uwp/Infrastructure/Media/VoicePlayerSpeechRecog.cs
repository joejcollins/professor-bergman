using Dinmore.Uwp.Constants;
using Dinmore.Uwp.Helpers;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace Dinmore.Uwp.Infrastructure.Media
{
    class VoicePlayerSpeechRecog: IDisposable
    {
        private bool disposedValue = false; 
        private static MediaPlayer mediaPlayer = new MediaPlayer();
        private bool StopOnNextTrack;
        private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(0, 1);
        public bool IsCurrentlyPlaying { get; set; }

        public VoicePlayerSpeechRecog() {
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded; ;
        }

        private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            // Signal the SpeakAsync method
            semaphoreSlim.Release();
            this.IsCurrentlyPlaying = false;
            Debug.WriteLine("Audio playback complete");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    mediaPlayer.Dispose();
                }

                disposedValue = true;
            }
        }

        public async Task SayAsync(string phrase) {

            if (Settings.GetBool(DeviceSettingKeys.SoundOnKey))
            {
                if (mediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing)
                {
                    Debug.WriteLine("Audio playback started");
                    this.IsCurrentlyPlaying = true;
                    var synth = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();
                    SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync(phrase);
                    MediaSource mediaSource = MediaSource.CreateFromStream(stream, stream.ContentType);
                    mediaPlayer.Source = mediaSource;
                    mediaPlayer.Play();

                    // Wait until the MediaEnded event on MediaElement is raised,
                    // before turning on speech recognition again. The semaphore
                    // is signaled in the mediaElement_MediaEnded event handler.
                    await semaphoreSlim.WaitAsync();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

    }
}
