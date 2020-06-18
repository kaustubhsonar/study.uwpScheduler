using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;

namespace RaspberryPiUwpHeadlessApp.ServiceConsumer
{
    internal sealed class IotHubClient
    {
        //private readonly string deviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONN_STRING");
        private readonly string deviceConnectionString = "HostName=PlayboxHub1.azure-devices.net;DeviceId=PlayboxDevice1;SharedAccessKey=UmikFys4RBx4Quzh7vC7Bk5TjZLHkWxw65Mb7WDLNQU=";

        private readonly TransportType transportType = TransportType.Amqp;
        private readonly ILoggingChannel loggingChannel;

        internal IotHubClient(ILoggingChannel loggingChannel)
        {
            this.loggingChannel = loggingChannel;

            if (string.IsNullOrEmpty(deviceConnectionString))
            {
                loggingChannel.LogMessage("Please provide a device connection string", LoggingLevel.Error);
            }
        }
        internal async Task SendToHub(DeviceTelemetry deviceTelemetry)
        {
            try
            {
                using (DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, transportType))
                {
                    //await deviceClient.OpenAsync();
                    //await UpdateTwin(deviceClient);
                    await SendTelemetry(deviceClient, deviceTelemetry);
                }
            }
            catch (Exception)
            {
                loggingChannel.LogMessage("Exception occured while creating device client", LoggingLevel.Error);
            }
        }

        private static async Task SendTelemetry(DeviceClient deviceClient, DeviceTelemetry deviceTelemetry)
        {
            var payload = JsonConvert.SerializeObject(deviceTelemetry);
            var message = new Message(Encoding.ASCII.GetBytes(payload));
            await deviceClient.SendEventAsync(message);
        }

        private static async Task UpdateTwin(DeviceClient deviceClient)
        {
            //ToDo-Read from the device and fill
            var twinProperties = new TwinCollection();
            twinProperties["connection.type"] = "wi-fi";
            twinProperties["connectionStrength"] = "full";
            await deviceClient.UpdateReportedPropertiesAsync(twinProperties);
        }
    }
}
