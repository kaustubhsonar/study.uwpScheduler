using System;
using Windows.Storage;
using System.Net.NetworkInformation;

namespace RaspberryPiUwpHeadlessApp
{
    public static class LocalDataStore
    {
        private static ApplicationDataContainer localSettings;

        public static void CreateLocalConfigurationSettings()
        {
            localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["DeviceId"] = GetUniqueDeviceId(); 
            localSettings.Values["AppRunningStartTime"] = "60000"; //DateTime
            localSettings.Values["AppRunningEndTime"] = "60000"; //DateTime
            localSettings.Values["RecordingIntervalInMinutes"] = 10;//Seconds
            localSettings.Values["RecordingTimeInMilliSeconds"] = 13000;
        }

        public static object DeviceId => localSettings.Values["DeviceId"];
        public static object RecordingIntervalInMinutes => localSettings.Values["RecordingIntervalInMinutes"];
        public static object RecordingTimeInMilliSeconds => localSettings.Values["RecordingTimeInMilliSeconds"];


        //public static T ReadSettings<T>(string key)
        //{
        //    return localSettings.Values.ContainsKey(key) ? (T)localSettings.Values[key] :
        //        throw new ArgumentNullException($"setting {key} does not exist in application Local settings");
        //}

        //ToDo-Read it from IoT hub
        private static Guid GetUniqueDeviceId()
        {
            var macAddress = GetMacAddress();
            var macId = new Guid(string.Concat("00000000-0000-0000-0000-", macAddress));
            return macId;
        }

        private static string GetMacAddress()
        {
            string macAddresses = "";
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Only consider Ethernet network interfaces, thereby ignoring any
                // loopback devices etc.
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                //if (nic.NetworkInterfaceType != NetworkInterfaceType.Ethernet) continue;
                if (nic.OperationalStatus == OperationalStatus.Up)
                {
                    macAddresses = nic.GetPhysicalAddress().ToString();
                    break;
                }
            }
            return macAddresses;
        }
    }
}