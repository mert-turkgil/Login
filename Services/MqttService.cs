using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;
using Login.Services;
using Login.Hubs;

namespace Login.Services
{
    public class MqttService : IMqttService, IHostedService, IDisposable
    {
        private readonly IMqttClient _mqttClient;
        private readonly IMqttClientOptions _mqttOptions;
        private readonly MqttConfig _config;
        private readonly ILogger<MqttService> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        public MqttService(
            IOptions<MqttConfig> config,
            ILogger<MqttService> logger,
            IHubContext<NotificationHub> hubContext)
        {
            _config = config.Value;
            _logger = logger;
            _hubContext = hubContext;

            // Create the MQTT client using the factory.
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            // Build the MQTT client options.
            _mqttOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_config.Host, _config.Port)
                .WithCredentials(_config.Username, _config.Password)
                .WithClientId($"{_config.ClientId}_{Guid.NewGuid()}")
                .WithCleanSession()
                .Build();

            ConfigureMqttClient();
        }

        private void ConfigureMqttClient()
        {
            // Set up the connected handler.
            _mqttClient.UseConnectedHandler(async e =>
            {
                _logger.LogInformation("Connected to MQTT broker.");

                // Subscribe to topics for room numbers 1 to 12.
                for (int room = 1; room <= 12; room++)
                {
                    string statusTopic = $"ciceklisogukhavadeposu/control_room/room{room}/status";
                    string tempTopic = $"ciceklisogukhavadeposu/control_room/room{room}/temp";

                    // Use the simple SubscribeAsync overload (topic, QoS)
                    await _mqttClient.SubscribeAsync(statusTopic, MqttQualityOfServiceLevel.AtLeastOnce);
                    await _mqttClient.SubscribeAsync(tempTopic, MqttQualityOfServiceLevel.AtLeastOnce);

                    _logger.LogInformation($"Subscribed to: {statusTopic} and {tempTopic}");
                }
            });

            // Set up the disconnected handler.
            _mqttClient.UseDisconnectedHandler(async e =>
            {
                _logger.LogWarning("Disconnected from MQTT broker. Attempting reconnect...");
                await Task.Delay(TimeSpan.FromSeconds(5));
                await ConnectAsync(CancellationToken.None);
            });

            // Set up the message received handler.
            _mqttClient.UseApplicationMessageReceivedHandler(async e =>
            {
                try
                {
                    var topic = e.ApplicationMessage.Topic;
                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    _logger.LogInformation($"Message received on topic {topic}: {payload}");

                    // Broadcast the message to all SignalR clients.
                    await _hubContext.Clients.All.SendAsync("ReceiveNotification", topic, payload);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing MQTT message");
                }
            });
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (!_mqttClient.IsConnected)
            {
                await _mqttClient.ConnectAsync(_mqttOptions, cancellationToken);
            }
        }

        public async Task PublishAsync(string topic, string message)
        {
            if (!_mqttClient.IsConnected)
            {
                await ConnectAsync(CancellationToken.None);
            }

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(message))
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient.PublishAsync(mqttMessage, CancellationToken.None);
            _logger.LogInformation($"Published message to topic '{topic}': {message}");
        }

        public async Task SubscribeAsync(string topic)
        {
            // Subscribe using the simpler overload available in v5.
            await _mqttClient.SubscribeAsync(topic, MqttQualityOfServiceLevel.AtLeastOnce);
        }

        // IHostedService implementation.
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await ConnectAsync(cancellationToken);
        }

        public void Dispose()
        {
            _mqttClient?.Dispose();
            GC.SuppressFinalize(this);
        }
        public async Task ConnectAsync()
        {
            if (!_mqttClient.IsConnected)
            {
                await _mqttClient.ConnectAsync(_mqttOptions);
            }
        }
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync();
            }
        }

    }
}
