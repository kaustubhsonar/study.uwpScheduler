using System;
using Windows.Storage;
using System.Net.NetworkInformation;

namespace SchedularApp.Data
{
    public static class LocalDataStore
    {
        private static ApplicationDataContainer localSettings;

        public static void CreateLocalConfigurationSettings()
        {
            localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["SchedulerStartTime"] = "12:30:10";
            localSettings.Values["SchedulerEndTime"] = "13:30:10";
            localSettings.Values["BackgroundTaskEntryPoint"] = "RaspberryPiUwpHeadlessApp.StartupTask";
            localSettings.Values["BackgroundTaskName"] = "";
        }

        public static TimeSpan SchedulerStartTime => Convert.ToDateTime(localSettings.Values["SchedulerStartTime"]).TimeOfDay;
        public static TimeSpan SchedulerEndTime => Convert.ToDateTime(localSettings.Values["SchedulerEndTime"]).TimeOfDay;
    }
}
