using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace owfsmq
{  
    public sealed class Service
    {
        private readonly string Valueregex = @"<TD><B>{0}<\/B><\/TD><TD>(?<value>\d+(\.\d+)?)<\/TD>";

        private readonly CancellationTokenSource cts;
        private readonly Config config;
        private readonly ILogger<Service> log;
        private readonly IMqttClientService mqttClientService;

        public Service(IConfigProvider configProvider, IMqttClientService mqttClientService, ILogger<Service> log)
        {
            this.mqttClientService = mqttClientService;
            try
            {
                this.config = configProvider.GetConfig();
            }
            catch (Exception ex)
            {
                log.LogCritical(ex.Message, "Failed to load configuration");
                log.LogDebug(ex.StackTrace);
            }
            this.log = log;
            cts = new CancellationTokenSource();
        }

        public void Stop()
        {
            log.LogInformation("Shutting down");
            cts.Cancel();
        }
        
        public async Task Run()
        {
            if (config == null)
                return;

            var mqttClient = await mqttClientService.ConnectAsync(cts.Token);

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    int updatedDevices = await Update(mqttClient);
                    await Task.Delay(config.IntervalSeconds * 1000, cts.Token);
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error in service loop");
                }
            }
           
            log.LogInformation("Done.");
        }    

        private async Task<int> Update(IMqttClient mqttClient)
        {
            using (log.BeginScope("[updating]"))
            {
                int updated = 0;

                foreach (var deviceGroup in config.Devices.GroupBy(d => d.DeviceId))
                {
                    if (cts.IsCancellationRequested)
                        break;

                    try
                    {
                        Dictionary<string, double> deviceMeasurements = await ReadDeviceValues(deviceGroup.Key, deviceGroup.Select(d => d.Measurement));

                        if (cts.IsCancellationRequested)
                            return updated;

                        log.LogInformation($"Publishing {deviceMeasurements.Count} measurements for device '{deviceGroup.Key}'");
                        
                        foreach (DeviceConfig device in deviceGroup)
                        {
                            if (!deviceMeasurements.TryGetValue(device.Measurement, out double value))
                                continue;

                            var message = new MqttApplicationMessageBuilder()
                               .WithTopic($"{device.Topic}")
                               .WithPayload(value.ToString(NumberFormatInfo.InvariantInfo))
                               .WithQualityOfServiceLevel(config.MqttQoS)
                               .Build();

                            log.LogDebug($"Publishing value {value} for device '{deviceGroup.Key}' mesurement '{device.Measurement}' to mqtt topic '{device.Topic}'");

                            await mqttClient.PublishAsync(message, cts.Token);
                        }
                        updated += deviceMeasurements.Count;
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, $"Error updating device '{deviceGroup.Key}'");
                    }
                }
                return updated;
            }
        }

        private async Task<Dictionary<string, double>> ReadDeviceValues(string deviceId, IEnumerable<string> measurements)
        {
            HttpClient client = new();
            Dictionary<string, double> deviceMeasurements = new Dictionary<string, double>();
            
            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{config.OwHttpHost}/{deviceId}"), cts.Token);
            
            if (cts.IsCancellationRequested)
                return null;

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new InvalidOperationException("Bad status from OWFS: " + response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            foreach (var measurement in measurements)
            {
                var match = new Regex(string.Format(Valueregex, measurement)).Match(body);
                if (match.Success)
                {
                    string valueStr = match.Groups["value"].Value;
                    if (!double.TryParse(valueStr, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out double v))
                    {
                        log.LogWarning($"Malformed data '{valueStr}' for '{deviceId}/{measurement}'");
                        continue;
                    }
                    deviceMeasurements[measurement] = v;
                }
                else
                {
                    log.LogWarning($"Found no measurement '{measurement}' on device response from device '{deviceId}'");
                }
            }

            return deviceMeasurements;
        }
    }
}
