using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dinmore.Uwp.Models;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;
using Windows.Media.Core;
using System.IO;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.Storage.Streams;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Dinmore.Uwp.Helpers;
using Dinmore.Uwp.Constants;

namespace Dinmore.Uwp.Infrastructure.Media
{
    class VoicePlayerGenerated : IDisposable, IVoicePlayer
    {
        private bool disposedValue = false; 
        private static MediaPlayer mediaPlayer = new MediaPlayer();
        private MediaPlaybackList playbackList = new MediaPlaybackList();
        private bool StopOnNextTrack;

        public VoicePlayerGenerated() {
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded; ;
        }

        private async void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            IsCurrentlyPlaying = false;
            await PlayNext();
        }

        private Queue<string> speechlist = new Queue<string>();

        public bool IsCurrentlyPlaying { get; set; }

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

        public async void Play(DetectionState currentState)
        {
            var avgAge = currentState.FacesFoundByApi.OrderByDescending(x => x.faceAttributes.age).First().faceAttributes.age;

            var demographic = GetDemographicFromAge(avgAge);
            StorageFile file = await GetScriptFromDemographic(demographic);
            var thingstosay = await FileIO.ReadLinesAsync(file);
            Say(thingstosay.ToList());


        }

        private string GetDemographicFromAge(double avgAge)
        {
            if (avgAge < 17) { return "12-17"; }
            if (avgAge < 24) { return "12-17"; }
            if (avgAge < 34) { return "12-17"; }
            if (avgAge < 44) { return "12-17"; }
            if (avgAge < 150) { return "12-17"; }

            return "12-17";
        }

        public async void PlayIntroduction(int numberOfPeople)
        {
            var demographic = "intro";
            if (numberOfPeople > 1)
                demographic = "introGroup";

            StorageFile file = await GetScriptFromDemographic(demographic);
            var thingstosay = await FileIO.ReadLinesAsync(file);
            Say(thingstosay.ToList());
        }

        private static async Task<StorageFile> GetScriptFromDemographic(string demographic)
        {
            StorageFolder appInstalledFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var file = await appInstalledFolder.GetFileAsync($"Assets\\Voice\\{demographic}.txt");
            return file;
        }

        public void Say(string phrase) {  
            
            this.Say(new List<string>() { phrase });
        }

        private async void Say(List<string> list)
        {
            foreach (var item in list)
            {
                speechlist.Enqueue(item);
            }
            await PlayNext();
        }

        public async Task<bool> PlayNext()
        {
            // Respect sound on setting
            if (Settings.GetBool(DeviceSettingKeys.SoundOnKey))
            {
                if (!this.IsCurrentlyPlaying && mediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing && speechlist.Count > 0)
                {
                    this.IsCurrentlyPlaying = true;
                    var item = speechlist.Dequeue();
                    var synth = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();
                    SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync(item);
                    MediaSource mediaSource = MediaSource.CreateFromStream(stream, stream.ContentType);
                    mediaPlayer.Source = mediaSource;
                    mediaPlayer.Play();
                    return true;
                }
            }

            return false;
        }

        public void Stop()
        {
            speechlist.Clear();
        }

        public void Dispose()
        {
            Dispose(true);
        }

    }
}
