namespace Dreamine.PLC.Mitsubishi.MC.Protocol;

/// <summary>
/// \if KO
/// <para>Mitsubishi MC 바이너리 프레임용 리틀 엔디언 쓰기 도우미를 제공합니다.</para>
/// \endif
/// \if EN
/// <para>Provides little-endian writing helpers for Mitsubishi MC binary frames.</para>
/// \endif
/// </summary>
public static class MitsubishiMcEndian
{
    /// <summary>
    /// \if KO
    /// <para>16비트 부호 없는 정수를 리틀 엔디언 바이트로 씁니다.</para>
    /// \endif
    /// \if EN
    /// <para>Writes a 16-bit unsigned integer as little-endian bytes.</para>
    /// \endif
    /// </summary>
    /// <param name="buffer">
    /// \if KO
    /// <para>대상 버퍼입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target buffer.</para>
    /// \endif
    /// </param><param name="value">
    /// \if KO
    /// <para>쓸 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to write.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para><paramref name="buffer"/>가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="buffer"/> is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public static void WriteUInt16LittleEndian(ICollection<byte> buffer, ushort value)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        buffer.Add((byte)(value & 0xFF));
        buffer.Add((byte)((value >> 8) & 0xFF));
    }

    /// <summary>
    /// \if KO
    /// <para>24비트 부호 없는 정수를 리틀 엔디언 바이트로 씁니다.</para>
    /// \endif
    /// \if EN
    /// <para>Writes a 24-bit unsigned integer as little-endian bytes.</para>
    /// \endif
    /// </summary>
    /// <param name="buffer">
    /// \if KO
    /// <para>대상 버퍼입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target buffer.</para>
    /// \endif
    /// </param><param name="value">
    /// \if KO
    /// <para>쓸 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to write.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para><paramref name="buffer"/>가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="buffer"/> is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// \if KO
    /// <para>값이 0~0xFFFFFF 범위를 벗어난 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the value is outside 0 through 0xFFFFFF.</para>
    /// \endif
    /// </exception>
    public static void WriteUInt24LittleEndian(ICollection<byte> buffer, int value)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (value is < 0 or > 0xFFFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "The value must be between 0 and 0xFFFFFF.");
        }

        buffer.Add((byte)(value & 0xFF));
        buffer.Add((byte)((value >> 8) & 0xFF));
        buffer.Add((byte)((value >> 16) & 0xFF));
    }
}
