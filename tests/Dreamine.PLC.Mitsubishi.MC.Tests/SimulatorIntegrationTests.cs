using System.Net;
using System.Net.Sockets;
using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Mitsubishi.MC.Clients;
using Dreamine.PLC.Mitsubishi.MC.Options;
using Dreamine.PLC.Mitsubishi.MC.Protocol;
using Dreamine.PLC.Mitsubishi.MC.Simulation;

namespace Dreamine.PLC.Mitsubishi.MC.Tests;

public sealed class SimulatorIntegrationTests
{
    [Fact]
    public async Task TcpSimulator_RoundTripsWordsAndAutoResponse()
    {
        var port = ReserveTcpPort();
        await using var server = new MitsubishiMcTcpSimulatorServer(CreateServerOptions(port));
        await server.StartAsync();

        await ExerciseSimulatorAsync(port, MitsubishiMcTransportType.Tcp);
    }

    [Fact]
    public async Task UdpSimulator_RoundTripsWordsAndAutoResponse()
    {
        var port = ReserveUdpPort();
        await using var server = new MitsubishiMcUdpSimulatorServer(CreateServerOptions(port));
        await server.StartAsync();

        await ExerciseSimulatorAsync(port, MitsubishiMcTransportType.Udp);
    }

    private static async Task ExerciseSimulatorAsync(
        int port,
        MitsubishiMcTransportType transportType)
    {
        await using var client = new MitsubishiMcPlcClient(new MitsubishiMcConnectionOptions
        {
            Host = IPAddress.Loopback.ToString(),
            Port = port,
            TransportType = transportType,
            ConnectTimeoutMs = 2_000,
            SendTimeoutMs = 2_000,
            ReceiveTimeoutMs = 2_000,
            RetryCount = 1
        });

        Assert.True((await client.ConnectAsync()).IsSuccess);

        Assert.True((await client.WriteWordsAsync(
            new PlcAddress(PlcDeviceType.D, 100),
            new short[] { 41 })).IsSuccess);

        var trigger = await client.ReadWordsAsync(
            new PlcAddress(PlcDeviceType.D, 100),
            1);
        var response = await client.ReadWordsAsync(
            new PlcAddress(PlcDeviceType.D, 101),
            1);

        Assert.True(trigger.IsSuccess);
        Assert.Equal((short)41, Assert.Single(trigger.Value!));
        Assert.True(response.IsSuccess);
        Assert.Equal((short)42, Assert.Single(response.Value!));
        Assert.True((await client.DisconnectAsync()).IsSuccess);
    }

    private static MitsubishiMcSimulatorServerOptions CreateServerOptions(int port) =>
        new()
        {
            Host = IPAddress.Loopback.ToString(),
            Port = port,
            EnableAutoWordResponse = true,
            AutoResponseTriggerOffset = 100,
            AutoResponseOffset = 101,
            AutoResponseIncrement = 1
        };

    private static int ReserveTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static int ReserveUdpPort()
    {
        using var client = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)client.Client.LocalEndPoint!).Port;
    }
}
