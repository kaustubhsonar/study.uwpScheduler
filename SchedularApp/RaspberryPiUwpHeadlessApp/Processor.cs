using System;
using System.Diagnostics;
using Windows.System.Threading;
using Windows.Foundation.Diagnostics;
using RaspberryPiUwpHeadlessApp.Recorders;
using System.Threading.Tasks;

namespace RaspberryPiUwpHeadlessApp
{
    //ToDo-Move code to respective classes-Adhere to SingleResponsibility principle
    public sealed class Processor
    {
        private ThreadPoolTimer timer;
        private readonly int recordingTimerIntervalInMinutes = (int)LocalDataStore.RecordingIntervalInMinutes;
        private StreamRecorder streamRecorder;
        private FileRecorder fileRecorder;
        private readonly ILoggingChannel loggingChannel;

        public Processor(ILoggingChannel loggingChannel)
        {
            this.loggingChannel = loggingChannel;
        }

        public void Process()
        {
            //streamRecorder = new StreamRecorder();
            //fileRecorder = new FileRecorder(loggingChannel);
            loggingChannel.LogMessage("About to start the timer", LoggingLevel.Information);
            Debug.WriteLine("About to start the timer");
            timer = ThreadPoolTimer.CreatePeriodicTimer(RecordingTimerHandler,
                TimeSpan.FromSeconds(recordingTimerIntervalInMinutes));
        }

        private async void RecordingTimerHandler(ThreadPoolTimer threadPoolTimer)
        {
            //await streamRecorder.RecordToStream();
            //await fileRecorder.Record();
            await Task.Run(() =>
            {
                Debug.WriteLine("Recording started... Time- " + DateTime.Now.ToString());
                System.Threading.Thread.Sleep(5000);
                Debug.WriteLine("Recording completed... Time- " + DateTime.Now.ToString());
            });
            //StopRecorder(recorder);
        }

        private void StopRecorder(StreamRecorder recorder)
        {
            timer.Cancel();
            Debug.WriteLine("\nRecorder stopped");
            var missedRecordingsCount = recorder.NumberOfMissedRecordings;
            Debug.WriteLine("Number of missed recordings:" + missedRecordingsCount);
        }
    }
}