using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Mitsubishi.MC.Clients;
using Dreamine.PLC.Mitsubishi.MC.Devices;
using Dreamine.PLC.Mitsubishi.MC.Options;
using Dreamine.PLC.Mitsubishi.MC.Protocol;
using Dreamine.PLC.Mitsubishi.MC.Transport;

namespace Dreamine.PLC.Mitsubishi.MC.Tests;

public sealed class ProtocolAndClientTests
{
    [Theory]
    [InlineData(PlcDeviceType.D, MitsubishiMcDeviceCode.D)]
    [InlineData(PlcDeviceType.M, MitsubishiMcDeviceCode.M)]
    [InlineData(PlcDeviceType.X, MitsubishiMcDeviceCode.X)]
    [InlineData(PlcDeviceType.Y, MitsubishiMcDeviceCode.Y)]
    [InlineData(PlcDeviceType.B, MitsubishiMcDeviceCode.B)]
    [InlineData(PlcDeviceType.W, MitsubishiMcDeviceCode.W)]
    [InlineData(PlcDeviceType.R, MitsubishiMcDeviceCode.R)]
    [InlineData(PlcDeviceType.ZR, MitsubishiMcDeviceCode.ZR)]
    public void DeviceCodeMapper_MapsSupportedTypes(
        PlcDeviceType deviceType,
        MitsubishiMcDeviceCode expected)
    {
        var result = new MitsubishiMcDeviceCodeMapper().Map(deviceType);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void DeviceCodeMapper_RejectsUnknownType()
    {
        var result = new MitsubishiMcDeviceCodeMapper().Map(PlcDeviceType.Unknown);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void FrameBuilder_BuildsBinary3EReadFrame()
    {
        var result = new MitsubishiMcBinary3EFrameBuilder().BuildBatchReadFrame(
            new MitsubishiMcConnectionOptions(),
            new PlcAddress(PlcDeviceType.D, 100),
            2,
            isBitAccess: false);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(0x00, result.Value[0]);
        Assert.Equal(0x50, result.Value[1]);
        Assert.Contains((byte)MitsubishiMcDeviceCode.D, result.Value);
    }

    [Fact]
    public void FrameBuilder_RejectsInvalidReadCountAndEmptyWrites()
    {
        var builder = new MitsubishiMcBinary3EFrameBuilder();
        var options = new MitsubishiMcConnectionOptions();
        var address = new PlcAddress(PlcDeviceType.D, 100);

        Assert.False(builder.BuildBatchReadFrame(options, address, 0, false).IsSuccess);
        Assert.False(builder.BuildBatchWriteWordsFrame(options, address, []).IsSuccess);
        Assert.False(builder.BuildBatchWriteBitsFrame(options, address, []).IsSuccess);
    }

    [Fact]
    public void ResponseParser_ParsesWordAndBitResponses()
    {
        var parser = new MitsubishiMcBinary3EResponseParser();

        var words = parser.ParseReadWords(
            BuildResponse(0x00, 0x00, 0x34, 0x12, 0xFE, 0xFF),
            2);
        var bits = parser.ParseReadBits(
            BuildResponse(0x00, 0x00, 0x11, 0x10),
            4);

        Assert.True(words.IsSuccess);
        Assert.Equal(new short[] { 0x1234, -2 }, words.Value);
        Assert.True(bits.IsSuccess);
        Assert.Equal(new[] { true, true, true, false }, bits.Value);
    }

    [Theory]
    [MemberData(nameof(InvalidResponses))]
    public void ResponseParser_RejectsInvalidResponses(byte[] frame)
    {
        var result = new MitsubishiMcBinary3EResponseParser().Parse(frame);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ResponseParser_RejectsInvalidCountsAndIncompletePayloads()
    {
        var parser = new MitsubishiMcBinary3EResponseParser();

        Assert.False(parser.ParseReadWords(BuildResponse(0, 0), 0).IsSuccess);
        Assert.False(parser.ParseReadBits(BuildResponse(0, 0), -1).IsSuccess);
        Assert.False(parser.ParseReadWords(BuildResponse(0, 0, 1), 1).IsSuccess);
        Assert.False(parser.ParseReadBits(BuildResponse(0, 0), 1).IsSuccess);
        Assert.False(parser.ParseReadWords(BuildResponse(0x51, 0xC0), 1).IsSuccess);
        Assert.False(parser.ParseReadBits(BuildResponse(0x51, 0xC0), 1).IsSuccess);
    }

    public static TheoryData<byte[]> InvalidResponses =>
        new()
        {
            Array.Empty<byte>(),
            new byte[] { 0x00, 0x50, 0, 0, 0, 0, 0, 2, 0, 0, 0 },
            new byte[] { 0x00, 0xD0, 0, 0, 0, 0, 0, 8, 0, 0, 0 },
            BuildResponse(0x51, 0xC0)
        };

    [Fact]
    public void EndianHelpers_WriteLittleEndianAndRejectOutOfRangeUInt24()
    {
        var bytes = new List<byte>();

        MitsubishiMcEndian.WriteUInt16LittleEndian(bytes, 0x1234);

        Assert.Equal(new byte[] { 0x34, 0x12 }, bytes);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MitsubishiMcEndian.WriteUInt24LittleEndian(bytes, -1));
    }

    [Fact]
    public async Task FakeTransport_RequiresConnectionAndQueuedResponse()
    {
        await using var transport = new FakeMitsubishiMcTransport();

        Assert.False((await transport.SendAndReceiveAsync([1], 100, 0)).IsSuccess);
        Assert.True((await transport.ConnectAsync("localhost", 5000, 100)).IsSuccess);
        Assert.False((await transport.SendAndReceiveAsync([1], 100, 0)).IsSuccess);

        transport.EnqueueResponse(BuildResponse(0, 0));
        var result = await transport.SendAndReceiveAsync([1, 2, 3], 100, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, transport.SentFrames.Count);
        Assert.Equal(new byte[] { 1, 2, 3 }, transport.SentFrames[1]);
        Assert.True((await transport.DisconnectAsync()).IsSuccess);
        Assert.False(transport.IsConnected);
    }

    [Fact]
    public async Task Client_RoundTripsWordsAndBitsThroughFakeTransport()
    {
        await using var transport = new FakeMitsubishiMcTransport();
        await using var client = CreateClient(transport);

        Assert.True((await client.ConnectAsync()).IsSuccess);

        transport.EnqueueResponse(BuildResponse(0x00, 0x00));
        Assert.True((await client.WriteWordsAsync(
            new PlcAddress(PlcDeviceType.D, 100),
            new short[] { 12, 34 })).IsSuccess);

        transport.EnqueueResponse(BuildResponse(0x00, 0x00, 0x0C, 0x00, 0x22, 0x00));
        var words = await client.ReadWordsAsync(new PlcAddress(PlcDeviceType.D, 100), 2);
        Assert.True(words.IsSuccess);
        Assert.Equal(new short[] { 12, 34 }, words.Value);

        transport.EnqueueResponse(BuildResponse(0x00, 0x00));
        Assert.True((await client.WriteBitsAsync(
            new PlcAddress(PlcDeviceType.M, 10),
            new[] { true, false, true })).IsSuccess);

        transport.EnqueueResponse(BuildResponse(0x00, 0x00, 0x10, 0x10));
        var bits = await client.ReadBitsAsync(new PlcAddress(PlcDeviceType.M, 10), 3);
        Assert.True(bits.IsSuccess);
        Assert.Equal(new[] { true, false, true }, bits.Value);
    }

    [Fact]
    public async Task FakeTransport_HonorsCancellation()
    {
        await using var transport = new FakeMitsubishiMcTransport();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => transport.ConnectAsync("localhost", 5000, 100, cancellation.Token));
    }

    [Fact]
    public async Task Client_ReportsTransportAndProtocolFailuresForEveryOperation()
    {
        await using var transport = new FakeMitsubishiMcTransport();
        await using var client = CreateClient(transport);
        Assert.True((await client.ConnectAsync()).IsSuccess);
        var words = new PlcAddress(PlcDeviceType.D, 100);
        var bits = new PlcAddress(PlcDeviceType.M, 10);

        Assert.False((await client.ReadWordsAsync(words, 1)).IsSuccess);
        Assert.False((await client.ReadBitsAsync(bits, 1)).IsSuccess);
        Assert.False((await client.WriteWordsAsync(words, [1])).IsSuccess);
        Assert.False((await client.WriteBitsAsync(bits, [true])).IsSuccess);

        transport.EnqueueResponse(BuildResponse(0x51, 0xC0));
        Assert.False((await client.ReadWordsAsync(words, 1)).IsSuccess);
        transport.EnqueueResponse(BuildResponse(0x51, 0xC0));
        Assert.False((await client.ReadBitsAsync(bits, 1)).IsSuccess);
        transport.EnqueueResponse(BuildResponse(0x51, 0xC0));
        Assert.False((await client.WriteWordsAsync(words, [1])).IsSuccess);
        transport.EnqueueResponse(BuildResponse(0x51, 0xC0));
        Assert.False((await client.WriteBitsAsync(bits, [true])).IsSuccess);
    }

    [Fact]
    public async Task Client_ConstructorsValidateDependenciesAndTransportTypes()
    {
        Assert.Throws<ArgumentNullException>(() => new MitsubishiMcPlcClient(null!));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new MitsubishiMcPlcClient(
                new MitsubishiMcConnectionOptions
                {
                    TransportType = (MitsubishiMcTransportType)999
                }));

        var options = new MitsubishiMcConnectionOptions();
        var transport = new FakeMitsubishiMcTransport();
        var builder = new MitsubishiMcBinary3EFrameBuilder();
        var parser = new MitsubishiMcBinary3EResponseParser();
        Assert.Throws<ArgumentNullException>(
            () => new MitsubishiMcPlcClient(null!, transport, builder, parser));
        Assert.Throws<ArgumentNullException>(
            () => new MitsubishiMcPlcClient(options, null!, builder, parser));
        Assert.Throws<ArgumentNullException>(
            () => new MitsubishiMcPlcClient(options, transport, null!, parser));
        Assert.Throws<ArgumentNullException>(
            () => new MitsubishiMcPlcClient(options, transport, builder, null!));

        await using var tcp = new MitsubishiMcPlcClient(
            new MitsubishiMcConnectionOptions { TransportType = MitsubishiMcTransportType.Tcp });
        await using var udp = new MitsubishiMcPlcClient(
            new MitsubishiMcConnectionOptions { TransportType = MitsubishiMcTransportType.Udp });
        Assert.Equal(MitsubishiMcTransportType.Tcp, tcp.Options.TransportType);
        Assert.Equal(MitsubishiMcTransportType.Udp, udp.Options.TransportType);
    }

    [Fact]
    public async Task FakeTransport_ValidatesInputCancellationAndDispose()
    {
        var transport = new FakeMitsubishiMcTransport();
        await transport.ConnectAsync("localhost", 5000, 100);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => transport.SendAndReceiveAsync(null!, 100, 0));
        Assert.False((await transport.SendAndReceiveAsync([], 100, 0)).IsSuccess);
        Assert.Throws<ArgumentNullException>(() => transport.EnqueueResponse(null!));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => transport.DisconnectAsync(cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => transport.SendAndReceiveAsync([1], 100, 0, cancellation.Token));

        transport.EnqueueResponse(BuildResponse(0, 0));
        await transport.DisposeAsync();
        Assert.False(transport.IsConnected);
        Assert.Empty(transport.SentFrames);
    }

    private static MitsubishiMcPlcClient CreateClient(FakeMitsubishiMcTransport transport) =>
        new(
            new MitsubishiMcConnectionOptions
            {
                Host = "localhost",
                Port = 5000,
                ConnectTimeoutMs = 100,
                ReceiveTimeoutMs = 100,
                RetryCount = 0
            },
            transport,
            new MitsubishiMcBinary3EFrameBuilder(),
            new MitsubishiMcBinary3EResponseParser());

    private static byte[] BuildResponse(params byte[] data)
    {
        var length = data.Length;
        return
        [
            0x00, 0xD0, 0, 0, 0, 0, 0,
            (byte)length, (byte)(length >> 8),
            .. data
        ];
    }
}
