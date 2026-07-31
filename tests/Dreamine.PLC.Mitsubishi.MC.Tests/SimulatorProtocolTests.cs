using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Core.Memory;
using Dreamine.PLC.Mitsubishi.MC.Options;
using Dreamine.PLC.Mitsubishi.MC.Protocol;
using Dreamine.PLC.Mitsubishi.MC.Simulation;

namespace Dreamine.PLC.Mitsubishi.MC.Tests;

public sealed class SimulatorProtocolTests
{
    private readonly MitsubishiMcConnectionOptions _connection = new();
    private readonly MitsubishiMcBinary3EFrameBuilder _builder = new();
    private readonly MitsubishiMcBinary3EResponseParser _parser = new();

    [Fact]
    public void Constructor_RejectsNullDependencies()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MitsubishiMcBinary3ESimulatorProtocol(null!, new()));
        Assert.Throws<ArgumentNullException>(
            () => new MitsubishiMcBinary3ESimulatorProtocol(new(), null!));
    }

    [Fact]
    public void Execute_RoundTripsWordAndBitMemory()
    {
        var protocol = CreateProtocol();
        var wordAddress = new PlcAddress(PlcDeviceType.D, 100);
        var bitAddress = new PlcAddress(PlcDeviceType.M, 20);

        var writeWords = protocol.Execute(
            _builder.BuildBatchWriteWordsFrame(_connection, wordAddress, [123, -45]).Value!);
        Assert.True(_parser.Parse(writeWords).IsSuccess);

        var readWords = protocol.Execute(
            _builder.BuildBatchReadFrame(_connection, wordAddress, 2, false).Value!);
        Assert.Equal(new short[] { 123, -45 }, _parser.ParseReadWords(readWords, 2).Value);

        var writeBits = protocol.Execute(
            _builder.BuildBatchWriteBitsFrame(
                _connection, bitAddress, [true, false, true, true, false]).Value!);
        Assert.True(_parser.Parse(writeBits).IsSuccess);

        var readBits = protocol.Execute(
            _builder.BuildBatchReadFrame(_connection, bitAddress, 5, true).Value!);
        Assert.Equal(
            new[] { true, false, true, true, false },
            _parser.ParseReadBits(readBits, 5).Value);
    }

    [Fact]
    public void Execute_AppliesAutoResponseAndReportsOverflow()
    {
        var memory = new InMemoryPlcMemory();
        var protocol = CreateProtocol(memory);
        var messages = new List<string>();
        protocol.StatusChanged += (_, message) => messages.Add(message);
        var trigger = new PlcAddress(PlcDeviceType.D, 100);

        protocol.Execute(
            _builder.BuildBatchWriteWordsFrame(_connection, trigger, [41]).Value!);
        Assert.Equal(
            (short)42,
            Assert.Single(memory.ReadWords(new PlcAddress(PlcDeviceType.D, 101), 1).Value!));
        Assert.Contains(messages, message => message.Contains("D101=42"));

        protocol.Execute(
            _builder.BuildBatchWriteWordsFrame(_connection, trigger, [short.MaxValue]).Value!);
        Assert.Contains(messages, message => message.Contains("overflow"));
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void Execute_RejectsMalformedRequests(byte[] request)
    {
        var protocol = CreateProtocol();
        string? status = null;
        protocol.StatusChanged += (_, message) => status = message;

        var response = protocol.Execute(request);

        var parsed = _parser.Parse(response);
        Assert.False(parsed.IsSuccess);
        Assert.Equal(0xC051, parsed.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(status));
    }

    [Fact]
    public void Execute_RejectsUnsupportedCommandsSubcommandsAndPayloads()
    {
        var protocol = CreateProtocol();
        var address = new PlcAddress(PlcDeviceType.D, 10);

        var unknownCommand =
            _builder.BuildBatchReadFrame(_connection, address, 1, false).Value!;
        SetUInt16(unknownCommand, 11, 0x9999);
        Assert.Equal(0xC059, _parser.Parse(protocol.Execute(unknownCommand)).ErrorCode);

        var unknownReadSubcommand =
            _builder.BuildBatchReadFrame(_connection, address, 1, false).Value!;
        SetUInt16(unknownReadSubcommand, 13, 0x9999);
        Assert.Equal(0xC059, _parser.Parse(protocol.Execute(unknownReadSubcommand)).ErrorCode);

        var zeroRead =
            _builder.BuildBatchReadFrame(_connection, address, 1, false).Value!;
        SetUInt16(zeroRead, 19, 0);
        Assert.Equal(0xC051, _parser.Parse(protocol.Execute(zeroRead)).ErrorCode);

        var shortWordWrite =
            _builder.BuildBatchWriteWordsFrame(_connection, address, [1]).Value!;
        SetUInt16(shortWordWrite, 19, 2);
        Assert.Equal(0xC051, _parser.Parse(protocol.Execute(shortWordWrite)).ErrorCode);

        var shortBitWrite =
            _builder.BuildBatchWriteBitsFrame(_connection, address, [true]).Value!;
        SetUInt16(shortBitWrite, 19, 3);
        Assert.Equal(0xC051, _parser.Parse(protocol.Execute(shortBitWrite)).ErrorCode);

        var unknownWriteSubcommand =
            _builder.BuildBatchWriteWordsFrame(_connection, address, [1]).Value!;
        SetUInt16(unknownWriteSubcommand, 13, 0x9999);
        Assert.Equal(0xC059, _parser.Parse(protocol.Execute(unknownWriteSubcommand)).ErrorCode);
    }

    public static TheoryData<byte[]> InvalidRequests =>
        new()
        {
            Array.Empty<byte>(),
            new byte[6],
            new byte[21],
            BuildInvalidSubHeader(),
            BuildIncompleteRequest(),
            BuildUnsupportedDeviceRequest()
        };

    private MitsubishiMcBinary3ESimulatorProtocol CreateProtocol(
        InMemoryPlcMemory? memory = null) =>
        new(
            memory ?? new InMemoryPlcMemory(),
            new MitsubishiMcSimulatorServerOptions
            {
                EnableAutoWordResponse = true,
                AutoResponseTriggerOffset = 100,
                AutoResponseOffset = 101,
                AutoResponseIncrement = 1
            });

    private static byte[] BuildInvalidSubHeader()
    {
        var frame = ValidSkeleton();
        frame[0] = 0x34;
        frame[1] = 0x12;
        return frame;
    }

    private static byte[] BuildIncompleteRequest()
    {
        var frame = ValidSkeleton();
        SetUInt16(frame, 7, 100);
        return frame;
    }

    private static byte[] BuildUnsupportedDeviceRequest()
    {
        var frame = ValidSkeleton();
        frame[18] = 0xFF;
        return frame;
    }

    private static byte[] ValidSkeleton() =>
        [
            0x00, 0x50, 0, 0xFF, 0xFF, 0x03, 0,
            12, 0, 0, 0,
            0x01, 0x04, 0, 0,
            0, 0, 0, 0xA8, 1, 0
        ];

    private static void SetUInt16(byte[] frame, int offset, ushort value)
    {
        frame[offset] = (byte)value;
        frame[offset + 1] = (byte)(value >> 8);
    }
}
