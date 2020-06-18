using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;

namespace RaspberryPiUwpHeadlessApp.Recorders
{
    public sealed class StreamRecorder
    {
        private MediaCapture mediaCapture;
        private bool isRecording;
        private readonly int recordingTimeInMilliSeconds = (int)LocalDataStore.RecordingTimeInMilliSeconds;
        public int NumberOfMissedRecordings { get; private set; }

        public StreamRecorder()
        {
            isRecording = false;
            InitializeAudioSettings();
        }

        internal async Task<bool> Record()
        {
            if (isRecording)
            {
                Debug.WriteLine("\nCurrent recording is not yet finished,so skip this cycle");
                NumberOfMissedRecordings++;
                return isRecording;
            }

            var currentDate = DateTime.UtcNow;
            try
            {
                var recordProfileM4a = MediaEncodingProfile.CreateM4a(AudioEncodingQuality.Auto);
                var recordProfileWav = MediaEncodingProfile.CreateWav(AudioEncodingQuality.Auto);
                recordProfileWav.Audio = AudioEncodingProperties.CreatePcm(8000, 1, 16);
                InMemoryRandomAccessStream recordedStream = new InMemoryRandomAccessStream();
                await StartRecordingToStream(recordProfileWav, recordedStream);
                Thread.Sleep(recordingTimeInMilliSeconds);
                await StopRecording();
                //ToDo-Send it to Azure
                return isRecording;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("\n" + ex.Message);
                Cleanup();
                return isRecording;
            }
        }

        private async Task StartRecordingToStream(MediaEncodingProfile recordProfile, InMemoryRandomAccessStream memoryStream)
        {
            await mediaCapture.StartRecordToStreamAsync(recordProfile, memoryStream);
            isRecording = true;
            Debug.WriteLine("Audio recording in progress...");
        }

        private async Task StopRecording()
        {
            await mediaCapture.StopRecordAsync();
            isRecording = false;
            Debug.WriteLine("Stopped audio recording...");
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
                Debug.WriteLine("\nDevice successfully initialized for audio recording!");
                mediaCapture.Failed += MediaCapture_Failed;
                mediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitExceeded;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("\nUnable to initialize microphone for audio mode: " + ex.Message);
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
                Debug.WriteLine("\nMediaCaptureFailed: " + currentFailure.Message);

                if (isRecording)
                {
                    await mediaCapture.StopRecordAsync();
                    Debug.WriteLine("\nRecording Stopped");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("\n" + ex.Message);
            }
            finally
            {
                Debug.WriteLine("\nCheck if recorder is disconnected. Try re-launching the app");
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
                            Debug.WriteLine("\nStopping StartRecorder on exceeding max record duration");
                            await mediaCapture.StopRecordAsync();
                            isRecording = false;
                            if (mediaCapture.MediaCaptureSettings.StreamingCaptureMode == StreamingCaptureMode.Audio)
                            {
                                Debug.WriteLine("\nStopped record on exceeding max record duration: ");
                            }
                            else
                            {
                                //ToDo-
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("\n" + e.Message);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("\n" + e.Message);
            }
        }
    }
}