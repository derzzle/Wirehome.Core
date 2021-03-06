﻿using System;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;

namespace Wirehome.Core.Hardware.MQTT
{
    public class MqttTopicImporter
    {
        private readonly MqttImportTopicParameters _parameters;
        private readonly MqttService _mqttService;
        private readonly ILogger _logger;

        private IManagedMqttClient _mqttClient;

        public MqttTopicImporter(MqttImportTopicParameters parameters, MqttService mqttService, ILogger logger)
        {
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start()
        {
            var optionsBuilder = new ManagedMqttClientOptionsBuilder();
            optionsBuilder = optionsBuilder.WithClientOptions(
                o => o
                    .WithTcpServer(_parameters.Server, _parameters.Port)
                    .WithCredentials(_parameters.Username, _parameters.Password)
                    .WithClientId(_parameters.ClientId)
                    .WithTls(new MqttClientOptionsBuilderTlsParameters
                    {
                        UseTls = _parameters.UseTls
                    }));

            if (!string.IsNullOrEmpty(_parameters.ClientId))
            {
                optionsBuilder = optionsBuilder.WithClientOptions(o => o.WithClientId(_parameters.ClientId));
            }

            var options = optionsBuilder.Build();

            _mqttClient = new MqttFactory().CreateManagedMqttClient(new LoggerAdapter(_logger));
            _mqttClient.SubscribeAsync(_parameters.Topic, _parameters.QualityOfServiceLevel).GetAwaiter().GetResult();
            _mqttClient.UseApplicationMessageReceivedHandler(e => OnApplicationMessageReceived(e));
            _mqttClient.StartAsync(options).GetAwaiter().GetResult();
        }

        public void Stop()
        {
            _mqttClient?.StopAsync().GetAwaiter().GetResult();
            _mqttClient?.Dispose();
        }

        private void OnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            _mqttService.Publish(new MqttPublishParameters
            {
                Topic = e.ApplicationMessage.Topic,
                Payload = e.ApplicationMessage.Payload,
                QualityOfServiceLevel = e.ApplicationMessage.QualityOfServiceLevel,
                Retain = e.ApplicationMessage.Retain
            });
        }
    }
}
