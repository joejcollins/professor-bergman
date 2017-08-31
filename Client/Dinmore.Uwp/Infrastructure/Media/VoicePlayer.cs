using System;
using System.Linq;
using Dinmore.Uwp.Models;
using Windows.Media.Playback;
using Windows.Media.Core;
using System.Collections.Generic;

namespace Dinmore.Uwp.Infrastructure.Media
{

    internal class VoicePlayer : IDisposable, IVoicePlayer
    {
        // see https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/play-audio-and-video-with-mediaplayer


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

            PlayWav(PlayList.List
                        .Where(w => w.PlayListGroup == playListGroup).ToList()
                    );
        }

        public void Play(DetectionState currentState)
        {
            var avgAge = currentState.FacesFoundByApi.OrderByDescending(x => x.faceAttributes.age).First().faceAttributes.age;
            PlayListGroup playListGroup = GetPlayListGroupByDemographic(avgAge);

                PlayWav(PlayList.List
                        .Where(w => w.PlayListGroup == playListGroup).ToList()
                    );   
           
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

        public void PlayWav(List<PlayListItem> list)
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
                var mediaSource = MediaSource.CreateFromUri(new Uri($"ms-appx:///{item.Name}"));
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

        private void Session_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            if (sender.PlaybackState == MediaPlaybackState.Paused)
            {
                //var session = mediaPlayer.PlaybackSession;
                //mediaPlayer.CurrentState = MediaPlaybackState.None;
                //TODO at this point we should be at the end
                //session.PlaybackStateChanged += Session_PlaybackStateChanged;
                //var session = mediaPlayer.PlaybackSession;
                //session.Position = TimeSpan.Zero;

                //mediaPlayer.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/Voice/goat.wav"));

                //mediaPlayer.Play();
            }
        }

        private bool disposedValue = false; // To detect redundant calls
        private bool StopOnNextTrack;

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
