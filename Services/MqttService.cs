using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;

namespace Login.Services
{
public class MqttService : IMqttService, IHostedService, IDisposable
{
    private readonly IMqttClient _mqttClient;
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
        _mqttClient = new MqttFactory().CreateMqttClient();
        ConfigureMqttClient();
    }

    private void ConfigureMqttClient()
    {
        _mqttClient.ConnectedAsync += HandleConnectedAsync;
        _mqttClient.DisconnectedAsync += HandleDisconnectedAsync;
        _mqttClient.ApplicationMessageReceivedAsync += HandleMessageReceivedAsync;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ConnectToBrokerAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _mqttClient.DisconnectAsync();
        Dispose();
    }

    private async Task ConnectToBrokerAsync()
    {
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_config.Host, _config.Port)
            .WithCredentials(_config.Username, _config.Password)
            .WithClientId($"{_config.ClientId}_{Guid.NewGuid()}")
            .WithCleanSession()
            .Build();

        await _mqttClient.ConnectAsync(options, CancellationToken.None);
    }

    private async Task HandleConnectedAsync(MqttClientConnectedEventArgs e)
    {
        _logger.LogInformation("Connected to MQTT broker");
        await SubscribeAsync("user/+/notifications");
    }

    private async Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs e)
    {
        _logger.LogWarning("Disconnected from MQTT broker. Attempting reconnect...");
        await Task.Delay(TimeSpan.FromSeconds(5));
        await ConnectToBrokerAsync();
    }

    private async Task HandleMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            
            if (topic.StartsWith("user/") && topic.EndsWith("/notifications"))
            {
                var userId = topic.Split('/')[1];
                await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", payload);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MQTT message");
        }
    }

    public async Task PublishAsync(string topic, string message)
    {
        var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(message))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _mqttClient.PublishAsync(mqttMessage);
    }

    public async Task SubscribeAsync(string topic)
    {
        await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build());
    }

    public void Dispose()
    {
        _mqttClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}
}