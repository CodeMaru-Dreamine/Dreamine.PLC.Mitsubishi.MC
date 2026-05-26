using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Abstractions.Results;
using Dreamine.PLC.Mitsubishi.MC.Protocol;

namespace Dreamine.PLC.Mitsubishi.MC.Devices;

/// <summary>
/// Maps common Dreamine PLC device types to Mitsubishi MC protocol device codes.
/// </summary>
public sealed class MitsubishiMcDeviceCodeMapper
{
    /// <summary>
    /// Converts a common PLC device type to a Mitsubishi MC device code.
    /// </summary>
    /// <param name="deviceType">The common PLC device type.</param>
    /// <returns>The Mitsubishi MC device code result.</returns>
    public PlcResult<MitsubishiMcDeviceCode> Map(PlcDeviceType deviceType)
    {
        return deviceType switch
        {
            PlcDeviceType.D => PlcResult<MitsubishiMcDeviceCode>.Success(MitsubishiMcDeviceCode.D),
            PlcDeviceType.M => PlcResult<MitsubishiMcDeviceCode>.Success(MitsubishiMcDeviceCode.M),
            PlcDeviceType.X => PlcResult<MitsubishiMcDeviceCode>.Success(MitsubishiMcDeviceCode.X),
            PlcDeviceType.Y => PlcResult<MitsubishiMcDeviceCode>.Success(MitsubishiMcDeviceCode.Y),
            PlcDeviceType.B => PlcResult<MitsubishiMcDeviceCode>.Success(MitsubishiMcDeviceCode.B),
            PlcDeviceType.W => PlcResult<MitsubishiMcDeviceCode>.Success(MitsubishiMcDeviceCode.W),
            PlcDeviceType.R => PlcResult<MitsubishiMcDeviceCode>.Success(MitsubishiMcDeviceCode.R),
            PlcDeviceType.ZR => PlcResult<MitsubishiMcDeviceCode>.Success(MitsubishiMcDeviceCode.ZR),
            _ => PlcResult<MitsubishiMcDeviceCode>.Failure($"Unsupported Mitsubishi MC device type: {deviceType}")
        };
    }
}