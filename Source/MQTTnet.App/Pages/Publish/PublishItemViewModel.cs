﻿using System;
using System.Threading.Tasks;
using MQTTnet.App.Common;
using MQTTnet.App.Common.QualityOfServiceLevel;

namespace MQTTnet.App.Pages.Publish;

public sealed class PublishItemViewModel : BaseViewModel
{
    public PublishItemViewModel()
    {
        Payload = string.Empty;
        PayloadFormatIndicator.IsUnspecified = true;
    }

    public event EventHandler? DeleteRequested;

    public event Func<PublishItemViewModel, Task>? PublishRequested;

    public string ContentType
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public uint MessageExpiryInterval
    {
        get => GetValue<uint>();
        set => SetValue(value);
    }

    public string Name
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public string Payload
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public PayloadFormatIndicatorViewModel PayloadFormatIndicator { get; } = new();

    public QualityOfServiceLevelSelectorViewModel QualityOfServiceLevel { get; } = new();

    public PublishResponseViewModel Response { get; } = new();

    public bool Retain
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public uint SubscriptionIdentifier
    {
        get => GetValue<uint>();
        set => SetValue(value);
    }

    public string Topic
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public ushort TopicAlias
    {
        get => GetValue<ushort>();
        set => SetValue(value);
    }

    public void Delete()
    {
        DeleteRequested?.Invoke(this, EventArgs.Empty);
    }

    public Task Publish()
    {
        return PublishRequested?.Invoke(this) ?? Task.CompletedTask;
    }
}