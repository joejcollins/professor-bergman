using Dinmore.Uwp.Constants;
using Dinmore.Uwp.Helpers;
using Dinmore.Uwp.Infrastructure.Media;
using Dinmore.Uwp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using Windows.Networking;
using Windows.Networking.Connectivity;
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
        /// Semaphore to ensure Speech Reconigtion logic only executes one at a time
        /// </summary>
        private SemaphoreSlim speechRecognitionProcessingSemaphore = new SemaphoreSlim(1);


        /// <summary>
        /// Detect any speech to the app
        /// </summary>
        private SpeechRecognizer SpeechRecognizer;

        private bool IsSpeechRecognitionInProgress = false;

        private double ApiIntervalMs;
        private int NumberMilliSecsForFacesToDisappear;
        private int NumberMilliSecsToWaitForHello;
        private int NumberMillSecsBeforeWePlayAgain;
        private int NumberMilliSecsForSpeechRecognitionTimeout;
        private TimeSpan TimerInterval;

        /// <summary>
        /// The current step of the state machine for detecting faces, playing sounds etc.
        /// </summary>
        public DetectionState CurrentState { get; set; }

        public ObservableCollection<StatusMessage> StatusLog { get; set; } = new ObservableCollection<StatusMessage>();

        private static ResourceLoader AppSettings;

        private IVoicePlayer vp = new VoicePlayerGenerated();

        private VoicePlayerGenerated vpGenerated = new VoicePlayerGenerated();

        private VoicePlayerSpeechRecog vpSpeechRecog = new VoicePlayerSpeechRecog();

        /// <summary>
        /// Initializes a new instance of the <see cref="WebcamFaceDetector"/> class.
        /// </summary>
        public WebcamFaceDetector()
        {
            //Defaults
            AppSettings = ResourceLoader.GetForCurrentView();
            NumberMilliSecsForFacesToDisappear = int.Parse(AppSettings.GetString("NumberMilliSecsForFacesToDisappear"));
            NumberMilliSecsToWaitForHello = int.Parse(AppSettings.GetString("NumberMilliSecsToWaitForHello"));
            NumberMillSecsBeforeWePlayAgain = int.Parse(AppSettings.GetString("NumberMillSecsBeforeWePlayAgain"));
            TimerInterval = TimeSpan.FromMilliseconds(int.Parse(AppSettings.GetString("TimerIntervalMilliSecs")));
            ApiIntervalMs = double.Parse(AppSettings.GetString("ApiIntervalMilliSecs"));
            NumberMilliSecsForSpeechRecognitionTimeout = int.Parse(AppSettings.GetString("NumberMilliSecsForSpeechRecognitionTimeout"));

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
            if (Settings.GetBool(DeviceSettingKeys.VerbaliseSystemInformationOnBootKey))
            {
                LogStatusMessage($"The IP address is: {GetLocalIp()}", StatusSeverity.Info, true);
                LogStatusMessage($"The exhibit is {Settings.GetString(DeviceSettingKeys.DeviceExhibitKey)}", StatusSeverity.Info, true);
                LogStatusMessage($"The device label is {Settings.GetString(DeviceSettingKeys.DeviceLabelKey)}", StatusSeverity.Info, true);
            }

            // Only check microphone enabled and create speech objects if we're running in interactive (QnA) mode
            if (Settings.GetBool(DeviceSettingKeys.InteractiveKey))
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
                        Debug.WriteLine($"Initialising speech recognizer"); //This can fail randomly
                        SpeechRecognizer = new SpeechRecognizer();
                        SpeechRecognizer.Timeouts.InitialSilenceTimeout = TimeSpan.FromMilliseconds(NumberMilliSecsForSpeechRecognitionTimeout);
                        await SpeechRecognizer.CompileConstraintsAsync();
                        Debug.WriteLine($"Speech recognizer initialised");
                    }
                    catch (Exception exp)
                    {
                        Say($"There was an error initialising the speech recognizer: {exp.Message}");
                    }
                }
            }

            if (faceTracker == null)
            {
                faceTracker = await FaceTracker.CreateAsync();
                ChangeDetectionState(DetectionStates.Startup);
            }

        }

        /// <summary>
        /// Updates local device settings from the device API
        /// </summary>
        /// <returns>True if the function worked. False if the device has not been onboarded or there was a problem</returns>
        private async Task<bool> UpdateDeviceSettings()
        {
            LogStatusMessage("Getting device settings", StatusSeverity.Info, false);

            // First check if we have a device ID (has the device been onboarded yet?)
            //var deviceId = ApplicationData.Current.LocalSettings.Values[_DeviceIdKey];
            var deviceId = Settings.GetString(DeviceSettingKeys.DeviceIdKey);
            if (deviceId == null)
            {
                LogStatusMessage($"No Device ID. Cannot get device settings.", StatusSeverity.Error, false);
                return false;
            }

            // Call device API to get device settings
            Device device = await Api.GetDevice(AppSettings, deviceId);

            if (device == null)
            {
                LogStatusMessage($"Could not find this device in the device data store. Suggest this device is reset and re-onboarded.", StatusSeverity.Error, false);
                return false;
            }

            // Is the reset flag true?
            if (device.ResetOnBoot)
            {
                LogStatusMessage($"Resetting device settings.", StatusSeverity.Info, true);

                //set all settings to null
                Settings.Set(DeviceSettingKeys.DeviceExhibitKey, null);
                Settings.Set(DeviceSettingKeys.DeviceLabelKey, null);
                Settings.Set(DeviceSettingKeys.DeviceIdKey, null);
                Settings.Set(DeviceSettingKeys.InteractiveKey, null);
                Settings.Set(DeviceSettingKeys.VerbaliseSystemInformationOnBootKey, null);
                Settings.Set(DeviceSettingKeys.SoundOnKey, null);
                Settings.Set(DeviceSettingKeys.ResetOnBootKey, null);
                Settings.Set(DeviceSettingKeys.VoicePackageUrlKey, null);
                Settings.Set(DeviceSettingKeys.QnAKnowledgeBaseIdKey, null);

                // Call API to set ResetOnBoot flag to false to avoid a loop
                await Api.UpdateDevice(AppSettings, device);
            }
            else
            {
                //capture the previous url before it potentially gets changed
                var preLoadVoicePackageUrl = Settings.GetString(DeviceSettingKeys.VoicePackageUrlKey);

                // Store device settings in Windows local app settings
                Settings.Set(DeviceSettingKeys.DeviceExhibitKey, device.Exhibit);
                Settings.Set(DeviceSettingKeys.DeviceLabelKey, device.DeviceLabel);
                Settings.Set(DeviceSettingKeys.InteractiveKey, device.Interactive);
                Settings.Set(DeviceSettingKeys.VerbaliseSystemInformationOnBootKey, device.VerbaliseSystemInformationOnBoot);
                Settings.Set(DeviceSettingKeys.SoundOnKey, device.SoundOn);
                Settings.Set(DeviceSettingKeys.ResetOnBootKey, device.ResetOnBoot);
                Settings.Set(DeviceSettingKeys.QnAKnowledgeBaseIdKey, device.QnAKnowledgeBaseId);
                Settings.Set(DeviceSettingKeys.VoicePackageUrlKey, device.VoicePackageUrl);

                //Download the voice package only if the local and cloud Voice package url does not match
                if (device.VoicePackageUrl != preLoadVoicePackageUrl)
                {
                    LogStatusMessage("Looks like there is a new voice package so downloading it (could be a while).", StatusSeverity.Info, true);
                    await Infrastructure.VoicePackageService.DownloadUnpackVoicePackage(Settings.GetString(DeviceSettingKeys.VoicePackageUrlKey));
                    LogStatusMessage("Got the voice package.", StatusSeverity.Info, true);
                }
            }

            return true;
        }

        private string GetLocalIp()
        {
            // TODO: Make this more robust https://github.com/blackradley/dinmore/issues/29
            try
            {
                var ip = string.Empty;
                foreach (HostName localHostName in NetworkInformation.GetHostNames().Where(n => n.Type == HostNameType.Ipv4))
                {
                    if (localHostName.IPInformation != null)
                    {
                        ip =  localHostName.ToString();
                        break;
                    }
                }
                return ip;
            }
            catch
            {
                return "Could not find IP address";
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

                LogStatusMessage("There is no webcam present, please add a USB webcam and restart the exhibit", StatusSeverity.Info, true);

                // If the user has disabled their webcam this exception is thrown; provide a descriptive message to inform the user of this fact.
                //LogStatusMessage("Webcam is disabled or access to the webcam is disabled for this app.\nEnsure Privacy Settings allow webcam usage.", StatusSeverity.Error);
                successful = false;

            }
            catch (Exception ex)
            {
                LogStatusMessage("There is no webcam present, please add a USB webcam and restart the exhibit", StatusSeverity.Info, true);
                successful = false;
            }

            return successful;
        }

        /// <summary>
        /// Start listening for audio
        /// </summary>
        /// <returns></returns>
        private async Task StartSpeechRecognition()
        {
            try
            {
                await speechRecognitionProcessingSemaphore.WaitAsync();

                frameProcessingTimer?.Cancel();

                // End previous session
                await StopSpeechRecognition();

                SpeechRecognizer.ContinuousRecognitionSession.Completed += ContinuousRecognitionSession_Completed;
                SpeechRecognizer.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated;

                await SpeechRecognizer.ContinuousRecognitionSession.StartAsync();
                IsSpeechRecognitionInProgress = true;
                Debug.WriteLine($"StartSpeechRecognition: Recognizer started successfully");
                LogStatusMessage($"StartSpeechRecognition: Recognizer started successfully", StatusSeverity.Error, false);
            }
            catch (Exception exp)
            {
                Debug.WriteLine($"StartSpeechRecognition: Error {exp}");
            }
            finally
            {
                speechRecognitionProcessingSemaphore.Release();
            }
        }

        /// <summary>
        /// Stop listening for audio
        /// </summary>
        /// <returns></returns>
        private async Task StopSpeechRecognition()
        {
            // In case we're in initialising of the app and speech recognizer has not started yet
            if (SpeechRecognizer == null)
            {
                return;
            }

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
                }
                catch (Exception exp)
                {
                    LogStatusMessage($"StopSpeechRecognition: error: {exp}", StatusSeverity.Info);
                    Debug.WriteLine($"StopSpeechRecognition error: {exp}");
                }
                finally
                {
                    IsSpeechRecognitionInProgress = false;
                }
            }
        }

        /// <summary>
        /// Called once Speech Recognizer has detected something
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void ContinuousRecognitionSession_ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            Debug.WriteLine($"ContinuousRecognitionSession_ResultGenerated: Result generated: {args.Result.Status}");
            if (args.Result.Status != SpeechRecognitionResultStatus.Success)
            {
                return;
            }

            Debug.WriteLine($"ContinuousRecognitionSession_ResultGenerated: Result: {args.Result.Text} (Confidence: {args.Result.Confidence})");
            LogStatusMessage($"ContinuousRecognitionSession_ResultGenerated: Result: {args.Result.Text} (Confidence: {args.Result.Confidence})", StatusSeverity.Info, false);
            if (args.Result.Confidence == SpeechRecognitionConfidence.Low || args.Result.Text?.Length == 0)
            {
                return;
            }

            try
            {
                string conversationId = await Api.PostBotMessageToApiAsync(AppSettings, args.Result.Text);
                if (conversationId == null)
                {
                    Debug.WriteLine($"ContinuousRecognitionSession_ResultGenerated: No conversation Id returned from bot");
                    LogStatusMessage($"ContinuousRecognitionSession_ResultGenerated: No conversation Id returned from bot", StatusSeverity.Info, false);
                    return;
                }

                string botResponse = await Api.GetBotMessageFromApiAsync(AppSettings, conversationId) ?? "No reply";
                Debug.WriteLine($"ContinuousRecognitionSession_ResultGenerated: Bot response {botResponse}");
                LogStatusMessage($"ContinuousRecognitionSession_ResultGenerated: Bot response {botResponse}", StatusSeverity.Info);
                await StopSpeechRecognition();
                await SayAsync(botResponse);
            }
            catch (Exception exp)
            {
                Debug.WriteLine($"ContinuousRecognitionSession_ResultGenerated: Error calling bot {exp}");
                LogStatusMessage($"ContinuousRecognitionSession_ResultGenerated: Error calling bot {exp}", StatusSeverity.Error);
            }
        }

        /// <summary>
        /// Called after ContinuousRecognitionSession_ResultGenerated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ContinuousRecognitionSession_Completed(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            Debug.WriteLine($"Speech recognizer session completed: {args.Status}");
            LogStatusMessage($"Speech recognizer session completed: {args.Status}", StatusSeverity.Info);
            IsSpeechRecognitionInProgress = false;
            RunTimer();
        }

        private void Say(string phrase)
        {
            vpGenerated.Say(phrase);
        }

        /// <summary>
        /// For use by Speech Recognition - where we need async to ensure that audio has completed before starting over
        /// </summary>
        /// <param name="phrase"></param>
        private async Task SayAsync(string phrase)
        {
            await vpSpeechRecog.SayAsync(phrase);
            await StartSpeechRecognition();
        }

        private void RunTimer()
        {
            frameProcessingTimer = ThreadPoolTimer.CreateTimer(
                new TimerElapsedHandler(ProcessCurrentStateAsync), TimerInterval);
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
                Debug.WriteLine($"State machine is: {CurrentState.State}");

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
                            Settings.Set(DeviceSettingKeys.DeviceIdKey, result);
                            LogStatusMessage($"Found a QR code, thanks.", StatusSeverity.Info, true);

                            // Get device settings
                            await this.UpdateDeviceSettings();

                            this.vp = await Infrastructure.VoicePackageService.VoicePlayerFactory(Settings.GetString(DeviceSettingKeys.VoicePackageUrlKey));

                            ChangeDetectionState(DetectionStates.WaitingForFaces);
                        }
                        break;

                    case DetectionStates.WaitingForFaces:
                        CurrentState.ApiRequestParameters = await ProcessCurrentVideoFrameAsync();

                        if (CurrentState.ApiRequestParameters != null)
                        {
                            ChangeDetectionState(DetectionStates.FaceDetectedOnDevice);
                        }
                        break;

                    case DetectionStates.FaceDetectedOnDevice:

                        //Should we play? MORE DESC REQUIRED
                        if (CurrentState.LastImageApiPush.AddMilliseconds(ApiIntervalMs) < DateTimeOffset.UtcNow
                            && CurrentState.TimeVideoWasStopped.AddMilliseconds(NumberMillSecsBeforeWePlayAgain) < DateTimeOffset.UtcNow)
                        {
                            //ThreadPoolTimer.CreateTimer(
                            //    new TimerElapsedHandler(HelloAudioHandler),
                            //    TimeSpan.FromMilliseconds(NumberMilliSecsToWaitForHello));
                            if (Settings.GetBool(DeviceSettingKeys.InteractiveKey))
                            {
                                // Check we're not already running a speech recognition
                                if (!IsSpeechRecognitionInProgress)
                                {
                                    // Kick off new speech recognizer
                                    await SayAsync("I'm listening, talk to me");
                                }
                            }
                            else
                            {
                                HelloAudio();
                            }

                            LogStatusMessage($"Sending faces to api", StatusSeverity.Info, false);

                            CurrentState.LastImageApiPush = DateTimeOffset.UtcNow;
                            CurrentState.FacesFoundByApi = await PostImageToApiAsync(CurrentState.ApiRequestParameters.Image);
                            
                            ChangeDetectionState(DetectionStates.ApiResponseReceived);
                        }

                        break;

                    case DetectionStates.ApiResponseReceived:

                        if (CurrentState.FacesFoundByApi != null && CurrentState.FacesFoundByApi.Any())
                        {
                            var firstFaceAge = CurrentState.FacesFoundByApi.FirstOrDefault().faceAttributes.age.ToString();
                            LogStatusMessage($"Face(s) detected. First face's age is {firstFaceAge}", StatusSeverity.Info, false);
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
                            // For time being use the interactive flag to determine whether to play narration
                            // If this is played here it will be detected by the speech recognition
                            if (!Settings.GetBool(DeviceSettingKeys.InteractiveKey))
                            {
                                LogStatusMessage("Starting playlist", StatusSeverity.Info);
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
                        LogStatusMessage($"Faces present: {CurrentState.FacesStillPresent} Speech Recognition active: {IsSpeechRecognitionInProgress}", StatusSeverity.Info, false);

                        //we dont have a face
                        if (!CurrentState.FacesStillPresent)
                        {
                            //TODO Refactor this out.
                            await Task.Delay(NumberMilliSecsForFacesToDisappear)
                                .ContinueWith((t =>
                                {
                                    CurrentState.FacesStillPresent = AreFacesStillPresent().Result;
                                    if (!CurrentState.FacesStillPresent)
                                    {
                                        LogStatusMessage($"Faces have gone for a few or more secs, stop the audio playback", StatusSeverity.Info, false);
                                        ChangeDetectionState(DetectionStates.WaitingForFaces);
                                        vp.Stop();
                                        CurrentState.TimeVideoWasStopped = DateTimeOffset.UtcNow;
                                        return;
                                    }
                                }
                                ));
                        }
                        break;

                    default:
                        ChangeDetectionState(DetectionStates.Idle);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogStatusMessage("Unable to process current frame. " + ex.ToString(), StatusSeverity.Error, false);
            }
            finally
            {
                // Check we're not already running a speech recognition
                if (!IsSpeechRecognitionInProgress)
                {
                    RunTimer();
                }
            }
        }

        private void HelloAudio()
        {
            LogStatusMessage("Starting introduction", StatusSeverity.Info, false);

            vp.PlayIntroduction(CurrentState.ApiRequestParameters.Faces.Count());
            //timer.Cancel();
        }


        private void LogStatusMessage(string message, StatusSeverity severity, bool say = false)
        {
            var ignored = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                while (StatusLog.Count > 100)
                {
                    StatusLog.RemoveAt(StatusLog.Count - 1);
                }

                StatusLog.Insert(0, new StatusMessage(message, severity));
            });

            if (say)
            {
                vpGenerated.Say(message);
            }
        }

        private async Task<List<Face>> PostImageToApiAsync(byte[] image)
        {
            try
            {
                var face = await Api.PostPatron(AppSettings, image);

                if (face != null)
                {
                    return face;
                }
                else
                {
                    vp.Stop();
                    LogStatusMessage("Exception posting to Patron api: Null response", StatusSeverity.Error, false);
                    return null;
                }
            }
            catch (Exception ex)
            {
                vp.Stop();
                LogStatusMessage("Exception posting to Patron api: " + ex.ToString(), StatusSeverity.Error, false);
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

                            LogStatusMessage($"Found face(s) on camera: {faces.Count}", StatusSeverity.Info, false);


                            return new ApiRequestParameters
                            {
                                Image = ms.ToArray(),
                                Faces = faces
                            };
                        }
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogStatusMessage("Unable to process current frame: " + ex.ToString(), StatusSeverity.Error, false);
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
                LogStatusMessage("Unable to process current frame: " + ex.ToString(), StatusSeverity.Error, false);
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
                    await StopSpeechRecognition();
                    CurrentState.State = newState;
                    break;
                case DetectionStates.Startup:
                    if (!await StartWebcamStreaming())
                    {
                        ChangeDetectionState(DetectionStates.Idle);
                        break;
                    }
                    var deviceId = Settings.GetString(DeviceSettingKeys.DeviceIdKey);
                    if (deviceId == null)
                    {
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
