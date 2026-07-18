namespace Dreamine.PLC.Mitsubishi.MC.Protocol;

/// <summary>
/// \if KO
/// <para>Mitsubishi MC 프로토콜 프레임 형식을 정의합니다.</para>
/// \endif
/// \if EN
/// <para>Defines Mitsubishi MC protocol frame formats.</para>
/// \endif
/// </summary>
public enum MitsubishiMcFrameFormat
{
    /// <summary>
    /// \if KO
    /// <para>Binary 3E 프레임입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The Binary 3E frame.</para>
    /// \endif
    /// </summary>
    Binary3E = 0,

    /// <summary>
    /// \if KO
    /// <para>ASCII 3E 프레임입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The ASCII 3E frame.</para>
    /// \endif
    /// </summary>
    Ascii3E = 1
}
