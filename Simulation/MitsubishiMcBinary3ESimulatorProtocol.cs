using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Abstractions.Results;
using Dreamine.PLC.Core.Memory;
using Dreamine.PLC.Mitsubishi.MC.Protocol;

namespace Dreamine.PLC.Mitsubishi.MC.Simulation;

/// <summary>
/// \if KO
/// <para>읽기·쓰기 테스트용 최소 Mitsubishi MC Binary 3E 시뮬레이션을 실행합니다.</para>
/// \endif
/// \if EN
/// <para>Executes a minimal Mitsubishi MC Binary 3E simulation for read/write tests.</para>
/// \endif
/// </summary>
public sealed class MitsubishiMcBinary3ESimulatorProtocol
{
    /// <summary>
    /// \if KO
    /// <para>Request Sub Header 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the request sub header value.</para>
    /// \endif
    /// </summary>
    private const ushort RequestSubHeader = 0x5000;
    /// <summary>
    /// \if KO
    /// <para>Response Sub Header 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the response sub header value.</para>
    /// \endif
    /// </summary>
    private const ushort ResponseSubHeader = 0xD000;
    /// <summary>
    /// \if KO
    /// <para>memory 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the memory value.</para>
    /// \endif
    /// </summary>
    private readonly InMemoryPlcMemory _memory;
    /// <summary>
    /// \if KO
    /// <para>options 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the options value.</para>
    /// \endif
    /// </summary>
    private readonly MitsubishiMcSimulatorServerOptions _options;

    /// <summary>
    /// \if KO
    /// <para>공유 PLC 메모리와 옵션으로 시뮬레이터 프로토콜을 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes the simulator protocol with shared PLC memory and options.</para>
    /// \endif
    /// </summary>
    /// <param name="memory">
    /// \if KO
    /// <para>공유 PLC 메모리입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The shared PLC memory.</para>
    /// \endif
    /// </param><param name="options">
    /// \if KO
    /// <para>시뮬레이터 옵션입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The simulator options.</para>
    /// \endif
    /// </param><exception cref="ArgumentNullException">
    /// \if KO
    /// <para>메모리 또는 옵션이 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when memory or options is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public MitsubishiMcBinary3ESimulatorProtocol(
        InMemoryPlcMemory memory,
        MitsubishiMcSimulatorServerOptions options)
    {
        _memory = memory ?? throw new ArgumentNullException(nameof(memory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// \if KO
    /// <para>시뮬레이터 진단 상태 메시지가 발생할 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when the simulator emits a diagnostic status message.</para>
    /// \endif
    /// </summary>
    public event EventHandler<string>? StatusChanged;

    /// <summary>
    /// \if KO
    /// <para>Binary 3E 요청 프레임 하나를 실행하고 응답 프레임을 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Executes one Binary 3E request and returns its response frame.</para>
    /// \endif
    /// </summary>
    /// <param name="requestFrame">
    /// \if KO
    /// <para>MC 요청 프레임입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The MC request frame.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>MC 응답 프레임입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The MC response frame.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>워드 또는 비트 일괄 읽기 요청을 실행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Executes a word or bit batch-read request.</para>
    /// \endif
    /// </summary><param name="request">
    /// \if KO
    /// <para>구문 분석된 요청입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The parsed request.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>응답 프레임입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The response frame.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>워드 또는 비트 일괄 쓰기 요청을 실행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Executes a word or bit batch-write request.</para>
    /// \endif
    /// </summary><param name="request">
    /// \if KO
    /// <para>구문 분석된 요청입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The parsed request.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>응답 프레임입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The response frame.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>트리거 요청에 대해 증가된 자동 워드 응답을 메모리에 씁니다.</para>
    /// \endif
    /// \if EN
    /// <para>Writes an incremented automatic word response for a trigger request.</para>
    /// \endif
    /// </summary><param name="request">
    /// \if KO
    /// <para>쓰기 요청입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The write request.</para>
    /// \endif
    /// </param><param name="values">
    /// \if KO
    /// <para>쓴 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The written values.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Binary 3E 요청 헤더, 명령, 주소, 점 수 및 데이터를 구문 분석합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Parses Binary 3E request header, command, address, points, and data.</para>
    /// \endif
    /// </summary><param name="frame">
    /// \if KO
    /// <para>요청 프레임입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The request frame.</para>
    /// \endif
    /// </param><param name="request">
    /// \if KO
    /// <para>구문 분석된 요청입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The parsed request.</para>
    /// \endif
    /// </param><param name="errorMessage">
    /// \if KO
    /// <para>실패 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The failure message.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>성공하면 <see langword="true"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> on success.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>가능한 요청 라우팅 정보를 보존해 오류 응답을 만듭니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds an error response while preserving available request routing data.</para>
    /// \endif
    /// </summary><param name="requestFrame">
    /// \if KO
    /// <para>원본 요청입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The original request.</para>
    /// \endif
    /// </param><param name="endCode">
    /// \if KO
    /// <para>오류 종료 코드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The error end code.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>오류 응답입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The error response.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>요청 라우팅 헤더, 종료 코드 및 데이터로 Binary 3E 응답을 만듭니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds a Binary 3E response from routing header, end code, and data.</para>
    /// \endif
    /// </summary><param name="requestHeader">
    /// \if KO
    /// <para>요청 헤더입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The request header.</para>
    /// \endif
    /// </param><param name="endCode">
    /// \if KO
    /// <para>종료 코드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The end code.</para>
    /// \endif
    /// </param><param name="data">
    /// \if KO
    /// <para>응답 데이터입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The response data.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>응답 프레임입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The response frame.</para>
    /// \endif
    /// </returns><exception cref="OverflowException">
    /// \if KO
    /// <para>응답 데이터 길이가 프로토콜 범위를 벗어난 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when response data exceeds protocol length.</para>
    /// \endif
    /// </exception>
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

    /// <summary>
    /// \if KO
    /// <para>두 비트씩 상·하위 니블에 압축합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Packs two bits into the high and low nibbles of each byte.</para>
    /// \endif
    /// </summary><param name="values">
    /// \if KO
    /// <para>비트 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The bit values.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>압축된 바이트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The packed bytes.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>니블로 압축된 데이터를 비트 배열로 풉니다.</para>
    /// \endif
    /// \if EN
    /// <para>Unpacks nibble-packed data into a bit array.</para>
    /// \endif
    /// </summary><param name="data">
    /// \if KO
    /// <para>압축 데이터입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The packed data.</para>
    /// \endif
    /// </param><param name="count">
    /// \if KO
    /// <para>풀 비트 수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The bit count.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>비트 배열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The bit array.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>리틀 엔디언 16비트 값을 읽습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Reads a little-endian 16-bit value.</para>
    /// \endif
    /// </summary><param name="buffer">
    /// \if KO
    /// <para>버퍼입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The buffer.</para>
    /// \endif
    /// </param><param name="offset">
    /// \if KO
    /// <para>오프셋입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The offset.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>읽은 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value read.</para>
    /// \endif
    /// </returns>
    private static ushort ReadUInt16LittleEndian(ReadOnlySpan<byte> buffer, int offset)
    {
        return (ushort)(buffer[offset] | (buffer[offset + 1] << 8));
    }

    /// <summary>
    /// \if KO
    /// <para>리틀 엔디언 24비트 값을 읽습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Reads a little-endian 24-bit value.</para>
    /// \endif
    /// </summary><param name="buffer">
    /// \if KO
    /// <para>버퍼입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The buffer.</para>
    /// \endif
    /// </param><param name="offset">
    /// \if KO
    /// <para>오프셋입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The offset.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>읽은 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value read.</para>
    /// \endif
    /// </returns>
    private static int ReadUInt24LittleEndian(ReadOnlySpan<byte> buffer, int offset)
    {
        return buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16);
    }

    /// <summary>
    /// \if KO
    /// <para>시뮬레이터가 디바이스 코드를 지원하는지 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether the simulator supports a device code.</para>
    /// \endif
    /// </summary><param name="deviceCode">
    /// \if KO
    /// <para>디바이스 코드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The device code.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>지원하면 <see langword="true"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when supported.</para>
    /// \endif
    /// </returns>
    private static bool IsSupportedDeviceCode(byte deviceCode)
    {
        return deviceCode is 0xA8 or 0x90 or 0x9C or 0x9D or 0xA0 or 0xB4 or 0xAF or 0xB0;
    }

    /// <summary>
    /// \if KO
    /// <para>MC 디바이스 코드를 공통 PLC 타입으로 변환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Converts an MC device code to a common PLC type.</para>
    /// \endif
    /// </summary><param name="deviceCode">
    /// \if KO
    /// <para>디바이스 코드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The device code.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>공통 디바이스 타입입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The common device type.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>구문 분석된 MC 요청 필드를 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores parsed MC request fields.</para>
    /// \endif
    /// </summary>
    /// <param name="Header">
    /// \if KO
    /// <para>라우팅 헤더입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The routing header.</para>
    /// \endif
    /// </param><param name="Command">
    /// \if KO
    /// <para>명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The command.</para>
    /// \endif
    /// </param><param name="SubCommand">
    /// \if KO
    /// <para>하위 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The sub-command.</para>
    /// \endif
    /// </param><param name="DeviceOffset">
    /// \if KO
    /// <para>디바이스 오프셋입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The device offset.</para>
    /// \endif
    /// </param><param name="DeviceCode">
    /// \if KO
    /// <para>디바이스 코드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The device code.</para>
    /// \endif
    /// </param><param name="Points">
    /// \if KO
    /// <para>점 수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The point count.</para>
    /// \endif
    /// </param><param name="Data">
    /// \if KO
    /// <para>요청 데이터입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The request data.</para>
    /// \endif
    /// </param>
    private readonly record struct McRequest(
        byte[] Header,
        ushort Command,
        ushort SubCommand,
        int DeviceOffset,
        byte DeviceCode,
        int Points,
        byte[] Data);
}
