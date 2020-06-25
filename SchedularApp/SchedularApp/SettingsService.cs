using Newtonsoft.Json;
using NuGet.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace SchedularApp
{
    public class LocalSettings
    {
        public string SchedulerStartTime { get; set; }
        public string SchedulerEndTime { get; set; }
        public string forceStop { get; set; }
    }

    public static class SettingsService
    {
        private const string SETTINGS_FILENAME = "localSettings.json";
        private static StorageFolder _settingsFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
        public async static Task<LocalSettings> LoadSettings()
        {
            try
            {
                StorageFile storageFile = await _settingsFolder.GetFileAsync(SETTINGS_FILENAME);
                if (storageFile == null) return null;

                string content = await FileIO.ReadTextAsync(storageFile);
                return JsonConvert.DeserializeObject<LocalSettings>(content);
            }
            catch
            { return null; }
        }

        public async static Task<bool> SaveSettings(LocalSettings data)
        {
            try
            {
                StorageFile file = await _settingsFolder.CreateFileAsync(SETTINGS_FILENAME, CreationCollisionOption.ReplaceExisting);
                string content = JsonConvert.SerializeObject(data);
                await FileIO.WriteTextAsync(file, content);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async static Task<bool> InitializeSettings()
        {
            try
            {
                var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/localSettings.json"));
                if (file != null)
                {
                    // Copy .json file to LocalFolder so we can read / write to it.
                    // No CollisionOption will default to Fail if file already exists,
                    // to copy every time the code is run, add NameCollisionOption.ReplaceExisting

                    await file.CopyAsync(ApplicationData.Current.LocalFolder, "localSettings.json", NameCollisionOption.ReplaceExisting);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
