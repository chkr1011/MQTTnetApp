﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet.App.Common;
using MQTTnet.App.Services.Mqtt;

namespace MQTTnet.App.Pages.Publish;

public sealed class PublishPageViewModel : BaseViewModel
{
    readonly MqttClientService _mqttClientService;

    public PublishPageViewModel(MqttClientService mqttClientService)
    {
        _mqttClientService = mqttClientService ?? throw new ArgumentNullException(nameof(mqttClientService));
        
        // Make sure that we start with at least one item.
        AddItem();
        SelectedItem = Items.FirstOrDefault();
    }

    public ObservableCollection<PublishItemViewModel> Items { get; } = new();

    public PublishItemViewModel? SelectedItem
    {
        get => GetValue<PublishItemViewModel>();
        set => SetValue(value);
    }

    public void AddItem()
    {
        var newItem = new PublishItemViewModel(this)
        {
            Name = "Untitled"
        };

        // Prepare the UI with at lest one user property.
        // It will not be send when the name is empty.
        newItem.UserProperties.AddItem();

        newItem.PublishRequested += OnItemPublishRequested;

        Items.Add(newItem);
        SelectedItem = newItem;
    }

    public void ClearItems()
    {
        Items.Clear();
        SelectedItem = null;
    }

    public void RemoveItem(PublishItemViewModel item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        Items.Remove(item);
    }

    async Task OnItemPublishRequested(PublishItemViewModel arg)
    {
        try
        {
            var response = await _mqttClientService.Publish(arg);
            arg.Response.ApplyResponse(response);
        }
        catch (Exception exception)
        {
            App.ShowException(exception);
        }
    }
}