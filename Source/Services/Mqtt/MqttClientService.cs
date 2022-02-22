﻿using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Internal;
using MQTTnetApp.Pages.Connection;
using MQTTnetApp.Pages.Publish;
using MQTTnetApp.Pages.Subscriptions;

namespace MQTTnetApp.Services.Mqtt;

public sealed class MqttClientService
{
    readonly AsyncEvent<MqttApplicationMessageReceivedEventArgs> _applicationMessageReceivedEvent = new();
    readonly List<Action<InspectMqttPacketEventArgs>> _messageInspectors = new();
    readonly MqttNetEventLogger _mqttNetEventLogger = new();

    MqttClient? _mqttClient;

    public MqttClientService()
    {
        _mqttNetEventLogger.LogMessagePublished += OnLogMessagePublished;
    }

    public event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceived
    {
        add => _applicationMessageReceivedEvent.AddHandler(value);
        remove => _applicationMessageReceivedEvent.RemoveHandler(value);
    }

    public event Action<MqttNetLogMessagePublishedEventArgs>? LogMessagePublished;

    public bool IsConnected => _mqttClient?.IsConnected == true;

    public async Task<MqttClientConnectResult> Connect(ConnectionItemViewModel item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (_mqttClient != null)
        {
            await _mqttClient.DisconnectAsync();
            _mqttClient.Dispose();
        }

        _mqttClient = new MqttFactory(_mqttNetEventLogger).CreateMqttClient();

        var clientOptionsBuilder = new MqttClientOptionsBuilder().WithCommunicationTimeout(TimeSpan.FromSeconds(item.ServerOptions.CommunicationTimeout))
            .WithProtocolVersion(item.ServerOptions.SelectedProtocolVersion.Value)
            .WithClientId(item.SessionOptions.ClientId)
            .WithCleanSession(item.SessionOptions.CleanSession)
            .WithCredentials(item.SessionOptions.UserName, item.SessionOptions.Password)
            .WithRequestProblemInformation(item.SessionOptions.RequestProblemInformation)
            .WithRequestResponseInformation(item.SessionOptions.RequestResponseInformation)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(item.SessionOptions.KeepAliveInterval));

        if (item.SessionOptions.SessionExpiryInterval > 0)
        {
            clientOptionsBuilder.WithSessionExpiryInterval((uint)item.SessionOptions.SessionExpiryInterval);
        }

        if (!string.IsNullOrEmpty(item.SessionOptions.AuthenticationMethod))
        {
            clientOptionsBuilder.WithAuthentication(item.SessionOptions.AuthenticationMethod, Convert.FromBase64String(item.SessionOptions.AuthenticationData));
        }

        if (item.ServerOptions.SelectedTransport?.Value == Transport.TCP)
        {
            clientOptionsBuilder.WithTcpServer(item.ServerOptions.Host, item.ServerOptions.Port);
        }
        else
        {
            clientOptionsBuilder.WithWebSocketServer(item.ServerOptions.Host);
        }

        if (item.ServerOptions.SelectedTlsVersion?.Value != SslProtocols.None)
        {
            clientOptionsBuilder.WithTls(o =>
            {
                o.SslProtocol = item.ServerOptions.SelectedTlsVersion.Value;
            });
        }

        _mqttClient.ApplicationMessageReceivedAsync += OnApplicationMessageReceived;
        _mqttClient.InspectPackage += OnInspectPackage;
        _mqttClient.DisconnectedAsync += OnDisconnected;

        var result = await _mqttClient.ConnectAsync(clientOptionsBuilder.Build());

        return result;
    }

    public Task Disconnect()
    {
        ThrowIfNotConnected();

        return _mqttClient.DisconnectAsync();
    }

    public Task<MqttClientPublishResult> Publish(PublishItemViewModel item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        ThrowIfNotConnected();

        var applicationMessageBuilder = new MqttApplicationMessageBuilder().WithTopic(item.Topic)
            .WithQualityOfServiceLevel(item.QualityOfServiceLevel.Value)
            .WithRetainFlag(item.Retain)
            .WithMessageExpiryInterval(item.MessageExpiryInterval)
            .WithContentType(item.ContentType)
            .WithPayloadFormatIndicator(item.PayloadFormatIndicator.ToPayloadFormatIndicator())
            .WithPayload(item.PayloadFormatIndicator.ToPayload(item.Payload))
            .WithSubscriptionIdentifier(item.SubscriptionIdentifier)
            .WithResponseTopic(item.ResponseTopic);

        if (item.TopicAlias > 0)
        {
            applicationMessageBuilder.WithTopicAlias(item.TopicAlias);
        }

        foreach (var userProperty in item.UserProperties.Items)
        {
            if (!string.IsNullOrEmpty(userProperty.Name))
            {
                applicationMessageBuilder.WithUserProperty(userProperty.Name, userProperty.Value);
            }
        }

        return _mqttClient!.PublishAsync(applicationMessageBuilder.Build());
    }

    public void RegisterMessageInspectorHandler(Action<InspectMqttPacketEventArgs> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        _messageInspectors.Add(handler);
    }

    public async Task<MqttClientSubscribeResult> Subscribe(SubscriptionItemViewModel subscriptionItem)
    {
        if (subscriptionItem == null)
        {
            throw new ArgumentNullException(nameof(subscriptionItem));
        }

        ThrowIfNotConnected();

        var topicFilter = new MqttTopicFilterBuilder().WithTopic(subscriptionItem.Topic)
            .WithQualityOfServiceLevel(subscriptionItem.QualityOfServiceLevel.Value)
            .WithNoLocal(subscriptionItem.NoLocal)
            .WithRetainHandling(subscriptionItem.RetainHandlingSelector.RetainHandling)
            .WithRetainAsPublished(subscriptionItem.RetainAsPublished)
            .Build();

        var subscribeOptionsBuilder = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(topicFilter);

        foreach (var userProperty in subscriptionItem.UserProperties.Items)
        {
            if (!string.IsNullOrEmpty(userProperty.Name))
            {
                subscribeOptionsBuilder.WithUserProperty(userProperty.Name, userProperty.Value);
            }
        }

        var subscribeOptions = subscribeOptionsBuilder.Build();

        var subscribeResult = await _mqttClient.SubscribeAsync(subscribeOptions).ConfigureAwait(false);

        return subscribeResult;
    }

    public async Task<MqttClientUnsubscribeResult> Unsubscribe(SubscriptionItemViewModel subscriptionItem)
    {
        if (subscriptionItem == null)
        {
            throw new ArgumentNullException(nameof(subscriptionItem));
        }

        ThrowIfNotConnected();

        var unsubscribeResult = await _mqttClient.UnsubscribeAsync(subscriptionItem.Topic);

        return unsubscribeResult;
    }

    Task OnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        return _applicationMessageReceivedEvent.InvokeAsync(eventArgs);
    }

    Task OnDisconnected(MqttClientDisconnectedEventArgs arg)
    {
        if (arg.ClientWasConnected)
        {
            //arg.Reason
        }

        return Task.CompletedTask;
    }

    Task OnInspectPackage(InspectMqttPacketEventArgs eventArgs)
    {
        foreach (var messageInspector in _messageInspectors)
        {
            messageInspector.Invoke(eventArgs);
        }

        return Task.CompletedTask;
    }

    void OnLogMessagePublished(object? sender, MqttNetLogMessagePublishedEventArgs e)
    {
        LogMessagePublished?.Invoke(e);
    }

    void ThrowIfNotConnected()
    {
        if (_mqttClient == null || !_mqttClient.IsConnected)
        {
            throw new InvalidOperationException("The MQTT client is not connected.");
        }
    }
}