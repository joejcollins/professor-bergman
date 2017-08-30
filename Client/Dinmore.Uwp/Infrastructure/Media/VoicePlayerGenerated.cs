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

namespace Dinmore.Uwp.Infrastructure.Media
{
    class VoicePlayerGenerated : IDisposable, IVoicePlayer
    {
        private bool disposedValue = false; // To detect redundant calls
        private static MediaPlayer mediaPlayer = new MediaPlayer();
        private MediaPlaybackList playbackList = new MediaPlaybackList();
        private bool StopOnNextTrack;

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

        public async void PlayIntroduction(PlayListGroup playlistGroup)
        {
            var demographic = "intro";
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
        ;
            this.Say(new List<string>() { phrase });
        }
        private async void Say(List<string> list)
        {
            var synth = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();           
            SpeechSynthesisStream stream;
            mediaPlayer.PlaybackSession.PositionChanged += PositionChanged;
            var session = mediaPlayer.PlaybackSession;
            if (session.PlaybackState == MediaPlaybackState.None)
            {
                session.Position = TimeSpan.Zero;
            }
            playbackList.Items.Clear();
            foreach (var item in list)
            {
                stream = await synth.SynthesizeTextToStreamAsync(item);
                var mediaSource = MediaSource.CreateFromStream(stream, stream.ContentType);
                playbackList.Items.Add(new MediaPlaybackItem(mediaSource));
            }
            // Check if playlist changes track and stop if the viewer has exited
            playbackList.CurrentItemChanged += PlaybackList_CurrentItemChanged;
            mediaPlayer.Source = playbackList;
            IsCurrentlyPlaying = true;
            mediaPlayer.Play();
        }

        public void Stop()
        {
            StopOnNextTrack = true;
            IsCurrentlyPlaying = false;
        }

        private void PositionChanged(MediaPlaybackSession sender, object args)
        {

            if (sender.Position >= sender.NaturalDuration && playbackList.CurrentItemIndex == playbackList.Items.Count - 1)
            {
                IsCurrentlyPlaying = false;
            }

        }

        private void PlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {

            if (StopOnNextTrack)
            {
                mediaPlayer.Pause();
                IsCurrentlyPlaying = false;
                StopOnNextTrack = false;
            }
        }
        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }

    }
}
