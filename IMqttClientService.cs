using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace owfsmq
{
    public interface IMqttClientService
    {
        Task<IMqttClient> ConnectAsync(CancellationToken ct);
    }

    public class MqttClientService : IMqttClientService
    {
        private readonly IConfigProvider configProvider;
        private readonly ILogger<MqttClientService> log;

        public MqttClientService(IConfigProvider configProvider, ILogger<MqttClientService> log)
        {
            this.configProvider = configProvider;
            this.log = log;
        }
        public async Task<IMqttClient> ConnectAsync(CancellationToken ct)
        {
            var config = configProvider.GetConfig();
            var mqttClient = new MqttFactory().CreateMqttClient();
            var optionsBuilder = new MqttClientOptionsBuilder().WithTcpServer(config.MqttHost, config.MqttPort);
            if (!string.IsNullOrEmpty(config.MqttUserName))
                optionsBuilder = optionsBuilder.WithCredentials(config.MqttUserName, config.MqttPassword ?? "");

            IMqttClientOptions options = optionsBuilder.Build();

            mqttClient.UseDisconnectedHandler(async e =>
            {
                if (ct.IsCancellationRequested)
                    return;

                log.LogWarning($"MQTT Client disconnected ({e.Reason})");
                await Task.Delay(TimeSpan.FromSeconds(5));
                try
                {
                    await mqttClient.ConnectAsync(options, ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, $"MQTT Client failed to reconnect");
                }
            });

            log.LogInformation($"Connecting to Mqtt broker at {config.MqttHost}:{config.MqttPort}");
            var connectResult = await mqttClient.ConnectAsync(options, ct);
            log.LogInformation($"Connected: {connectResult.ResultCode}");
            return mqttClient;
        }


    }

}
