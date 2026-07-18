namespace Dreamine.PLC.Mitsubishi.MC.Options;

/// <summary>
/// \if KO
/// <para>Mitsubishi MC 프로토콜 통신 전송 타입을 정의합니다.</para>
/// \endif
/// \if EN
/// <para>Defines the transport type for Mitsubishi MC protocol communication.</para>
/// \endif
/// </summary>
public enum MitsubishiMcTransportType
{
    /// <summary>
    /// \if KO
    /// <para>TCP 전송을 사용합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Uses TCP transport.</para>
    /// \endif
    /// </summary>
    Tcp = 0,

    /// <summary>
    /// \if KO
    /// <para>UDP 전송을 사용합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Uses UDP transport.</para>
    /// \endif
    /// </summary>
    Udp = 1
}
