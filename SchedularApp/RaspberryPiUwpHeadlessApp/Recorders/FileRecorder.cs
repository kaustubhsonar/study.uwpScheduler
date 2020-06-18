using RaspberryPiUwpHeadlessApp.ServiceConsumer;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Diagnostics;
using Windows.Media.Capture;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;

namespace RaspberryPiUwpHeadlessApp.Recorders
{
    //ToDO-Use DI
    public sealed class FileRecorder
    { 
        private MediaCapture mediaCapture;
        private const string AudioFilePrefix = "Track";
        private bool isRecording;
        private readonly Guid deviceId = (Guid)LocalDataStore.DeviceId;
        private readonly int recordingTimeInMilliSeconds = (int)LocalDataStore.RecordingTimeInMilliSeconds;
        public int NumberOfMissedRecordings { get; private set; }
        private readonly ILoggingChannel loggingChannel;
        private readonly IotHubClient iotHubClient;

        public FileRecorder(ILoggingChannel loggingChannel)
        {
            isRecording = false;
            InitializeAudioSettings();
            this.loggingChannel = loggingChannel;
            iotHubClient = new IotHubClient(loggingChannel);
        }

        //ToDo-Make it public via an interface
        internal async Task<bool> Record()
        {
            if (isRecording)
            {
                loggingChannel.LogMessage("\nCurrent recording is not yet finished,so skip this cycle");
                NumberOfMissedRecordings++;
                return isRecording;
            }

            DateTime currentDate = DateTime.UtcNow;
            var audioFileName = AudioFilePrefix + "_" + currentDate.ToString("yyyyMMddHHmmssfff") + ".m4a";
            try
            {
                //FindCodecsOnThisDevice();//ToDo-enable in debug
                var stopwatch = Stopwatch.StartNew();
                loggingChannel.LogMessage("\nStarted audio recording");
                var audioFile = await KnownFolders.VideosLibrary.CreateFileAsync(audioFileName, CreationCollisionOption.GenerateUniqueName);
                var recordProfileM4a = MediaEncodingProfile.CreateM4a(AudioEncodingQuality.Low);
                recordProfileM4a.Audio = AudioEncodingProperties.CreatePcm(8000, 1, 16);
                loggingChannel.LogMessage("\nAudio storage file preparation successful");
                await mediaCapture.StartRecordToStorageFileAsync(recordProfileM4a, audioFile);
                isRecording = true;
                loggingChannel.LogMessage("\nAudio recording in progress...");
                Thread.Sleep(recordingTimeInMilliSeconds);
                await StopRecording();
                stopwatch.Stop();
                loggingChannel.LogMessage("Recording logic took " + stopwatch.Elapsed.Seconds.ToString() + " seconds");
                SendToIotHub(currentDate, audioFile);
                return isRecording;
            }
            catch (Exception ex)
            {
                loggingChannel.LogMessage("\n" + ex.Message);
                Cleanup();
                return isRecording;
            }
        }

        private async void FindCodecsOnThisDevice()
        {
            string codecs = "";
            var codecQuery = new CodecQuery();
            var results =   await codecQuery.FindAllAsync(CodecKind.Audio, CodecCategory.Encoder, "");

            foreach (var codecInfo in results)
            {
                codecs+= "============================================================\n";
                codecs+= string.Format("Codec: {0}\n", codecInfo.DisplayName);
                codecs+= string.Format("Kind: {0}\n", codecInfo.Kind.ToString());
                codecs+= string.Format("Category: {0}\n", codecInfo.Category.ToString());
                codecs+= string.Format("Trusted: {0}\n", codecInfo.IsTrusted.ToString());

                foreach (string subType in codecInfo.Subtypes)
                {
                    codecs+= string.Format("   Subtype: {0}\n", subType);
                }
            }

            Debug.WriteLine(codecs);
        }

        private async Task StopRecording()
        {
            await mediaCapture.StopRecordAsync();
            isRecording = false;
            loggingChannel.LogMessage("Stopped audio recording");
        }
        private void SendToIotHub(DateTime recordingTime, StorageFile audioFile)
        {
            var deviceTelemetry = new DeviceTelemetry { /*RecordedTime = recordingTime,*/ RecordedStream = GetBytesAsync(audioFile).Result };
            iotHubClient.SendToHub(deviceTelemetry).GetAwaiter().GetResult();
        }

        private static async Task<byte[]> GetBytesAsync(StorageFile file)
        {
            byte[] fileBytes = null;
            if (file == null) return null;
            using (var stream = await file.OpenReadAsync())
            {
                fileBytes = new byte[stream.Size];
                using (var reader = new DataReader(stream))
                {
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(fileBytes);
                }
            }
            return fileBytes;
        }

        //ToDo-Return status
        private async void InitializeAudioSettings()
        {
            try
            {
                if (mediaCapture != null)
                {
                    if (!isRecording) return;
                    await mediaCapture.StopRecordAsync();
                    isRecording = false;
                    mediaCapture.Dispose();
                    mediaCapture = null;
                    return;
                }

                mediaCapture = new MediaCapture();
                var settings = new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Audio,
                    MediaCategory = MediaCategory.Other,
                    AudioProcessing = Windows.Media.AudioProcessing.Default
                };
                await mediaCapture.InitializeAsync(settings);
                mediaCapture.AudioDeviceController.VolumePercent = 100;
                // Set callbacks for failure and recording limit exceeded
                loggingChannel.LogMessage("\nDevice successfully initialized for audio recording!");
                mediaCapture.Failed += MediaCapture_Failed;
                mediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitExceeded;
            }
            catch (Exception ex)
            {
                loggingChannel.LogMessage("\nUnable to initialize microphone for audio mode: " + ex.Message);
            }
        }

        private async void Cleanup()
        {
            if (mediaCapture != null)
            {
                if (isRecording)
                {
                    await mediaCapture.StopRecordAsync();
                    isRecording = false;
                }

                mediaCapture.Dispose();
                mediaCapture = null;
            }
        }

        private async void MediaCapture_Failed(MediaCapture currentCaptureObject,
            MediaCaptureFailedEventArgs currentFailure)
        {
            try
            {
                loggingChannel.LogMessage("\nMediaCaptureFailed: " + currentFailure.Message);

                if (isRecording)
                {
                    await mediaCapture.StopRecordAsync();
                    loggingChannel.LogMessage("\nRecording Stopped");
                }
            }
            catch (Exception ex)
            {
                loggingChannel.LogMessage("\n" + ex.Message);
            }
            finally
            {
                loggingChannel.LogMessage("\nCheck if recorder is disconnected. Try re-launching the app");
            }
        }

        private async void MediaCapture_RecordLimitExceeded(MediaCapture currentCaptureObject)
        {
            try
            {
                if (isRecording)
                {
                    {
                        try
                        {
                            loggingChannel.LogMessage("\nStopping StartRecorder on exceeding max record duration");
                            await mediaCapture.StopRecordAsync();
                            isRecording = false;
                            if (mediaCapture.MediaCaptureSettings.StreamingCaptureMode == StreamingCaptureMode.Audio)
                            {
                                loggingChannel.LogMessage("\nStopped record on exceeding max record duration ");
                            }
                            else
                            {
                                //ToDo-
                            }
                        }
                        catch (Exception e)
                        {
                            loggingChannel.LogMessage("\n" + e.Message);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                loggingChannel.LogMessage("\n" + e.Message);
            }
        }
    }
}