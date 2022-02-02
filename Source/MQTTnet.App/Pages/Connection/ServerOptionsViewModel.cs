﻿using System.Linq;
using System.Security.Authentication;
using MQTTnet.App.Common;
using ReactiveUI;

namespace MQTTnet.App.Pages.Connection;

public sealed class ServerOptionsViewModel : BaseViewModel
{
    int _communicationTimeout;
    string _host = string.Empty;
    int _port;

    public ServerOptionsViewModel()
    {
        Host = "localhost";
        Port = 1883;
        CommunicationTimeout = 10;

        Transports.Add(new TransportViewModel("TCP", Transport.TCP));
        Transports.Add(new TransportViewModel("WebSocket", Transport.WebSocket));
        Transports.SelectedItem = Transports.FirstOrDefault()!;

        TlsVersions.Add(new TlsVersionViewModel("no TLS", SslProtocols.None));
        TlsVersions.Add(new TlsVersionViewModel("TLS 1.0", SslProtocols.Tls));
        TlsVersions.Add(new TlsVersionViewModel("TLS 1.1", SslProtocols.Tls11));
        TlsVersions.Add(new TlsVersionViewModel("TLS 1.2", SslProtocols.Tls12));
        TlsVersions.Add(new TlsVersionViewModel("TLS 1.3", SslProtocols.Tls13));

        TlsVersions.SelectedItem = TlsVersions.FirstOrDefault()!;
    }

    public int CommunicationTimeout
    {
        get => _communicationTimeout;
        set => this.RaiseAndSetIfChanged(ref _communicationTimeout, value);
    }

    public string Host
    {
        get => _host;
        set => this.RaiseAndSetIfChanged(ref _host, value);
    }

    public int Port
    {
        get => _port;
        set => this.RaiseAndSetIfChanged(ref _port, value);
    }

    public ViewModelCollection<TlsVersionViewModel> TlsVersions { get; } = new();

    public ViewModelCollection<TransportViewModel> Transports { get; } = new();
}