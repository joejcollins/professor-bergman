using Dinmore.Uwp.Infrastructure.Media;
using Dinmore.Uwp.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Core;
using Windows.Media.FaceAnalysis;
using Windows.Media.MediaProperties;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace Dinmore.Uwp
{
    /// <summary>
    /// Page for demonstrating FaceTracking.
    /// </summary>
    public sealed partial class WebcamFaceDetector : Page
    {
        /// <summary>
        /// Brush for drawing the bounding box around each identified face.
        /// </summary>
        private readonly SolidColorBrush lineBrush = new SolidColorBrush(Windows.UI.Colors.Yellow);

        /// <summary>
        /// Thickness of the face bounding box lines.
        /// </summary>
        private readonly double lineThickness = 2.0;

        /// <summary>
        /// Transparent fill for the bounding box.
        /// </summary>
        private readonly SolidColorBrush fillBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);

        /// <summary>
        /// References a MediaCapture instance; is null when not in Streaming state.
        /// </summary>
        private MediaCapture mediaCapture;

        /// <summary>
        /// Cache of properties from the current MediaCapture device which is used for capturing the preview frame.
        /// </summary>
        private VideoEncodingProperties videoProperties;

        /// <summary>
        /// References a FaceTracker instance.
        /// </summary>
        private FaceTracker faceTracker;

        /// <summary>
        /// A periodic timer to execute FaceTracker on preview frames
        /// </summary>
        private ThreadPoolTimer frameProcessingTimer;

        /// <summary>
        /// Semaphore to ensure FaceTracking logic only executes one at a time
        /// </summary>
        private SemaphoreSlim frameProcessingSemaphore = new SemaphoreSlim(1);

        /// <summary>
        /// The minimum interval required between API calls.
        /// </summary>
        private const double ApiIntervalMs = 500;

        // Use a 66 millisecond interval for our timer, i.e. 15 frames per second
        private TimeSpan timerInterval = TimeSpan.FromMilliseconds(66);

        /// <summary>
        /// The current step of the state machine for detecting faces, playing sounds etc.
        /// </summary>
        public DetectionState CurrentState { get; set; }

        public ObservableCollection<StatusMessage> StatusLog { get; set; } = new ObservableCollection<StatusMessage>();

        //private BoundingBoxCreator boundingBoxCreator = new BoundingBoxCreator();

        private static ResourceLoader AppSettings;

        private VoicePlayer vp = new VoicePlayer();

        /// <summary>
        /// Initializes a new instance of the <see cref="WebcamFaceDetector"/> class.
        /// </summary>
        public WebcamFaceDetector()
        {
            AppSettings = ResourceLoader.GetForCurrentView();

            InitializeComponent();

            CurrentState = new DetectionState { State = DetectionStates.Idle };
            App.Current.Suspending += OnSuspending;
        }

        /// <summary>
        /// Responds when we navigate to this page.
        /// </summary>
        /// <param name="e">Event data</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // The 'await' operation can only be used from within an async method but class constructors
            // cannot be labeled as async, and so we'll initialize FaceTracker here.
            if (faceTracker == null)
            {
                faceTracker = await FaceTracker.CreateAsync();
                ChangeDetectionState(DetectionStates.Startup);
            }
        }

        /// <summary>
        /// Responds to App Suspend event to stop/release MediaCapture object if it's running and return to Idle state.
        /// </summary>
        /// <param name="sender">The source of the Suspending event</param>
        /// <param name="e">Event data</param>
        private void OnSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            if (CurrentState.State == DetectionStates.Startup || CurrentState.State == DetectionStates.WaitingForFaces)
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                try
                {
                    ChangeDetectionState(DetectionStates.Idle);
                }
                finally
                {
                    deferral.Complete();
                }
            }
        }

        /// <summary>
        /// Initializes a new MediaCapture instance and starts the Preview streaming to the CamPreview UI element.
        /// </summary>
        /// <returns>Async Task object returning true if initialization and streaming were successful and false if an exception occurred.</returns>
        private async Task<bool> StartWebcamStreaming()
        {
            bool successful = true;

            try
            {
                mediaCapture = new MediaCapture();

                // For this scenario, we only need Video (not microphone) so specify this in the initializer.
                // NOTE: the appxmanifest only declares "webcam" under capabilities and if this is changed to include
                // microphone (default constructor) you must add "microphone" to the manifest or initialization will fail.
                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
                settings.StreamingCaptureMode = StreamingCaptureMode.Video;
                await mediaCapture.InitializeAsync(settings);
                mediaCapture.Failed += MediaCapture_CameraStreamFailed;

                // Cache the media properties as we'll need them later.
                var deviceController = mediaCapture.VideoDeviceController;
                videoProperties = deviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;

                // Immediately start streaming to our CaptureElement UI.
                // NOTE: CaptureElement's Source must be set before streaming is started.
                CamPreview.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();

                RunTimer();
            }
            catch (UnauthorizedAccessException)
            {
                // If the user has disabled their webcam this exception is thrown; provide a descriptive message to inform the user of this fact.
                LogStatusMessage("Webcam is disabled or access to the webcam is disabled for this app.\nEnsure Privacy Settings allow webcam usage.", StatusSeverity.Error);
                successful = false;
            }
            catch (Exception ex)
            {
                LogStatusMessage("Unable to start camera: " + ex.ToString(), StatusSeverity.Error);
                successful = false;
            }

            return successful;
        }

        private void RunTimer()
        {
            frameProcessingTimer = ThreadPoolTimer.CreateTimer(new TimerElapsedHandler(ProcessCurrentStateAsync), timerInterval);
        }

        /// <summary>
        /// Safely stops webcam streaming (if running) and releases MediaCapture object.
        /// </summary>
        private async void ShutdownWebCam()
        {
            if (frameProcessingTimer != null)
            {
                frameProcessingTimer.Cancel();
            }

            if (mediaCapture != null)
            {
                if (mediaCapture.CameraStreamState == Windows.Media.Devices.CameraStreamState.Streaming)
                {
                    try
                    {
                        await mediaCapture.StopPreviewAsync();
                    }
                    catch (Exception)
                    {
                        ;   // Since we're going to destroy the MediaCapture object there's nothing to do here
                    }
                }
                mediaCapture.Dispose();
            }

            frameProcessingTimer = null;
            CamPreview.Source = null;
            mediaCapture = null;
        }

        private async void ProcessCurrentStateAsync(ThreadPoolTimer timer)
        {
            try
            {
                switch (CurrentState.State)
                {
                    case DetectionStates.Idle:
                        break;
                    case DetectionStates.Startup:
                        break;
                    case DetectionStates.WaitingForFaces:
                        LogStatusMessage("Waiting for faces", StatusSeverity.Info);
                        CurrentState.ApiRequestParameters = await ProcessCurrentVideoFrameAsync();

                        if (CurrentState.ApiRequestParameters != null)
                        {
                            LogStatusMessage("Detected face(s)", StatusSeverity.Info);
                            ChangeDetectionState(DetectionStates.FaceDetectedOnDevice);
                        }
                        break;

                    case DetectionStates.FaceDetectedOnDevice:
                        LogStatusMessage("Just about to send API call for faces", StatusSeverity.Info);

                        if (CurrentState.LastImageApiPush.AddMilliseconds(ApiIntervalMs) < DateTimeOffset.UtcNow)
                        {
                            CurrentState.LastImageApiPush = DateTimeOffset.UtcNow;
                            CurrentState.FacesFoundByApi = await PostImageToApiAsync(CurrentState.ApiRequestParameters.Image);
                            ChangeDetectionState(DetectionStates.ApiResponseReceived);
                        }
                        break;

                    case DetectionStates.ApiResponseReceived:
                        LogStatusMessage("API response received", StatusSeverity.Info);
                        if (CurrentState.FacesFoundByApi != null && CurrentState.FacesFoundByApi.Any())
                        {
                            LogStatusMessage("Face(s) detected", StatusSeverity.Info);
                            ChangeDetectionState(DetectionStates.InterpretingApiResults);

                            break;
                        }
                        ChangeDetectionState(DetectionStates.WaitingForFaces);
                        break;

                    case DetectionStates.InterpretingApiResults:
                        // We have faces and data, so decide what to do here (play a sound etc).
                        // You'd probably kick this off in a background thread and track it by putting a
                        // reference into the CurrentState object (new property).

                        //play media if we are not currently playing
                        if (!vp.IsCurrentlyPlaying)
                        {
                            var play = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                //TODO This needs 
                                vp.Play(CurrentState);
                            });
                        }



                        //rectanngles post display, commenting for now
                        //var ignored = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        //{
                        //    SetupVisualization(CurrentState);
                        //});

                        // Check here if the media has finished playing or the people have walked away.
                        ChangeDetectionState(DetectionStates.WaitingForFaces);

                        break;
                    default:
                        ChangeDetectionState(DetectionStates.Idle);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogStatusMessage("Unable to process current frame. " + ex.ToString(), StatusSeverity.Error);
            }
            finally
            {
                RunTimer();
            }
        }

        private void LogStatusMessage(string message, StatusSeverity severity)
        {
            var ignored = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                while (StatusLog.Count > 100)
                {
                    StatusLog.RemoveAt(StatusLog.Count - 1);
                }

                StatusLog.Insert(0, new StatusMessage(message, severity));
            });
        }

        private async Task<List<Face>> PostImageToApiAsync(byte[] image)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var content = new StreamContent(new MemoryStream(image));
                    content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/octet-stream");

                    //build url to pass to api, REFACTORING NEEDED
                    var url = AppSettings.GetString("FaceApiUrl");
                    var device = AppSettings.GetString("Device");
                    var exhibit = AppSettings.GetString("Exhibit");
                    url = $"{url}?device={device}&exhibit={exhibit}";

                    var responseMessage = await httpClient.PostAsync(url, content);

                    var response = await responseMessage.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<List<Face>>(response);

                    return result;
                }
            }
            catch (Exception ex)
            {
                LogStatusMessage("Exception: " + ex.ToString(), StatusSeverity.Error);
                return null;
            }
        }

        /// <summary>
        /// Extracts a frame from the camera stream and detects if any faces are found. Used as a precursor to making an expensive API
        /// call to get proper face details.
        /// </summary>
        /// <remarks>
        /// Keep in mind this method is called from a Timer and not synchronized with the camera stream. Also, the processing time of FaceTracker
        /// will vary depending on the size of each frame and the number of faces being tracked. That is, a large image with several tracked faces may
        /// take longer to process.
        /// </remarks>
        private async Task<ApiRequestParameters> ProcessCurrentVideoFrameAsync()
        {
            // If a lock is being held it means we're still waiting for processing work on the previous frame to complete.
            // In this situation, don't wait on the semaphore but exit immediately.
            if (!frameProcessingSemaphore.Wait(0))
            {
                return null;
            }

            try
            {
                // Create a VideoFrame object specifying the pixel format we want our capture image to be (NV12 bitmap in this case).
                // GetPreviewFrame will convert the native webcam frame into this format.
                const BitmapPixelFormat InputPixelFormat = BitmapPixelFormat.Nv12;
                using (var previewFrame = new VideoFrame(InputPixelFormat, (int)videoProperties.Width, (int)videoProperties.Height))
                {
                    await mediaCapture.GetPreviewFrameAsync(previewFrame);


                    // The returned VideoFrame should be in the supported NV12 format but we need to verify this.
                    if (!FaceDetector.IsBitmapPixelFormatSupported(previewFrame.SoftwareBitmap.BitmapPixelFormat))
                    {
                        throw new NotSupportedException("PixelFormat '" + InputPixelFormat.ToString() + "' is not supported by FaceDetector");
                    }
                    var faces = await faceTracker.ProcessNextFrameAsync(previewFrame);
                    if (faces.Any())
                    {
                        // Found faces so create a bounding rectangle and store the parameters to make the API call and process the response.
                        using (var ms = new MemoryStream())
                        {
                            // It'll be faster to send a smaller rectangle of the faces found instead of the whole image. This is what we do here.
                            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, ms.AsRandomAccessStream());

                            // To use the encoder to resize we need to change the bitmap format. Might be a better way to do this, I can't see it.
                            var converted = SoftwareBitmap.Convert(previewFrame.SoftwareBitmap, BitmapPixelFormat.Rgba16);

                            encoder.SetSoftwareBitmap(converted);
                            //var bounds = boundingBoxCreator.BoundingBoxForFaces(faces, converted.PixelWidth, converted.PixelHeight);
                            //encoder.BitmapTransform.Bounds = bounds;
                            await encoder.FlushAsync();

                            return new ApiRequestParameters
                            {
                                Image = ms.ToArray(),
                                //ImageBounds = bounds,
                                //OriginalImageHeight = converted.PixelHeight,
                                //OriginalImageWidth = converted.PixelWidth,
                            };
                        }
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogStatusMessage("Unable to process current frame: " + ex.ToString(), StatusSeverity.Error);
                return null;
            }
            finally
            {
                frameProcessingSemaphore.Release();
            }
        }


        /// <summary>
        /// Manages the scenario's internal state. Invokes the internal methods and updates the UI according to the
        /// passed in state value. Handles failures and resets the state if necessary.
        /// </summary>
        /// <param name="newState">State to switch to</param>
        private async void ChangeDetectionState(DetectionStates newState)
        {
            switch (newState)
            {
                case DetectionStates.Idle:
                    ShutdownWebCam();
                    VisualizationCanvas.Children.Clear();
                    CurrentState.State = newState;
                    break;
                case DetectionStates.Startup:
                    if (!await StartWebcamStreaming())
                    {
                        ChangeDetectionState(DetectionStates.Idle);
                        break;
                    }
                    VisualizationCanvas.Children.Clear();
                    ChangeDetectionState(DetectionStates.WaitingForFaces);
                    break;
                default:
                    CurrentState.State = newState;
                    break;
            }
        }

        /// <summary>
        /// Handles MediaCapture stream failures by shutting down streaming and returning to Idle state.
        /// </summary>
        /// <param name="sender">The source of the event, i.e. our MediaCapture object</param>
        /// <param name="args">Event data</param>
        private void MediaCapture_CameraStreamFailed(MediaCapture sender, object args)
        {
            // MediaCapture is not Agile and so we cannot invoke its methods on this caller's thread
            // and instead need to schedule the state change on the UI thread.
            var ignored = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ChangeDetectionState(DetectionStates.Idle);
            });
        }
    }
}
