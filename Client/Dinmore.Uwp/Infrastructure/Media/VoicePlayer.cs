using System;
using System.Linq;
using Dinmore.Uwp.Models;
using Windows.Media.Playback;
using Windows.Media.Core;

namespace Dinmore.Uwp.Infrastructure.Media
{
    class VoicePlayer : IDisposable
    {
        // see https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/play-audio-and-video-with-mediaplayer


        private static MediaPlayer mediaPlayer = new MediaPlayer();

        internal void Play(DetectionState currentState)
        {
            var session = mediaPlayer.PlaybackSession;
            if (session.PlaybackState == MediaPlaybackState.None)
            {


                var source = "Sheep.wav";
                if (currentState.FacesFoundByApi.Count > 1)
                    source = "Goat.wav";

                //set back to zero
                session.Position = TimeSpan.Zero;
                session.PlaybackStateChanged += Session_PlaybackStateChanged;

                mediaPlayer.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/Voice/{source}"));

                mediaPlayer.Play();
            }


        }

        private void Session_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            if (sender.PlaybackState == MediaPlaybackState.Paused)
            {
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
