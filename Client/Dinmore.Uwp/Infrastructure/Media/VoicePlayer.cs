using System;
using System.Linq;
using Dinmore.Uwp.Models;
using Windows.Media.Playback;
using Windows.Media.Core;
using System.Collections.Generic;

namespace Dinmore.Uwp.Infrastructure.Media
{

    internal class VoicePlayer : IDisposable
    {
        // see https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/play-audio-and-video-with-mediaplayer


        private static MediaPlayer mediaPlayer = new MediaPlayer();
        private static PlayListItem currentPlayListItem;
        private MediaPlaybackList playbackList = new MediaPlaybackList();
        public bool IsCurrentlyPlaying;

        internal void Stop() {
            StopOnNextTrack = true;
            IsCurrentlyPlaying = false;
        }

        internal void PlayIntroduction(PlayListGroup playlistGroup) {
            PlayWav(PlayList.List
                        .Where(w => w.PlayListGroup == playlistGroup).ToList()
                    );
        }

        internal void Play(DetectionState currentState)
        {
           // TODO: Get Average Age

                PlayWav(PlayList.List
                        .Where(w => w.PlayListGroup == PlayListGroup.Demographic12to17).ToList()
                    );   
           
        }

        internal void PlayWav(List<PlayListItem> list)
        {
            mediaPlayer.PlaybackSession.PositionChanged += PositionChanged;
               var session = mediaPlayer.PlaybackSession;
            if (session.PlaybackState == MediaPlaybackState.None)           

                //set back to zero
                session.Position = TimeSpan.Zero;
            // mediaPlayer.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///{item.Name}"));

            // mediaPlayer.Play();

            playbackList.Items.Clear();
            foreach (var item in list)
            {
                var mediaSource = MediaSource.CreateFromUri(new Uri($"ms-appx:///{item.Name}"));
                playbackList.Items.Add(new MediaPlaybackItem(mediaSource));
            }

            // Check if playlist changes track and stop if the viewer has exited
            playbackList.CurrentItemChanged += PlaybackList_CurrentItemChanged;
            mediaPlayer.Source = playbackList;
            IsCurrentlyPlaying = true;
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
