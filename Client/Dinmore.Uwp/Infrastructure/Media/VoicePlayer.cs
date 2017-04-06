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

        internal void Play(DetectionState currentState)
        {
            if (currentState.FacesFoundByApi.Count == 1)
            {
                PlayWav(PlayList.List
                        .Where(w => w.PlayListGroup == PlayListGroup.SingleFace).ToList()
                    );   
            }
             else
            {
                PlayWav(PlayList.List
                        .Where(w => w.PlayListGroup == PlayListGroup.MultiFace).ToList()
                    );

            }
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

            var playbackList = new MediaPlaybackList();
            foreach (var item in list)
            {
                var mediaSource = MediaSource.CreateFromUri(new Uri($"ms-appx:///{item.Name}"));
                playbackList.Items.Add(new MediaPlaybackItem(mediaSource));
            }
            
            mediaPlayer.Source = playbackList;
            mediaPlayer.Play();

            }

        private void PositionChanged(MediaPlaybackSession sender, object args)
        {
            
            if (sender.Position >= sender.NaturalDuration)
            {
                sender.Position = new TimeSpan(0, 0, 0);
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
