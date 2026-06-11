using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Abstractions.Results;
using Dreamine.PLC.Mitsubishi.MC.Devices;
using Dreamine.PLC.Mitsubishi.MC.Options;

namespace Dreamine.PLC.Mitsubishi.MC.Protocol;

/// <summary>
/// Builds Mitsubishi MC protocol Binary 3E request frames.
/// </summary>
public sealed class MitsubishiMcBinary3EFrameBuilder
{
    private const ushort SubHeader = 0x5000;
    private readonly MitsubishiMcDeviceCodeMapper _deviceCodeMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="MitsubishiMcBinary3EFrameBuilder"/> class.
    /// </summary>
    public MitsubishiMcBinary3EFrameBuilder()
        : this(new MitsubishiMcDeviceCodeMapper())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MitsubishiMcBinary3EFrameBuilder"/> class.
    /// </summary>
    /// <param name="deviceCodeMapper">The Mitsubishi MC device code mapper.</param>
    public MitsubishiMcBinary3EFrameBuilder(MitsubishiMcDeviceCodeMapper deviceCodeMapper)
    {
        _deviceCodeMapper = deviceCodeMapper ?? throw new ArgumentNullException(nameof(deviceCodeMapper));
    }

    /// <summary>
    /// Builds a batch read request frame.
    /// </summary>
    /// <param name="options">The Mitsubishi MC connection options.</param>
    /// <param name="address">The PLC start address.</param>
    /// <param name="count">The number of devices to read.</param>
    /// <param name="isBitAccess">Whether the request accesses bit devices.</param>
    /// <returns>The request frame bytes.</returns>
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
    /// Builds a batch word write request frame.
    /// </summary>
    /// <param name="options">The Mitsubishi MC connection options.</param>
    /// <param name="address">The PLC start address.</param>
    /// <param name="values">The word values to write.</param>
    /// <returns>The request frame bytes.</returns>
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
    /// Builds a batch bit write request frame.
    /// </summary>
    /// <param name="options">The Mitsubishi MC connection options.</param>
    /// <param name="address">The PLC start address.</param>
    /// <param name="values">The bit values to write.</param>
    /// <returns>The request frame bytes.</returns>
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
