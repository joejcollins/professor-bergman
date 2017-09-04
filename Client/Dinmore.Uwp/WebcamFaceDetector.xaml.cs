using Dinmore.Uwp.Infrastructure.Media;
using Dinmore.Uwp.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.FaceAnalysis;
using Windows.Media.MediaProperties;
using Windows.Media.SpeechRecognition;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.System.Threading;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using ZXing;

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
        /// Detect any speech to the app
        /// </summary>
        private SpeechRecognizer SpeechRecognizer;

        private bool IsSpeechRecognitionInProgress = false;

        /// <summary>
        /// Speech needs enabling within settings
        /// </summary>
        private static uint HResultPrivacyStatementDeclined = 0x80045509;

        private double ApiIntervalMs;
        private int NumberMilliSecsForFacesToDisappear;
        private int NumberMilliSecsToWaitForHello;
        private int NumberMillSecsBeforeWePlayAgain;
        private TimeSpan timerInterval;

        /// <summary>
        /// The current step of the state machine for detecting faces, playing sounds etc.
        /// </summary>
        public DetectionState CurrentState { get; set; }

        public ObservableCollection<StatusMessage> StatusLog { get; set; } = new ObservableCollection<StatusMessage>();

        private static ResourceLoader AppSettings;

        private IVoicePlayer vp = new VoicePlayerGenerated();

        private VoicePlayerGenerated vpGenerated = new VoicePlayerGenerated();


        private const string _DeviceExhibitKey = "DeviceExhibit";
        private const string _DeviceLabelKey = "DeviceLabel";

        private const string _DeviceIdKey = "DeviceId";
        private const string _InteractiveKey = "Interactive";
        private const string _VerbaliseSystemInformationOnBootKey = "VerbaliseSystemInformationOnBoot";
        private const string _SoundOnKey = "SoundOn";
        private const string _ResetOnBootKey = "ResetOnBoot";
        private const string _VoicePackageUrlKey = "VoicePackageUrl";
        private const string _QnAKnowledgeBaseIdKey = "QnAKnowledgeBaseId";

        /// <summary>
        /// Initializes a new instance of the <see cref="WebcamFaceDetector"/> class.
        /// </summary>
        public WebcamFaceDetector()
        {
            //Defaults
            AppSettings = ResourceLoader.GetForCurrentView();
            NumberMilliSecsForFacesToDisappear =
                int.Parse(AppSettings.GetString("NumberMilliSecsForFacesToDisappear"));
            NumberMilliSecsToWaitForHello =
                int.Parse(AppSettings.GetString("NumberMilliSecsToWaitForHello"));
            NumberMillSecsBeforeWePlayAgain =
                int.Parse(AppSettings.GetString("NumberMillSecsBeforeWePlayAgain"));
            var timerIntervalMilliSecs =
                int.Parse(AppSettings.GetString("TimerIntervalMilliSecs"));
            timerInterval = TimeSpan.FromMilliseconds(timerIntervalMilliSecs);

            ApiIntervalMs =
                double.Parse(AppSettings.GetString("ApiIntervalMilliSecs"));


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
            //get device settings here
            await UpdateDeviceSettings();
            this.vp = await Infrastructure.VoicePackageService.VoicePlayerFactory();

            // VerbaliseSystemInformation
            if (ApplicationData.Current.LocalSettings.Values[_VerbaliseSystemInformationOnBootKey] != null)
            {
                var verbaliseSystemInformationOnBoot = (bool)ApplicationData.Current.LocalSettings.Values[_VerbaliseSystemInformationOnBootKey];
                if (verbaliseSystemInformationOnBoot)
                {
                    Say($"The IP address is: {GetLocalIp()}");
                    Say($"The exhibit is {ApplicationData.Current.LocalSettings.Values[_DeviceExhibitKey]}");
                    Say($"The device label is {ApplicationData.Current.LocalSettings.Values[_DeviceLabelKey]}");
                }
            }

            // The 'await' operation can only be used from within an async method but class constructors
            // cannot be labeled as async, and so we'll initialize FaceTracker here.
            if (faceTracker == null)
            {
                faceTracker = await FaceTracker.CreateAsync();
                ChangeDetectionState(DetectionStates.Startup);
            }

            if (ApplicationData.Current.LocalSettings.Values[_InteractiveKey] != null)
            {
                if (!(bool)ApplicationData.Current.LocalSettings.Values[_InteractiveKey])
                {
                    // Prompt for permission to access the microphone. This request will only happen
                    // once, it will not re-prompt if the user rejects the permission.
                    if (!await AudioCapturePermissions.RequestMicrophonePermission())
                    {
                        Say(AppSettings.GetString("MicrophonePrivacyDeclined"));
                    }
                    else
                    {
                        try
                        {
                            Debug.WriteLine($"Creating speech recognizer");
                            SpeechRecognizer = new SpeechRecognizer();
                            SpeechRecognizer.Timeouts.InitialSilenceTimeout = TimeSpan.FromDays(1);

                            SpeechRecognizer.StateChanged += SpeechRecognizer_StateChanged;

                            await SpeechRecognizer.CompileConstraintsAsync();
                        }
                        catch (Exception exp)
                        {
                            Say($"There was an error starting the speech recognizer: {exp.Message}");
                        }
                    }
                }
            }
        }

        private void SpeechRecognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            // Noisy..
            //Debug.WriteLine($"Speech recognizer state changed: {args.State}");
        }

        /// <summary>
        /// Updates local device settinsg from the device API
        /// </summary>
        /// <returns>True if the function worked. False if teh device has not been onboarded or there was a problem</returns>
        private async Task<bool> UpdateDeviceSettings()
        {
            Say("Getting device settings");

            // First check if we have a device ID (has the device been onboarded yet?)
            var deviceId = ApplicationData.Current.LocalSettings.Values[_DeviceIdKey];
            if (deviceId == null)
            {
                LogStatusMessage($"No Device ID. Cannot get device settings.", StatusSeverity.Error);
                return false;
            }

            var deviceApiUrl = AppSettings.GetString("DeviceApiUrl");

            // Call device API to get device settings
            Device device;
            using (var httpClient = new HttpClient())
            {
                var responseMessage = await httpClient.GetAsync(deviceApiUrl);

                if (!responseMessage.IsSuccessStatusCode)
                {
                    LogStatusMessage($"The Device API returned a non-sucess status {responseMessage.ReasonPhrase}", StatusSeverity.Error);
                    return false;
                }

                //This will return all devices in the Azure table store because there is not yet an API call to get a specific device
                //TO DO: Add and API call to return a speicfic device and update this code to use that call
                var response = await responseMessage.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<Device>>(response);

                //filter for this device. We won't need to do this if/when the API gets updated with the capability to return a specific device
                device = result.Where(d => d.Id.ToString() == deviceId.ToString()).FirstOrDefault();
            }

            if (device == null)
            {
                LogStatusMessage($"Could not find this device in the device data store. Suggest this device is reset and re-onboarded.", StatusSeverity.Error);
                return false;
            }

            // Is the reset flag true?
            if (device.ResetOnBoot)
            {
                Say($"Resetting device settings.");

                //set all settings to null
                ApplicationData.Current.LocalSettings.Values[_DeviceExhibitKey] = null;
                ApplicationData.Current.LocalSettings.Values[_DeviceLabelKey] = null;
                ApplicationData.Current.LocalSettings.Values[_DeviceIdKey] = null;
                ApplicationData.Current.LocalSettings.Values[_InteractiveKey] = null;
                ApplicationData.Current.LocalSettings.Values[_VerbaliseSystemInformationOnBootKey] = null;
                ApplicationData.Current.LocalSettings.Values[_SoundOnKey] = null;
                ApplicationData.Current.LocalSettings.Values[_ResetOnBootKey] = null;
                ApplicationData.Current.LocalSettings.Values[_VoicePackageUrlKey] = null;
                ApplicationData.Current.LocalSettings.Values[_QnAKnowledgeBaseIdKey] = null;

                // Call API to set ResetOnBoot flag to false to avoid a loop
                var responseString = string.Empty;
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(deviceApiUrl + "/" + device.Id.ToString());

                    //construct full API endpoint uri
                    var fullUrl = $"{httpClient.BaseAddress}?DeviceLabel={device.DeviceLabel}&Exhibit={device.Exhibit}&Venue={device.Venue}&Interactive={device.Interactive}&VerbaliseSystemInformationOnBoot={device.VerbaliseSystemInformationOnBoot}&SoundOn={device.SoundOn}&ResetOnBoot=false&VoicePackageUrl={device.VoicePackageUrl}&QnAKnowledgeBaseId={device.QnAKnowledgeBaseId}";

                    var responseMessage = await httpClient.PutAsync(fullUrl, null);
                    responseString = await responseMessage.Content.ReadAsStringAsync();
                }

            }
            else
            {
                // Store device settings in Windows local app settings
                ApplicationData.Current.LocalSettings.Values[_DeviceExhibitKey] = device.Exhibit;
                ApplicationData.Current.LocalSettings.Values[_DeviceLabelKey] = device.DeviceLabel;
                ApplicationData.Current.LocalSettings.Values[_InteractiveKey] = device.Interactive;
                ApplicationData.Current.LocalSettings.Values[_VerbaliseSystemInformationOnBootKey] = device.VerbaliseSystemInformationOnBoot;
                ApplicationData.Current.LocalSettings.Values[_SoundOnKey] = device.SoundOn;
                ApplicationData.Current.LocalSettings.Values[_ResetOnBootKey] = device.ResetOnBoot;
                ApplicationData.Current.LocalSettings.Values[_VoicePackageUrlKey] = device.VoicePackageUrl;
                ApplicationData.Current.LocalSettings.Values[_QnAKnowledgeBaseIdKey] = device.QnAKnowledgeBaseId;
            }

            return true;
        }

        private string GetLocalIp()
        {
            // TODO: Make this more robust https://github.com/blackradley/dinmore/issues/29
            try
            {
                var icp = NetworkInformation.GetInternetConnectionProfile();

                if (icp?.NetworkAdapter == null) return null;
                var hostname =
                    NetworkInformation.GetHostNames()
                        .SingleOrDefault(
                            hn =>
                                hn.IPInformation?.NetworkAdapter != null && hn.IPInformation.NetworkAdapter.NetworkAdapterId
                                == icp.NetworkAdapter.NetworkAdapterId);

                // the ip address
                return hostname?.CanonicalName;
            }
            catch
            {
                return "Greedy, 2 network cards";
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
        /// Get a given camera id if provided
        /// </summary>
        /// <returns></returns>
        private async Task<DeviceInformation> GetDesiredWebcameraDeviceAsync()
        {
            // Finds all video capture devices
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            DeviceInformation desiredDevice = devices.FirstOrDefault(x => x.Name.Equals(AppSettings.GetString("CameraDeviceName")));
            return desiredDevice ?? devices.FirstOrDefault();
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

                var device = await GetDesiredWebcameraDeviceAsync();
                var settings = new MediaCaptureInitializationSettings { VideoDeviceId = device.Id, StreamingCaptureMode = StreamingCaptureMode.Video };
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
                // There is now webcam present. Please Intall One.

                Say("There is no webcam present, please add a USB webcam and restart the exhibit");

                // If the user has disabled their webcam this exception is thrown; provide a descriptive message to inform the user of this fact.
                //LogStatusMessage("Webcam is disabled or access to the webcam is disabled for this app.\nEnsure Privacy Settings allow webcam usage.", StatusSeverity.Error);
                successful = false;

            }
            catch (Exception ex)
            {
                Say("There is no webcam present, please add a USB webcam and restart the exhibit");
                //LogStatusMessage("Unable to start camera: " + ex.ToString(), StatusSeverity.Error);
                successful = false;
            }

            return successful;
        }

        private async Task StartSpeechRecognition()
        {
            // End previous session
            await StopSpeechRecognition();

            try
            {
                SpeechRecognizer.ContinuousRecognitionSession.Completed += ContinuousRecognitionSession_Completed;
                SpeechRecognizer.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated;

                //Debug.WriteLine($"StartSpeechRecognition: Starting recognizer");
                //LogStatusMessage($"StartSpeechRecognition: Starting recognizer", StatusSeverity.Error);

                vpGenerated.Stop();
                if (!vpGenerated.IsCurrentlyPlaying)
                {
                    await SpeechRecognizer.ContinuousRecognitionSession.StartAsync();
                    IsSpeechRecognitionInProgress = true;
                    Debug.WriteLine($"StartSpeechRecognition: Recognizer started successfully");
                    LogStatusMessage($"StartSpeechRecognition: Recognizer started successfully", StatusSeverity.Error);
                }
                else
                {
                    Debug.WriteLine($"StartSpeechRecognition: Playing audio, skipping recogition");
                    LogStatusMessage($"StartSpeechRecognition: Playing audio, skipping recogition", StatusSeverity.Error);
                }
            }
            catch (Exception exp)
            {
                Debug.WriteLine($"StartSpeechRecognition: Error {exp}");
                //Say($"There was a problem with the speech recognizer: {exp.InnerException}");
                return;
            }
        }

        private async void ContinuousRecognitionSession_ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            Debug.WriteLine($"ContinuousRecognitionSession_ResultGenerated: Result generated: {args.Result.Status}");
            if (args.Result.Status != SpeechRecognitionResultStatus.Success)
            {
                return;
            }

            Debug.WriteLine($"ContinuousRecognitionSession_ResultGenerated: Result: {args.Result.Text} (Confidence: {args.Result.Confidence})");
            LogStatusMessage($"ContinuousRecognitionSession_ResultGenerated: Result: {args.Result.Text} (Confidence: {args.Result.Confidence})", StatusSeverity.Info);
            if (args.Result.Confidence == SpeechRecognitionConfidence.Low || args.Result.Text?.Length == 0)
            {
                return;
            }

            try
            {
                string conversationId = await PostMessageToApiAsync(args.Result.Text);
                if (conversationId == null)
                {
                    Debug.WriteLine($"ContinuousRecognitionSession_ResultGenerated: No conversation Id returned from bot");
                    LogStatusMessage($"ContinuousRecognitionSession_ResultGenerated: No conversation Id returned from bot", StatusSeverity.Info);
                    return;
                }

                string botResponse = await GetMessageFromApiAsync(conversationId) ?? "No reply";
                Debug.WriteLine($"ContinuousRecognitionSession_ResultGenerated: Bot response {botResponse}");
                LogStatusMessage($"ContinuousRecognitionSession_ResultGenerated: Bot response {botResponse}", StatusSeverity.Info);
                Say(botResponse);
            }
            catch (Exception exp)
            {
                Debug.WriteLine($"ContinuousRecognitionSession_ResultGenerated: Error calling bot {exp}");
                LogStatusMessage($"ContinuousRecognitionSession_ResultGenerated: Error calling bot {exp}", StatusSeverity.Error);
            }
        }

        private void ContinuousRecognitionSession_Completed(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            Debug.WriteLine($"Speech recognizer session completed: {args.Status}");
            LogStatusMessage($"Speech recognizer session completed: {args.Status}", StatusSeverity.Info);
            IsSpeechRecognitionInProgress = false;
        }

        private async Task StopSpeechRecognition()
        {
            // In case we're in initialising of the app and speech recognizer has not started yet
            if (SpeechRecognizer == null)
            {
                return;
            }

            //LogStatusMessage($"StopSpeechRecognition: Ending speech recognition", StatusSeverity.Info);
            //Debug.WriteLine($"StopSpeechRecognition: Ending speech recognition");

            // Unhook events
            SpeechRecognizer.ContinuousRecognitionSession.Completed -= ContinuousRecognitionSession_Completed;
            SpeechRecognizer.ContinuousRecognitionSession.ResultGenerated -= ContinuousRecognitionSession_ResultGenerated;

            if (IsSpeechRecognitionInProgress)
            {
                try
                {
                    if (SpeechRecognizer.State != SpeechRecognizerState.Idle)
                    {
                        await SpeechRecognizer.ContinuousRecognitionSession.CancelAsync();
                        LogStatusMessage($"StopSpeechRecognition: Speech recognition cancelled", StatusSeverity.Info);
                        Debug.WriteLine($"StopSpeechRecognition: Speech recognition cancelled");
                    }
                    else
                    {
                        await SpeechRecognizer.ContinuousRecognitionSession.StopAsync();
                        LogStatusMessage($"StopSpeechRecognition: Speech recognition stopped", StatusSeverity.Info);
                        Debug.WriteLine($"StopSpeechRecognition: Speech recognition stopped");
                    }
                    IsSpeechRecognitionInProgress = false;
                }
                catch (Exception exp)
                {
                    LogStatusMessage($"StopSpeechRecognition: error: {exp}", StatusSeverity.Info);
                    Debug.WriteLine($"StopSpeechRecognition error: {exp}");
                }
            }
        }

        private void Say(string phrase)
        {
            StopSpeechRecognition().GetAwaiter().GetResult();
            vpGenerated.Say(phrase);
        }

        private void RunTimer()
        {
            frameProcessingTimer = ThreadPoolTimer.CreateTimer(
                new TimerElapsedHandler(ProcessCurrentStateAsync), timerInterval);
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

                    case DetectionStates.OnBoarding:
                        var result = await ProcessCurrentVideoFrameForQRCodeAsync();
                        //if we now have a GUID, store it and then change the state
                        if (!string.IsNullOrEmpty(result))
                        {
                            //store the device id guid
                            ApplicationData.Current.LocalSettings.Values[_DeviceIdKey] = result;

                            LogStatusMessage($"Found a QR code with device id {result} which has been stored to app storage.", StatusSeverity.Info);

                            Say("I found a QR code, thanks.");

                            // Get device settings
                            await this.UpdateDeviceSettings();

                            // Update voice package
                            var voicePackageUrl = (string)ApplicationData.Current.LocalSettings.Values[_VoicePackageUrlKey];

                            Say("Downloading the voice package.");
                            await Infrastructure.VoicePackageService.DownloadVoice(voicePackageUrl);
                            Say("Unpacking the voice package.");
                            await Infrastructure.VoicePackageService.UnpackVoice(voicePackageUrl);
                            this.vp = await Infrastructure.VoicePackageService.VoicePlayerFactory(voicePackageUrl);

                            ChangeDetectionState(DetectionStates.WaitingForFaces);
                        }
                        break;

                    case DetectionStates.WaitingForFaces:
                        //LogStatusMessage("Waiting for faces", StatusSeverity.Info);
                        CurrentState.ApiRequestParameters = await ProcessCurrentVideoFrameAsync();

                        if (CurrentState.ApiRequestParameters != null)
                        {
                            ChangeDetectionState(DetectionStates.FaceDetectedOnDevice);
                        }
                        break;

                    case DetectionStates.FaceDetectedOnDevice:
                        //LogStatusMessage("Just about to send API call for faces", StatusSeverity.Info);

                        //Should we play? MORE DESC REQUIRED
                        if (CurrentState.LastImageApiPush.AddMilliseconds(ApiIntervalMs) < DateTimeOffset.UtcNow
                            && CurrentState.TimeVideoWasStopped.AddMilliseconds(NumberMillSecsBeforeWePlayAgain) < DateTimeOffset.UtcNow)
                        {
                            //ThreadPoolTimer.CreateTimer(
                            //    new TimerElapsedHandler(HelloAudioHandler),
                            //    TimeSpan.FromMilliseconds(NumberMilliSecsToWaitForHello));

                            if (!(bool)ApplicationData.Current.LocalSettings.Values[_InteractiveKey])
                            {
                                Say("I'm listening, talk to me");
                            }
                            else
                            {
                                HelloAudio();
                            }

                            CurrentState.LastImageApiPush = DateTimeOffset.UtcNow;
                            CurrentState.FacesFoundByApi = await PostImageToApiAsync(CurrentState.ApiRequestParameters.Image);

                            LogStatusMessage($"Sending faces to api", StatusSeverity.Info);
                            ChangeDetectionState(DetectionStates.ApiResponseReceived);
                        }

                        break;

                    case DetectionStates.ApiResponseReceived:
                        //LogStatusMessage("API response received", StatusSeverity.Info);

                        if (CurrentState.FacesFoundByApi != null && CurrentState.FacesFoundByApi.Any())
                        {
                            LogStatusMessage("Face(s) detected", StatusSeverity.Info);
                            ChangeDetectionState(DetectionStates.InterpretingApiResults);
                            CurrentState.FacesStillPresent = true;
                            break;
                        }
                        //ChangeDetectionState(DetectionStates.WaitingForFaces);
                        ChangeDetectionState(DetectionStates.WaitingForFacesToDisappear);
                        break;

                    case DetectionStates.InterpretingApiResults:
                        // We have faces and data, so decide what to do here (play a sound etc).
                        // You'd probably kick this off in a background thread and track it by putting a
                        // reference into the CurrentState object (new property).

                        //play media if we are not currently playing
                        CurrentState.FacesStillPresent = true;

                        if (!vp.IsCurrentlyPlaying)
                        {
                            LogStatusMessage("Starting playlist", StatusSeverity.Info);

                            if ((bool)ApplicationData.Current.LocalSettings.Values[_InteractiveKey])
                            {
                                var play = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                                {
                                    //TODO This needs 
                                    vp.Play(CurrentState);
                                });
                            }
                        }

                        // Check here if the media has finished playing or the people have walked away.
                        //ChangeDetectionState(DetectionStates.WaitingForFaces);
                        ChangeDetectionState(DetectionStates.WaitingForFacesToDisappear);

                        break;

                    //Some faces are on the device and the api has been called, and maybe the audio
                    //  is now playing
                    case DetectionStates.WaitingForFacesToDisappear:

                        CurrentState.FacesStillPresent = await AreFacesStillPresent();
                        LogStatusMessage($"Faces present: {CurrentState.FacesStillPresent}", StatusSeverity.Info);

                        //we dont have a face
                        if (!CurrentState.FacesStillPresent)
                        {
                            //TODO Refactor this out.
                            await Task.Delay(NumberMilliSecsForFacesToDisappear)
                                .ContinueWith((async t =>
                                {
                                    CurrentState.FacesStillPresent = AreFacesStillPresent().Result;
                                    if (!CurrentState.FacesStillPresent)
                                    {
                                        LogStatusMessage($"Faces have gone for a few or more secs, stop the audio playback", StatusSeverity.Info);
                                        ChangeDetectionState(DetectionStates.WaitingForFaces);
                                        vp.Stop();
                                        CurrentState.TimeVideoWasStopped = DateTimeOffset.UtcNow;

                                        // End speech recognition
                                        await StopSpeechRecognition();
                                        return;
                                    }
                                }
                                ));

                        }
                        else
                        {
                            if (!(bool)ApplicationData.Current.LocalSettings.Values[_InteractiveKey])
                            {
                                if (!IsSpeechRecognitionInProgress) await StartSpeechRecognition();
                                //if (!IsSpeechRecognitionInProgress) await Task.Run(async () => await StartSpeechRecognition());
                            }
                        }


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

        private void HelloAudio()
        {
            LogStatusMessage("Starting introduction", StatusSeverity.Info);

            vp.PlayIntroduction(CurrentState.ApiRequestParameters.Faces.Count());
            //timer.Cancel();
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

                    //build url to pass to api
                    var deviceId = ApplicationData.Current.LocalSettings.Values[_DeviceIdKey];
                    var url = AppSettings.GetString("FaceApiUrl");
                    var returnFaceLandmarks = AppSettings.GetString("ReturnFaceLandmarks");
                    var returnFaceAttributes = AppSettings.GetString("ReturnFaceAttributes");
                    url = $"{url}?deviceid={deviceId}&returnFaceLandmarks={returnFaceLandmarks}&returnFaceAttributes={returnFaceAttributes}";

                    var responseMessage = await httpClient.PostAsync(url, content);

                    if (!responseMessage.IsSuccessStatusCode)
                    {
                        switch (responseMessage.StatusCode.ToString())
                        {
                            case "BadRequest":
                                LogStatusMessage("The API returned a 400 Bad Request. This is caused by either a missing DeviceId parameter or one containig a GUID that is not already registered with the device API.", StatusSeverity.Error);
                                break;
                            default:
                                LogStatusMessage($"The API returned a non-sucess status {responseMessage.ReasonPhrase}", StatusSeverity.Error);
                                break;
                        }
                        return null;
                    }

                    var response = await responseMessage.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<List<Face>>(response);

                    return result;
                }
            }
            catch (Exception ex)
            {
                vp.Stop();
                LogStatusMessage("Exception: " + ex.ToString(), StatusSeverity.Error);
                return null;
            }
        }

        private async Task<string> PostMessageToApiAsync(string messageText)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    //var content = new StringContent(messageText);
                    var url = AppSettings.GetString("BotApiUrl");
                    var deviceId = ApplicationData.Current.LocalSettings.Values[_DeviceIdKey];
                    url = $"{url}?deviceid={deviceId}&message={messageText}";

                    var responseMessage = await httpClient.PostAsync(url, null);

                    if (!responseMessage.IsSuccessStatusCode)
                    {
                        switch (responseMessage.StatusCode.ToString())
                        {
                            case "BadRequest":
                                LogStatusMessage("The API returned a 400 Bad Request. This is caused by either a missing DeviceId parameter or one containig a GUID that is not already registered with the device API.", StatusSeverity.Error);
                                break;
                            default:
                                LogStatusMessage($"The API returned a non-sucess status {responseMessage.ReasonPhrase}", StatusSeverity.Error);
                                break;
                        }
                        return null;
                    }

                    var response = await responseMessage.Content.ReadAsStringAsync();
                    return response;
                    //JObject s = JObject.Parse(response);
                    //return s["id"].ToString();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PostMessageToApiAsync Exception: {ex}");
                LogStatusMessage("Exception: " + ex.ToString(), StatusSeverity.Error);
                return null;
            }
        }

        private async Task<string> GetMessageFromApiAsync(string conversationId)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var content = new StringContent(conversationId);

                    var url = AppSettings.GetString("BotApiUrl");
                    var deviceId = ApplicationData.Current.LocalSettings.Values[_DeviceIdKey];
                    url = $"{url}?conversationId={conversationId}";

                    var responseMessage = await httpClient.GetAsync(url);

                    if (!responseMessage.IsSuccessStatusCode)
                    {
                        switch (responseMessage.StatusCode.ToString())
                        {
                            case "BadRequest":
                                LogStatusMessage("The API returned a 400 Bad Request. This is caused by either a missing DeviceId parameter or one containig a GUID that is not already registered with the device API.", StatusSeverity.Error);
                                break;
                            default:
                                LogStatusMessage($"The API returned a non-sucess status {responseMessage.ReasonPhrase}", StatusSeverity.Error);
                                break;
                        }
                        return null;
                    }

                    var response = await responseMessage.Content.ReadAsStringAsync();
                    return response;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetMessageToApiAsync Exception: {ex}");
                LogStatusMessage("Exception: " + ex.ToString(), StatusSeverity.Error);
                return null;
            }
        }

        private async Task<String> ProcessCurrentVideoFrameForQRCodeAsync()
        {
            // If a lock is being held it means we're still waiting for processing work on the previous frame to complete.
            // In this situation, don't wait on the semaphore but exit immediately.
            if (!frameProcessingSemaphore.Wait(0))
            {
                return null;
            }

            var br = new BarcodeReader();

            try
            {
                const BitmapPixelFormat InputPixelFormat = BitmapPixelFormat.Nv12;
                using (var previewFrame = new VideoFrame(InputPixelFormat, (int)videoProperties.Width, (int)videoProperties.Height))
                {
                    await mediaCapture.GetPreviewFrameAsync(previewFrame);
                    var decoded = br.Decode(previewFrame.SoftwareBitmap);
                    return (decoded != null) ?
                        decoded.Text :
                        null;
                }
            }

            finally
            {
                frameProcessingSemaphore.Release();
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

                            LogStatusMessage($"Found face(s) on camera: {faces.Count}", StatusSeverity.Info);


                            return new ApiRequestParameters
                            {
                                Image = ms.ToArray(),
                                Faces = faces,
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

        private async Task<bool> AreFacesStillPresent()
        {

            try
            {
                const BitmapPixelFormat InputPixelFormat = BitmapPixelFormat.Nv12;
                using (var previewFrame = new VideoFrame(InputPixelFormat, (int)videoProperties.Width, (int)videoProperties.Height))
                {
                    await mediaCapture.GetPreviewFrameAsync(previewFrame);

                    var faces = await faceTracker.ProcessNextFrameAsync(previewFrame);
                    return faces.Any();
                }
            }
            catch (Exception ex)
            {
                LogStatusMessage("Unable to process current frame: " + ex.ToString(), StatusSeverity.Error);
                return false;  //TODO ? true or false?
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
                    CurrentState.State = newState;
                    break;
                case DetectionStates.Startup:
                    if (!await StartWebcamStreaming())
                    {
                        ChangeDetectionState(DetectionStates.Idle);
                        break;
                    }
                    // This needs to test for if a GUID as stored
                    var deviceId = ApplicationData.Current.LocalSettings.Values[_DeviceIdKey];
                    if (deviceId == null)
                    {
                        Say("I have no device ID. I'm now onboarding, show me a QR code containing a device ID.");
                        ChangeDetectionState(DetectionStates.OnBoarding);
                    }
                    else
                    {
                        ChangeDetectionState(DetectionStates.WaitingForFaces);
                    }
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
