namespace Dreamine.PLC.Mitsubishi.MC.Protocol;

/// <summary>
/// \if KO
/// <para>Mitsubishi MC 바이너리 프레임 디바이스 코드를 정의합니다.</para>
/// \endif
/// \if EN
/// <para>Defines Mitsubishi MC device codes for binary frames.</para>
/// \endif
/// </summary>
public enum MitsubishiMcDeviceCode : byte
{
    /// <summary>
    /// \if KO
    /// <para>특수 릴레이입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The special relay.</para>
    /// \endif
    /// </summary>
    SM = 0x91,

    /// <summary>
    /// \if KO
    /// <para>특수 레지스터입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The special register.</para>
    /// \endif
    /// </summary>
    SD = 0xA9,

    /// <summary>
    /// \if KO
    /// <para>입력 릴레이입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The input relay.</para>
    /// \endif
    /// </summary>
    X = 0x9C,

    /// <summary>
    /// \if KO
    /// <para>출력 릴레이입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The output relay.</para>
    /// \endif
    /// </summary>
    Y = 0x9D,

    /// <summary>
    /// \if KO
    /// <para>내부 릴레이입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The internal relay.</para>
    /// \endif
    /// </summary>
    M = 0x90,

    /// <summary>
    /// \if KO
    /// <para>데이터 레지스터입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The data register.</para>
    /// \endif
    /// </summary>
    D = 0xA8,

    /// <summary>
    /// \if KO
    /// <para>링크 릴레이입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The link relay.</para>
    /// \endif
    /// </summary>
    B = 0xA0,

    /// <summary>
    /// \if KO
    /// <para>링크 레지스터입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The link register.</para>
    /// \endif
    /// </summary>
    W = 0xB4,

    /// <summary>
    /// \if KO
    /// <para>파일 레지스터입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The file register.</para>
    /// \endif
    /// </summary>
    R = 0xAF,

    /// <summary>
    /// \if KO
    /// <para>ZR 파일 레지스터입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The ZR file register.</para>
    /// \endif
    /// </summary>
    ZR = 0xB0
}
