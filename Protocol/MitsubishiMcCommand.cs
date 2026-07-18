namespace Dreamine.PLC.Mitsubishi.MC.Protocol;

/// <summary>
/// \if KO
/// <para>Mitsubishi MC 프로토콜 명령 코드를 정의합니다.</para>
/// \endif
/// \if EN
/// <para>Defines Mitsubishi MC protocol command codes.</para>
/// \endif
/// </summary>
public enum MitsubishiMcCommand : ushort
{
    /// <summary>
    /// \if KO
    /// <para>일괄 읽기 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The batch-read command.</para>
    /// \endif
    /// </summary>
    BatchRead = 0x0401,

    /// <summary>
    /// \if KO
    /// <para>일괄 쓰기 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The batch-write command.</para>
    /// \endif
    /// </summary>
    BatchWrite = 0x1401
}
