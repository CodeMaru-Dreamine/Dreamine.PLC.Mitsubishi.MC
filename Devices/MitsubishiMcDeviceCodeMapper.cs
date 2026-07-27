using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Abstractions.Results;
using Dreamine.PLC.Mitsubishi.MC.Protocol;

namespace Dreamine.PLC.Mitsubishi.MC.Devices;

/// <summary>
/// \if KO
/// <para>공통 PLC 디바이스 타입을 Mitsubishi MC 디바이스 코드로 매핑합니다.</para>
/// \endif
/// \if EN
/// <para>Maps common PLC device types to Mitsubishi MC device codes.</para>
/// \endif
/// </summary>
public sealed class MitsubishiMcDeviceCodeMapper
{
    /// <summary>
    /// \if KO
    /// <para>공통 PLC 디바이스 타입을 Mitsubishi MC 코드로 변환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Converts a common PLC device type to a Mitsubishi MC code.</para>
    /// \endif
    /// </summary>
    /// <param name="deviceType">
    /// \if KO
    /// <para>공통 PLC 디바이스 타입입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The common PLC device type.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>매핑된 코드 또는 실패 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The mapped code or a failure message.</para>
    /// \endif
    /// </returns>
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
