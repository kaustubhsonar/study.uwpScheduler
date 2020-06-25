using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using Windows.ApplicationModel.Background;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml;


namespace SchedularApp
{
    public class uwpScheduler
    {
        public uwpScheduler()
        {
            DispatcherTimerSetup();
        }
        public TimeSpan SchedularTime = Convert.ToDateTime(DateTime.Now.AddMinutes(1).TimeOfDay.ToString()).TimeOfDay; //xxx
        public TimeSpan SchedularTimeEnd = Convert.ToDateTime(DateTime.Now.AddMinutes(2).TimeOfDay.ToString()).TimeOfDay; //xxx
      
        DispatcherTimer dispatcherTimer;
        DateTimeOffset startTime;
        DateTimeOffset lastTime;
        public int TickSeconds = 10;
        public bool schedulerRunning = false;
        public void DispatcherTimerSetup()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, TickSeconds);
            startTime = DateTimeOffset.Now;
            lastTime = startTime;
            dispatcherTimer.Start();
        }
        void dispatcherTimer_Tick(object sender, object e)
        {
            DateTimeOffset time = DateTimeOffset.Now;
            TimeSpan span = time - lastTime;
            lastTime = time;
            //if (SchedularTime.Hours == System.DateTime.Now.Hour && SchedularTime.Minutes == System.DateTime.Now.Minute && schedulerRunning == false)
            if (IsScheduleTime())
            {
                if (!IsSchedulerRunning())
                    StartSchedular();
            }
            else
            {
                if (IsSchedulerRunning())
                    StopSchedular();
            }

        }

        private bool IsSchedulerRunning()
        {
            //Debug.WriteLine("IsSchedulerRunning at " + DateTime.UtcNow.ToString());
            return schedulerRunning;
        }

        private bool IsScheduleTime()
        {
           // Debug.WriteLine("IsScheduleTime at " + DateTime.UtcNow.ToString());
            if (DateTime.Now.TimeOfDay > SchedularTime && DateTime.Now.TimeOfDay < SchedularTimeEnd)
                return true;
            else
                return false;
        }

        ApplicationTrigger trigger = null;
        public void StartSchedular()
        {
            Debug.WriteLine("StartSchedular at " + DateTime.UtcNow.ToString());
            schedulerRunning = true;
            trigger = new ApplicationTrigger();
            var task = BackgroundTaskSample.RegisterBackgroundTask(BackgroundTaskSample.SampleBackgroundTaskEntryPoint,
                                                                  BackgroundTaskSample.ApplicationTriggerTaskName,
                                                                  trigger,
                                                                  null);

            StartBackgroundTask();

        }

        private async void StartBackgroundTask()
        {
            // Reset the completion status
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values.Remove(BackgroundTaskSample.ApplicationTriggerTaskName);

            //Signal the ApplicationTrigger
            var result = await trigger.RequestAsync();
            BackgroundTaskSample.ApplicationTriggerTaskResult = "Signal result: " + result.ToString();
            schedulerRunning = true;
            //UpdateUI();
        }

        private void StopSchedular()
        {
            Debug.WriteLine("StopSchedular at " + DateTime.UtcNow.ToString());
            BackgroundTaskSample.UnregisterBackgroundTasks(BackgroundTaskSample.ApplicationTriggerTaskName);
            BackgroundTaskSample.ApplicationTriggerTaskResult = "";
            schedulerRunning = false;
            SchedularTime = Convert.ToDateTime(DateTime.Now.AddMinutes(2).TimeOfDay.ToString()).TimeOfDay; //xxx
            SchedularTimeEnd = Convert.ToDateTime(DateTime.Now.AddMinutes(3).TimeOfDay.ToString()).TimeOfDay; //xxx
            Debug.WriteLine("New Schedule time at " + SchedularTime.ToString());
        }



    }
}