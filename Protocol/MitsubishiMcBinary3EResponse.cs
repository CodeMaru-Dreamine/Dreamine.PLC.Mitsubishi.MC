namespace Dreamine.PLC.Mitsubishi.MC.Protocol;

/// <summary>
/// \if KO
/// <para>구문 분석된 Mitsubishi MC Binary 3E 응답을 나타냅니다.</para>
/// \endif
/// \if EN
/// <para>Represents a parsed Mitsubishi MC Binary 3E response.</para>
/// \endif
/// </summary>
public sealed class MitsubishiMcBinary3EResponse
{
    /// <summary>
    /// \if KO
    /// <para>종료 코드와 데이터로 응답을 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes the response with an end code and data.</para>
    /// \endif
    /// </summary>
    /// <param name="endCode">
    /// \if KO
    /// <para>MC 종료 코드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The MC end code.</para>
    /// \endif
    /// </param><param name="data">
    /// \if KO
    /// <para>응답 데이터입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The response data.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para><paramref name="data"/>가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="data"/> is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public MitsubishiMcBinary3EResponse(ushort endCode, byte[] data)
    {
        EndCode = endCode;
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    /// <summary>
    /// \if KO
    /// <para>MC 종료 코드를 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the MC end code.</para>
    /// \endif
    /// </summary>
    public ushort EndCode { get; }

    /// <summary>
    /// \if KO
    /// <para>응답 데이터 페이로드를 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the response data payload.</para>
    /// \endif
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// \if KO
    /// <para>응답이 성공인지 여부를 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets whether the response indicates success.</para>
    /// \endif
    /// </summary>
    public bool IsSuccess => EndCode == 0x0000;
}
