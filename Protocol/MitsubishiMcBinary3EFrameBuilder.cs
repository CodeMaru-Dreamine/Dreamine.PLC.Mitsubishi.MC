using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Abstractions.Results;
using Dreamine.PLC.Mitsubishi.MC.Devices;
using Dreamine.PLC.Mitsubishi.MC.Options;

namespace Dreamine.PLC.Mitsubishi.MC.Protocol;

/// <summary>
/// \if KO
/// <para>Mitsubishi MC 프로토콜 Binary 3E 요청 프레임을 만듭니다.</para>
/// \endif
/// \if EN
/// <para>Builds Mitsubishi MC protocol Binary 3E request frames.</para>
/// \endif
/// </summary>
public sealed class MitsubishiMcBinary3EFrameBuilder
{
    /// <summary>
    /// \if KO
    /// <para>Sub Header 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the sub header value.</para>
    /// \endif
    /// </summary>
    private const ushort SubHeader = 0x5000;
    /// <summary>
    /// \if KO
    /// <para>device Code Mapper 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the device code mapper value.</para>
    /// \endif
    /// </summary>
    private readonly MitsubishiMcDeviceCodeMapper _deviceCodeMapper;

    /// <summary>
    /// \if KO
    /// <para>기본 디바이스 매퍼로 프레임 빌더를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes the frame builder with the default device mapper.</para>
    /// \endif
    /// </summary>
    public MitsubishiMcBinary3EFrameBuilder()
        : this(new MitsubishiMcDeviceCodeMapper())
    {
    }

    /// <summary>
    /// \if KO
    /// <para>지정한 디바이스 매퍼로 프레임 빌더를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes the frame builder with the specified device mapper.</para>
    /// \endif
    /// </summary>
    /// <param name="deviceCodeMapper">
    /// \if KO
    /// <para>디바이스 코드 매퍼입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The device-code mapper.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para>매퍼가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the mapper is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public MitsubishiMcBinary3EFrameBuilder(MitsubishiMcDeviceCodeMapper deviceCodeMapper)
    {
        _deviceCodeMapper = deviceCodeMapper ?? throw new ArgumentNullException(nameof(deviceCodeMapper));
    }

    /// <summary>
    /// \if KO
    /// <para>일괄 읽기 요청 프레임을 만듭니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds a batch-read request frame.</para>
    /// \endif
    /// </summary>
    /// <param name="options">
    /// \if KO
    /// <para>연결 옵션입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The connection options.</para>
    /// \endif
    /// </param><param name="address">
    /// \if KO
    /// <para>시작 주소입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The start address.</para>
    /// \endif
    /// </param><param name="count">
    /// \if KO
    /// <para>읽을 디바이스 수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The device count.</para>
    /// \endif
    /// </param><param name="isBitAccess">
    /// \if KO
    /// <para>비트 접근 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Whether this is bit access.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>프레임 바이트 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The frame-byte result.</para>
    /// \endif
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para>옵션이 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when options is <see langword="null"/>.</para>
    /// \endif
    /// </exception><exception cref="OverflowException">
    /// \if KO
    /// <para>항목 수 또는 프레임 길이가 프로토콜 범위를 벗어난 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when count or frame length exceeds protocol limits.</para>
    /// \endif
    /// </exception><exception cref="ArgumentOutOfRangeException">
    /// \if KO
    /// <para>주소 오프셋이 24비트 범위를 벗어난 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the address offset is outside 24-bit range.</para>
    /// \endif
    /// </exception>
    public PlcResult<byte[]> BuildBatchReadFrame(
        MitsubishiMcConnectionOptions options,
        PlcAddress address,
        int count,
        bool isBitAccess)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (count <= 0)
        {
            return PlcResult<byte[]>.Failure("The read count must be greater than zero.");
        }

        var deviceCodeResult = _deviceCodeMapper.Map(address.DeviceType);
        if (!deviceCodeResult.IsSuccess)
        {
            return PlcResult<byte[]>.Failure(
                deviceCodeResult.Message ?? "Unsupported Mitsubishi MC device type.",
                deviceCodeResult.ErrorCode);
        }

        var payload = BuildBatchAccessPayload(
            MitsubishiMcCommand.BatchRead,
            isBitAccess ? MitsubishiMcSubCommand.Bit : MitsubishiMcSubCommand.Word,
            address,
            count,
            deviceCodeResult.Value);

        return PlcResult<byte[]>.Success(BuildFrame(options, payload));
    }

    /// <summary>
    /// \if KO
    /// <para>일괄 워드 쓰기 요청 프레임을 만듭니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds a batch word-write request frame.</para>
    /// \endif
    /// </summary>
    /// <param name="options">
    /// \if KO
    /// <para>연결 옵션입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The connection options.</para>
    /// \endif
    /// </param><param name="address">
    /// \if KO
    /// <para>시작 주소입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The start address.</para>
    /// \endif
    /// </param><param name="values">
    /// \if KO
    /// <para>워드 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The word values.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>프레임 바이트 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The frame-byte result.</para>
    /// \endif
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para>옵션 또는 값이 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when options or values is <see langword="null"/>.</para>
    /// \endif
    /// </exception><exception cref="OverflowException">
    /// \if KO
    /// <para>항목 수 또는 프레임 길이가 범위를 벗어난 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when count or frame length exceeds limits.</para>
    /// \endif
    /// </exception><exception cref="ArgumentOutOfRangeException">
    /// \if KO
    /// <para>주소 오프셋이 24비트 범위를 벗어난 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the address offset is outside 24-bit range.</para>
    /// \endif
    /// </exception>
    public PlcResult<byte[]> BuildBatchWriteWordsFrame(
        MitsubishiMcConnectionOptions options,
        PlcAddress address,
        IReadOnlyList<short> values)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0)
        {
            return PlcResult<byte[]>.Failure("The word value collection must not be empty.");
        }

        var deviceCodeResult = _deviceCodeMapper.Map(address.DeviceType);
        if (!deviceCodeResult.IsSuccess)
        {
            return PlcResult<byte[]>.Failure(
                deviceCodeResult.Message ?? "Unsupported Mitsubishi MC device type.",
                deviceCodeResult.ErrorCode);
        }

        var payload = BuildBatchAccessPayload(
            MitsubishiMcCommand.BatchWrite,
            MitsubishiMcSubCommand.Word,
            address,
            values.Count,
            deviceCodeResult.Value);

        foreach (var value in values)
        {
            MitsubishiMcEndian.WriteUInt16LittleEndian(payload, unchecked((ushort)value));
        }

        return PlcResult<byte[]>.Success(BuildFrame(options, payload));
    }

    /// <summary>
    /// \if KO
    /// <para>일괄 비트 쓰기 요청 프레임을 만듭니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds a batch bit-write request frame.</para>
    /// \endif
    /// </summary>
    /// <param name="options">
    /// \if KO
    /// <para>연결 옵션입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The connection options.</para>
    /// \endif
    /// </param><param name="address">
    /// \if KO
    /// <para>시작 주소입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The start address.</para>
    /// \endif
    /// </param><param name="values">
    /// \if KO
    /// <para>비트 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The bit values.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>프레임 바이트 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The frame-byte result.</para>
    /// \endif
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para>옵션 또는 값이 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when options or values is <see langword="null"/>.</para>
    /// \endif
    /// </exception><exception cref="OverflowException">
    /// \if KO
    /// <para>항목 수 또는 프레임 길이가 범위를 벗어난 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when count or frame length exceeds limits.</para>
    /// \endif
    /// </exception><exception cref="ArgumentOutOfRangeException">
    /// \if KO
    /// <para>주소 오프셋이 24비트 범위를 벗어난 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the address offset is outside 24-bit range.</para>
    /// \endif
    /// </exception>
    public PlcResult<byte[]> BuildBatchWriteBitsFrame(
        MitsubishiMcConnectionOptions options,
        PlcAddress address,
        IReadOnlyList<bool> values)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0)
        {
            return PlcResult<byte[]>.Failure("The bit value collection must not be empty.");
        }

        var deviceCodeResult = _deviceCodeMapper.Map(address.DeviceType);
        if (!deviceCodeResult.IsSuccess)
        {
            return PlcResult<byte[]>.Failure(
                deviceCodeResult.Message ?? "Unsupported Mitsubishi MC device type.",
                deviceCodeResult.ErrorCode);
        }

        var payload = BuildBatchAccessPayload(
            MitsubishiMcCommand.BatchWrite,
            MitsubishiMcSubCommand.Bit,
            address,
            values.Count,
            deviceCodeResult.Value);

        for (var i = 0; i < values.Count; i += 2)
        {
            var high = values[i] ? 0x10 : 0x00;
            var low = i + 1 < values.Count && values[i + 1] ? 0x01 : 0x00;

            payload.Add((byte)(high | low));
        }

        return PlcResult<byte[]>.Success(BuildFrame(options, payload));
    }

    /// <summary>
    /// \if KO
    /// <para>일괄 접근 명령 헤더 페이로드를 만듭니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds a batch-access command-header payload.</para>
    /// \endif
    /// </summary>
    /// <param name="command">
    /// \if KO
    /// <para>명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The command.</para>
    /// \endif
    /// </param><param name="subCommand">
    /// \if KO
    /// <para>하위 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The sub-command.</para>
    /// \endif
    /// </param><param name="address">
    /// \if KO
    /// <para>주소입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The address.</para>
    /// \endif
    /// </param><param name="count">
    /// \if KO
    /// <para>항목 수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The item count.</para>
    /// \endif
    /// </param><param name="deviceCode">
    /// \if KO
    /// <para>디바이스 코드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The device code.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>명령 페이로드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The command payload.</para>
    /// \endif
    /// </returns>
    private static List<byte> BuildBatchAccessPayload(
        MitsubishiMcCommand command,
        MitsubishiMcSubCommand subCommand,
        PlcAddress address,
        int count,
        MitsubishiMcDeviceCode deviceCode)
    {
        const int batchAccessHeaderLength = 10;
        var payload = new List<byte>(batchAccessHeaderLength);

        MitsubishiMcEndian.WriteUInt16LittleEndian(payload, (ushort)command);
        MitsubishiMcEndian.WriteUInt16LittleEndian(payload, (ushort)subCommand);
        MitsubishiMcEndian.WriteUInt24LittleEndian(payload, address.Offset);
        payload.Add((byte)deviceCode);
        MitsubishiMcEndian.WriteUInt16LittleEndian(payload, checked((ushort)count));

        return payload;
    }

    /// <summary>
    /// \if KO
    /// <para>라우팅 헤더, 길이, 타이머 및 페이로드를 결합해 Binary 3E 프레임을 만듭니다.</para>
    /// \endif
    /// \if EN
    /// <para>Combines routing header, length, timer, and payload into a Binary 3E frame.</para>
    /// \endif
    /// </summary>
    /// <param name="options">
    /// \if KO
    /// <para>연결 옵션입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The connection options.</para>
    /// \endif
    /// </param><param name="payload">
    /// \if KO
    /// <para>명령 페이로드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The command payload.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>완성된 프레임입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The completed frame.</para>
    /// \endif
    /// </returns>
    private static byte[] BuildFrame(
        MitsubishiMcConnectionOptions options,
        IReadOnlyCollection<byte> payload)
    {
        const int frameHeaderLength = 11;
        var frame = new List<byte>(frameHeaderLength + payload.Count);

        MitsubishiMcEndian.WriteUInt16LittleEndian(frame, SubHeader);
        frame.Add(options.NetworkNumber);
        frame.Add(options.PlcNumber);
        MitsubishiMcEndian.WriteUInt16LittleEndian(frame, options.DestinationModuleIoNumber);
        frame.Add(options.DestinationModuleStationNumber);

        var requestDataLength = checked((ushort)(payload.Count + 2));
        MitsubishiMcEndian.WriteUInt16LittleEndian(frame, requestDataLength);
        MitsubishiMcEndian.WriteUInt16LittleEndian(frame, options.MonitoringTimer);

        frame.AddRange(payload);

        return frame.ToArray();
    }
}
