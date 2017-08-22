using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dinmore.Uwp.Models;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;
using Windows.Media.Core;

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

        public void Play(DetectionState currentState)
        {
            Say(new List<string>() { "Hello this is the start of a playlist" });
        }

        public void PlayIntroduction(PlayListGroup playlistGroup)
        {
            Say(new List<string>() { "This is the first paragraph", "This is the second paragraph" });
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
