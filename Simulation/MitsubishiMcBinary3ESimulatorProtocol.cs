using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Abstractions.Results;
using Dreamine.PLC.Core.Memory;
using Dreamine.PLC.Mitsubishi.MC.Protocol;

namespace Dreamine.PLC.Mitsubishi.MC.Simulation;

/// <summary>
/// Executes a minimal Mitsubishi MC Binary 3E protocol simulation for read/write tests.
/// </summary>
public sealed class MitsubishiMcBinary3ESimulatorProtocol
{
    private const ushort RequestSubHeader = 0x5000;
    private const ushort ResponseSubHeader = 0xD000;
    private readonly InMemoryPlcMemory _memory;
    private readonly MitsubishiMcSimulatorServerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MitsubishiMcBinary3ESimulatorProtocol"/> class.
    /// </summary>
    /// <param name="memory">The shared PLC memory.</param>
    /// <param name="options">The simulator options.</param>
    public MitsubishiMcBinary3ESimulatorProtocol(
        InMemoryPlcMemory memory,
        MitsubishiMcSimulatorServerOptions options)
    {
        _memory = memory ?? throw new ArgumentNullException(nameof(memory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Occurs when the simulator has a diagnostic status message.
    /// </summary>
    public event EventHandler<string>? StatusChanged;

    /// <summary>
    /// Executes one Binary 3E request frame and returns a Binary 3E response frame.
    /// </summary>
    /// <param name="requestFrame">The MC request frame.</param>
    /// <returns>The MC response frame.</returns>
    public byte[] Execute(ReadOnlySpan<byte> requestFrame)
    {
        if (!TryParseRequest(requestFrame, out var request, out var errorMessage))
        {
            StatusChanged?.Invoke(this, errorMessage ?? "Invalid MC request frame.");
            return BuildErrorResponse(requestFrame, 0xC051);
        }

        return request.Command switch
        {
            (ushort)MitsubishiMcCommand.BatchRead => ExecuteRead(request),
            (ushort)MitsubishiMcCommand.BatchWrite => ExecuteWrite(request),
            _ => BuildResponse(request.Header, 0xC059, [])
        };
    }

    private byte[] ExecuteRead(McRequest request)
    {
        if (request.Points <= 0)
        {
            return BuildResponse(request.Header, 0xC051, []);
        }

        var address = new PlcAddress(ToDeviceType(request.DeviceCode), request.DeviceOffset);

        if (request.SubCommand == (ushort)MitsubishiMcSubCommand.Word)
        {
            var readResult = _memory.ReadWords(address, request.Points);
            if (!readResult.IsSuccess || readResult.Value is null)
            {
                return BuildResponse(request.Header, 0xC051, []);
            }

            var data = new List<byte>(request.Points * 2);
            foreach (var value in readResult.Value)
            {
                MitsubishiMcEndian.WriteUInt16LittleEndian(data, unchecked((ushort)value));
            }

            return BuildResponse(request.Header, 0x0000, data);
        }

        if (request.SubCommand == (ushort)MitsubishiMcSubCommand.Bit)
        {
            var readResult = _memory.ReadBits(address, request.Points);
            if (!readResult.IsSuccess || readResult.Value is null)
            {
                return BuildResponse(request.Header, 0xC051, []);
            }

            var data = PackBits(readResult.Value);
            return BuildResponse(request.Header, 0x0000, data);
        }

        return BuildResponse(request.Header, 0xC059, []);
    }

    private byte[] ExecuteWrite(McRequest request)
    {
        if (request.Points <= 0)
        {
            return BuildResponse(request.Header, 0xC051, []);
        }

        var address = new PlcAddress(ToDeviceType(request.DeviceCode), request.DeviceOffset);

        if (request.SubCommand == (ushort)MitsubishiMcSubCommand.Word)
        {
            var requiredLength = request.Points * 2;
            if (request.Data.Length < requiredLength)
            {
                return BuildResponse(request.Header, 0xC051, []);
            }

            var values = new short[request.Points];
            for (var i = 0; i < request.Points; i++)
            {
                values[i] = unchecked((short)ReadUInt16LittleEndian(request.Data, i * 2));
            }

            var writeResult = _memory.WriteWords(address, values);
            if (!writeResult.IsSuccess)
            {
                return BuildResponse(request.Header, 0xC051, []);
            }

            ApplyAutoWordResponse(request, values);
            return BuildResponse(request.Header, 0x0000, []);
        }

        if (request.SubCommand == (ushort)MitsubishiMcSubCommand.Bit)
        {
            var requiredLength = (request.Points + 1) / 2;
            if (request.Data.Length < requiredLength)
            {
                return BuildResponse(request.Header, 0xC051, []);
            }

            var values = UnpackBits(request.Data, request.Points);
            var writeResult = _memory.WriteBits(address, values);
            return writeResult.IsSuccess
                ? BuildResponse(request.Header, 0x0000, [])
                : BuildResponse(request.Header, 0xC051, []);
        }

        return BuildResponse(request.Header, 0xC059, []);
    }

    private void ApplyAutoWordResponse(McRequest request, IReadOnlyList<short> values)
    {
        if (!_options.EnableAutoWordResponse || values.Count != 1)
        {
            return;
        }

        if (request.DeviceCode != _options.AutoResponseTriggerDeviceCode ||
            request.DeviceOffset != _options.AutoResponseTriggerOffset)
        {
            return;
        }

        var rawResponseValue = values[0] + _options.AutoResponseIncrement;
        if (rawResponseValue is < short.MinValue or > short.MaxValue)
        {
            StatusChanged?.Invoke(this, $"MC auto response skipped: value overflow. value={rawResponseValue}");
            return;
        }

        var responseValue = (short)rawResponseValue;
        var responseAddress = new PlcAddress(ToDeviceType(_options.AutoResponseDeviceCode), _options.AutoResponseOffset);
        _memory.WriteWords(responseAddress, [responseValue]);
        StatusChanged?.Invoke(this, $"MC auto response: D{_options.AutoResponseOffset}={responseValue}");
    }

    private static bool TryParseRequest(
        ReadOnlySpan<byte> frame,
        out McRequest request,
        out string? errorMessage)
    {
        request = default;
        errorMessage = null;

        if (frame.Length < 21)
        {
            errorMessage = $"The MC request frame is too short. Length={frame.Length}.";
            return false;
        }

        var subHeader = ReadUInt16LittleEndian(frame, 0);
        if (subHeader != RequestSubHeader)
        {
            errorMessage = $"Invalid MC request sub-header: 0x{subHeader:X4}.";
            return false;
        }

        var requestDataLength = ReadUInt16LittleEndian(frame, 7);
        var expectedFrameLength = 9 + requestDataLength;
        if (frame.Length < expectedFrameLength)
        {
            errorMessage = $"The MC request frame is incomplete. Expected={expectedFrameLength}, Actual={frame.Length}.";
            return false;
        }

        var header = frame[..7].ToArray();
        var command = ReadUInt16LittleEndian(frame, 11);
        var subCommand = ReadUInt16LittleEndian(frame, 13);
        var deviceOffset = ReadUInt24LittleEndian(frame, 15);
        var deviceCode = frame[18];
        var points = ReadUInt16LittleEndian(frame, 19);
        var dataStart = 21;
        var dataLength = expectedFrameLength - dataStart;
        var data = dataLength > 0 ? frame.Slice(dataStart, dataLength).ToArray() : [];

        if (!IsSupportedDeviceCode(deviceCode))
        {
            errorMessage = $"Unsupported MC device code: 0x{deviceCode:X2}.";
            return false;
        }

        request = new McRequest(header, command, subCommand, deviceOffset, deviceCode, points, data);
        return true;
    }

    private static byte[] BuildErrorResponse(ReadOnlySpan<byte> requestFrame, ushort endCode)
    {
        var header = new byte[7];
        if (requestFrame.Length >= 7)
        {
            requestFrame[..7].CopyTo(header);
        }
        else
        {
            header[0] = 0x00;
            header[1] = 0x50;
        }

        return BuildResponse(header, endCode, []);
    }

    private static byte[] BuildResponse(IReadOnlyList<byte> requestHeader, ushort endCode, IReadOnlyList<byte> data)
    {
        var frame = new List<byte>(11 + data.Count);
        MitsubishiMcEndian.WriteUInt16LittleEndian(frame, ResponseSubHeader);

        frame.Add(requestHeader.Count > 2 ? requestHeader[2] : (byte)0x00);
        frame.Add(requestHeader.Count > 3 ? requestHeader[3] : (byte)0xFF);
        frame.Add(requestHeader.Count > 4 ? requestHeader[4] : (byte)0xFF);
        frame.Add(requestHeader.Count > 5 ? requestHeader[5] : (byte)0x03);
        frame.Add(requestHeader.Count > 6 ? requestHeader[6] : (byte)0x00);

        var responseDataLength = checked((ushort)(data.Count + 2));
        MitsubishiMcEndian.WriteUInt16LittleEndian(frame, responseDataLength);
        MitsubishiMcEndian.WriteUInt16LittleEndian(frame, endCode);
        foreach (var item in data)
        {
            frame.Add(item);
        }

        return frame.ToArray();
    }

    private static byte[] PackBits(IReadOnlyList<bool> values)
    {
        var bytes = new byte[(values.Count + 1) / 2];
        for (var i = 0; i < values.Count; i++)
        {
            if (!values[i])
            {
                continue;
            }

            if (i % 2 == 0)
            {
                bytes[i / 2] |= 0x10;
            }
            else
            {
                bytes[i / 2] |= 0x01;
            }
        }

        return bytes;
    }

    private static bool[] UnpackBits(ReadOnlySpan<byte> data, int count)
    {
        var values = new bool[count];
        for (var i = 0; i < count; i++)
        {
            var packed = data[i / 2];
            values[i] = i % 2 == 0
                ? (packed & 0x10) != 0
                : (packed & 0x01) != 0;
        }

        return values;
    }

    private static ushort ReadUInt16LittleEndian(ReadOnlySpan<byte> buffer, int offset)
    {
        return (ushort)(buffer[offset] | (buffer[offset + 1] << 8));
    }

    private static int ReadUInt24LittleEndian(ReadOnlySpan<byte> buffer, int offset)
    {
        return buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16);
    }

    private static bool IsSupportedDeviceCode(byte deviceCode)
    {
        return deviceCode is 0xA8 or 0x90 or 0x9C or 0x9D or 0xA0 or 0xB4 or 0xAF or 0xB0;
    }

    private static PlcDeviceType ToDeviceType(byte deviceCode)
    {
        return deviceCode switch
        {
            0xA8 => PlcDeviceType.D,
            0x90 => PlcDeviceType.M,
            0x9C => PlcDeviceType.X,
            0x9D => PlcDeviceType.Y,
            0xA0 => PlcDeviceType.B,
            0xB4 => PlcDeviceType.W,
            0xAF => PlcDeviceType.R,
            0xB0 => PlcDeviceType.ZR,
            _ => PlcDeviceType.Unknown
        };
    }

    private readonly record struct McRequest(
        byte[] Header,
        ushort Command,
        ushort SubCommand,
        int DeviceOffset,
        byte DeviceCode,
        int Points,
        byte[] Data);
}
