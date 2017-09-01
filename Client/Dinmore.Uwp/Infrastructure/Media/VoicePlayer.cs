using System;
using System.Linq;
using Dinmore.Uwp.Models;
using Windows.Media.Playback;
using Windows.Media.Core;
using System.Collections.Generic;
using Windows.Data.Json;
using Windows.Storage;

namespace Dinmore.Uwp.Infrastructure.Media
{

    internal class VoicePlayer : IDisposable, IVoicePlayer
    {

        private static MediaPlayer mediaPlayer = new MediaPlayer();
        private MediaPlaybackList playbackList = new MediaPlaybackList();
        public bool IsCurrentlyPlaying { get; set; }

        public void Stop() {
            StopOnNextTrack = true;
            IsCurrentlyPlaying = false;
        }

        public void PlayIntroduction(int numberOfPeople) {

            var playListGroup = PlayListGroup.HelloSingleFace;
            if (numberOfPeople > 1)
                playListGroup = PlayListGroup.HelloMultipleFace;

            var jsonArray = this.ja.GetNamedArray(playListGroup.ToString("F"));

            var playlist = new List<PlayListItem>();
            
            foreach (var item in jsonArray)
            {
                playlist.Add(new PlayListItem(playListGroup, 1, item.GetString()));

            }

            PlayWav(playlist);

        }

        public void Play(DetectionState currentState)
        {
            var avgAge = currentState.FacesFoundByApi.OrderByDescending(x => x.faceAttributes.age).First().faceAttributes.age;
            PlayListGroup playListGroup = GetPlayListGroupByDemographic(avgAge);

            var jsonArray = this.ja.GetNamedArray(playListGroup.ToString("F"));

            var playlist = new List<PlayListItem>();
            foreach (var item in jsonArray)
            {
                playlist.Add(new PlayListItem(playListGroup, 1, item.GetString()));

            }

            PlayWav(playlist);   
           
        }

        public void Say(string phrase) {
            throw new NotImplementedException("Not sure how to do this yet. We can't generate voices with Wavs after all.");
        }

        private PlayListGroup GetPlayListGroupByDemographic(double avgAge)
        {
            if (avgAge < 17) { return PlayListGroup.Demographic12to17; }
            if (avgAge < 24) { return PlayListGroup.Demographic18to24; }
            if (avgAge < 34) { return PlayListGroup.Demographic25to34; }
            if (avgAge < 44) { return PlayListGroup.Demographic35to44; }
            if (avgAge < 150) { return PlayListGroup.Demographic55to64; }

            return PlayListGroup.Demographic12to17;
        }

        public async void PlayWav(List<PlayListItem> list)
        {
            mediaPlayer.PlaybackSession.PositionChanged += PositionChanged;
               var session = mediaPlayer.PlaybackSession;
            if (session.PlaybackState == MediaPlaybackState.None)
            {
                session.Position = TimeSpan.Zero;
            }
            playbackList.Items.Clear();
            foreach (var item in list)
            {
                var file = await this.folder.GetFileAsync(item.Name);
                var mediaSource = MediaSource.CreateFromStorageFile(file);
                playbackList.Items.Add(new MediaPlaybackItem(mediaSource));
            }

            // Check if playlist changes track and stop if the viewer has exited
            playbackList.CurrentItemChanged += PlaybackList_CurrentItemChanged;
            mediaPlayer.Source = playbackList;
            mediaPlayer.Play();
            
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

        private bool disposedValue = false; // To detect redundant calls
        private bool StopOnNextTrack;
        private JsonObject ja;
        private StorageFolder folder;

        public VoicePlayer(JsonObject ja, StorageFolder folder)
        {
            this.ja = ja;
            this.folder = folder;
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

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }

    }
}
