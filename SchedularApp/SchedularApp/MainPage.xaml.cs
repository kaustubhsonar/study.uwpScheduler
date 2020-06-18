using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Storage.AccessCache;
using System.Diagnostics;
using Windows.ApplicationModel;
using System.Runtime.Serialization.Json;
using Windows.Data.Json;
using System;
using Windows.ApplicationModel.Background;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SchedularApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public System.TimeSpan SchedularTime;

        ApplicationTrigger trigger = null;

        public MainPage()
        {
            this.InitializeComponent();
            //ReadConfiguration().GetAwaiter();
            //DispatcherTimerSetup();
            // RegisterBackgroundTask();
            trigger = new ApplicationTrigger();
        }

        private void RegisterBackgroundTask(object sender, RoutedEventArgs e)
        {
            //trigger = new ApplicationTrigger();
            var task = BackgroundTaskSample.RegisterBackgroundTask(BackgroundTaskSample.SampleBackgroundTaskEntryPoint,
                                                                   BackgroundTaskSample.ApplicationTriggerTaskName,
                                                                   trigger,
                                                                   null);
            AttachProgressAndCompletedHandlers(task);
            // UpdateUI();
        }

        /// <summary>
        /// Unregister a ApplicationTriggerTask.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnregisterBackgroundTask(object sender, RoutedEventArgs e)
        {
            BackgroundTaskSample.UnregisterBackgroundTasks(BackgroundTaskSample.ApplicationTriggerTaskName);
            BackgroundTaskSample.ApplicationTriggerTaskResult = "";
            //UpdateUI();
        }

        private async void SignalBackgroundTask(object sender, RoutedEventArgs e)
        {
            // Reset the completion status
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values.Remove(BackgroundTaskSample.ApplicationTriggerTaskName);

            //Signal the ApplicationTrigger
            var result = await trigger.RequestAsync();
            BackgroundTaskSample.ApplicationTriggerTaskResult = "Signal result: " + result.ToString();
            //UpdateUI();
        }

        /// <summary>
        /// Attach progress and completed handers to a background task.
        /// </summary>
        /// <param name="task">The task to attach progress and completed handlers to.</param>
        private void AttachProgressAndCompletedHandlers(IBackgroundTaskRegistration task)
        {
            task.Progress += new BackgroundTaskProgressEventHandler(OnProgress);
            task.Completed += new BackgroundTaskCompletedEventHandler(OnCompleted);
        }

        /// <summary>
        /// Handle background task progress.
        /// </summary>
        /// <param name="task">The task that is reporting progress.</param>
        /// <param name="e">Arguments of the progress report.</param>
        private void OnProgress(IBackgroundTaskRegistration task, BackgroundTaskProgressEventArgs args)
        {
            //var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            //{
            //    var progress = "Progress: " + args.Progress + "%";
            //    BackgroundTaskSample.ApplicationTriggerTaskProgress = progress;
            //    UpdateUI();
            //});
        }

        /// <summary>
        /// Handle background task completion.
        /// </summary>
        /// <param name="task">The task that is reporting completion.</param>
        /// <param name="e">Arguments of the completion report.</param>
        private void OnCompleted(IBackgroundTaskRegistration task, BackgroundTaskCompletedEventArgs args)
        {
            //UpdateUI();
        }



        #region schedulerCode
        public async Task ReadConfiguration()
        {
            try
            {
                // C: \Users\sonarkau\AppData\Local\Packages\6368fb79 - 061c - 4887 - 95ec - 8e25c02b0940_cags7ajrabnnw\LocalState                
                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                Windows.Storage.StorageFile Config = await storageFolder.CreateFileAsync("appSettings.json", CreationCollisionOption.OpenIfExists);
                var SetConfiguration = await Windows.Storage.FileIO.ReadTextAsync(Config);
                if (SetConfiguration == "")
                {
                    await Windows.Storage.FileIO.AppendTextAsync(Config, "{ \"SchedularStartTime\":\"" + DateTime.Now.AddMinutes(2).TimeOfDay.ToString() + "\" }" + Environment.NewLine);
                }
                var ConfigSchedularStartTime = await Windows.Storage.FileIO.ReadTextAsync(Config);
                JsonValue jsonValue = JsonValue.Parse(ConfigSchedularStartTime);
                String SchedularStartTime = jsonValue.GetObject().GetNamedString("SchedularStartTime");
                DateTime sDate = Convert.ToDateTime(SchedularStartTime);
                //SchedularTime = Convert.ToDateTime(SchedularStartTime).TimeOfDay;
                SchedularTime = Convert.ToDateTime(DateTime.Now.AddMinutes(1).TimeOfDay.ToString()).TimeOfDay; //xxx
                await saveToLog("Configuration Loaded");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        DispatcherTimer dispatcherTimer;
        DateTimeOffset startTime;
        DateTimeOffset lastTime;
        DateTimeOffset stopTime;
        int timesTicked = 1;
        int timesToTick = 1000;
        public int TickSeconds = 30;
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
            saveToLog("Times ticked on:" + DateTime.Now.ToString() + " Ticked count:" + timesTicked).GetAwaiter();
            DateTimeOffset time = DateTimeOffset.Now;
            TimeSpan span = time - lastTime;
            lastTime = time;
            if (SchedularTime.Hours == System.DateTime.Now.Hour && SchedularTime.Minutes == System.DateTime.Now.Minute && schedulerRunning == false)
            {
                StartSchedular();
            }
            timesTicked++;
            if (timesTicked > timesToTick)
            {
                stopTime = time;
                dispatcherTimer.Stop();
                span = stopTime - startTime;
            }
        }
        public void StartSchedular()
        {
            saveToLog("Schedular started on:" + DateTime.Now.ToString()).GetAwaiter();
            schedulerRunning = true;
            dispatcherTimer.Stop();
            dispatcherTimer.Start();
            schedulerRunning = false;
        }
        public async Task saveToLog(String logData)
        {
            try
            {
                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                Windows.Storage.StorageFile LogFile = await storageFolder.CreateFileAsync("Log" + System.DateTime.Now.GetDateTimeFormats()[5].ToString() + ".txt", CreationCollisionOption.OpenIfExists);
                await Windows.Storage.FileIO.AppendTextAsync(LogFile, logData + Environment.NewLine);
            }
            catch (Exception EX)
            {
                EX.ToString();
                throw;
            }
        }
        #endregion

    }
}
