namespace Dreamine.PLC.Mitsubishi.MC.Protocol;

/// <summary>
/// \if KO
/// <para>Mitsubishi MC 프로토콜 하위 명령 코드를 정의합니다.</para>
/// \endif
/// \if EN
/// <para>Defines Mitsubishi MC protocol sub-command codes.</para>
/// \endif
/// </summary>
public enum MitsubishiMcSubCommand : ushort
{
    /// <summary>
    /// \if KO
    /// <para>워드 디바이스 접근입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Word-device access.</para>
    /// \endif
    /// </summary>
    Word = 0x0000,

    /// <summary>
    /// \if KO
    /// <para>비트 디바이스 접근입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Bit-device access.</para>
    /// \endif
    /// </summary>
    Bit = 0x0001
}
