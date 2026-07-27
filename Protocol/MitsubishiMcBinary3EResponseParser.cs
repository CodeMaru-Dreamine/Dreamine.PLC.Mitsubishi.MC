using Dreamine.PLC.Abstractions.Results;

namespace Dreamine.PLC.Mitsubishi.MC.Protocol;

/// <summary>
/// \if KO
/// <para>Mitsubishi MC Binary 3E 응답 프레임을 구문 분석합니다.</para>
/// \endif
/// \if EN
/// <para>Parses Mitsubishi MC Binary 3E response frames.</para>
/// \endif
/// </summary>
public sealed class MitsubishiMcBinary3EResponseParser
{
    /// <summary>
    /// \if KO
    /// <para>Minimum Response Length 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the minimum response length value.</para>
    /// \endif
    /// </summary>
    private const int MinimumResponseLength = 11;

    /// <summary>
    /// \if KO
    /// <para>원시 Binary 3E 응답의 헤더, 길이, 종료 코드 및 데이터를 구문 분석합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Parses header, length, end code, and data from a raw Binary 3E response.</para>
    /// \endif
    /// </summary>
    /// <param name="frame">
    /// \if KO
    /// <para>응답 프레임입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The response frame.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>구문 분석된 응답 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The parsed response result.</para>
    /// \endif
    /// </returns>
    public PlcResult<MitsubishiMcBinary3EResponse> Parse(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < MinimumResponseLength)
        {
            return PlcResult<MitsubishiMcBinary3EResponse>.Failure(
                $"The Mitsubishi MC response frame is too short. Length: {frame.Length}");
        }

        var subHeader = ReadUInt16LittleEndian(frame, 0);
        if (subHeader != 0xD000)
        {
            return PlcResult<MitsubishiMcBinary3EResponse>.Failure(
                $"Invalid Mitsubishi MC response sub-header: 0x{subHeader:X4}");
        }

        var responseDataLength = ReadUInt16LittleEndian(frame, 7);
        var expectedFrameLength = 9 + responseDataLength;

        if (frame.Length < expectedFrameLength)
        {
            return PlcResult<MitsubishiMcBinary3EResponse>.Failure(
                $"The Mitsubishi MC response frame is incomplete. Expected: {expectedFrameLength}, Actual: {frame.Length}");
        }

        var endCode = ReadUInt16LittleEndian(frame, 9);
        var dataLength = responseDataLength - 2;

        var data = new byte[dataLength];
        if (dataLength > 0)
        {
            frame.Slice(11, dataLength).CopyTo(data);
        }

        var response = new MitsubishiMcBinary3EResponse(endCode, data);

        if (!response.IsSuccess)
        {
            return PlcResult<MitsubishiMcBinary3EResponse>.Failure(
                $"Mitsubishi MC response returned error end code: 0x{endCode:X4}",
                endCode);
        }

        return PlcResult<MitsubishiMcBinary3EResponse>.Success(response);
    }

    /// <summary>
    /// \if KO
    /// <para>Binary 3E 읽기 응답에서 워드 값을 구문 분석합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Parses word values from a Binary 3E read response.</para>
    /// \endif
    /// </summary>
    /// <param name="frame">
    /// \if KO
    /// <para>응답 프레임입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The response frame.</para>
    /// \endif
    /// </param><param name="count">
    /// \if KO
    /// <para>예상 워드 수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The expected word count.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>워드 값 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The word-value result.</para>
    /// \endif
    /// </returns>
    public PlcResult<short[]> ParseReadWords(ReadOnlySpan<byte> frame, int count)
    {
        if (count <= 0)
        {
            return PlcResult<short[]>.Failure("The word count must be greater than zero.");
        }

        var responseResult = Parse(frame);
        if (!responseResult.IsSuccess || responseResult.Value is null)
        {
            return PlcResult<short[]>.Failure(
                responseResult.Message ?? "Failed to parse Mitsubishi MC read word response.",
                responseResult.ErrorCode);
        }

        var data = responseResult.Value.Data;
        var expectedDataLength = count * 2;

        if (data.Length < expectedDataLength)
        {
            return PlcResult<short[]>.Failure(
                $"The Mitsubishi MC word response data is incomplete. Expected: {expectedDataLength}, Actual: {data.Length}");
        }

        var values = new short[count];

        for (var i = 0; i < count; i++)
        {
            values[i] = unchecked((short)ReadUInt16LittleEndian(data, i * 2));
        }

        return PlcResult<short[]>.Success(values);
    }

    /// <summary>
    /// \if KO
    /// <para>Binary 3E 읽기 응답에서 압축된 비트 값을 구문 분석합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Parses packed bit values from a Binary 3E read response.</para>
    /// \endif
    /// </summary>
    /// <param name="frame">
    /// \if KO
    /// <para>응답 프레임입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The response frame.</para>
    /// \endif
    /// </param><param name="count">
    /// \if KO
    /// <para>예상 비트 수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The expected bit count.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>비트 값 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The bit-value result.</para>
    /// \endif
    /// </returns>
    public PlcResult<bool[]> ParseReadBits(ReadOnlySpan<byte> frame, int count)
    {
        if (count <= 0)
        {
            return PlcResult<bool[]>.Failure("The bit count must be greater than zero.");
        }

        var responseResult = Parse(frame);
        if (!responseResult.IsSuccess || responseResult.Value is null)
        {
            return PlcResult<bool[]>.Failure(
                responseResult.Message ?? "Failed to parse Mitsubishi MC read bit response.",
                responseResult.ErrorCode);
        }

        var data = responseResult.Value.Data;
        var expectedDataLength = (count + 1) / 2;

        if (data.Length < expectedDataLength)
        {
            return PlcResult<bool[]>.Failure(
                $"The Mitsubishi MC bit response data is incomplete. Expected: {expectedDataLength}, Actual: {data.Length}");
        }

        var values = new bool[count];

        for (var i = 0; i < count; i++)
        {
            var packed = data[i / 2];

            values[i] = i % 2 == 0
                ? (packed & 0x10) != 0
                : (packed & 0x01) != 0;
        }

        return PlcResult<bool[]>.Success(values);
    }

    /// <summary>
    /// \if KO
    /// <para>지정한 오프셋에서 리틀 엔디언 16비트 값을 읽습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Reads a little-endian 16-bit value at the specified offset.</para>
    /// \endif
    /// </summary><param name="buffer">
    /// \if KO
    /// <para>원본 버퍼입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The source buffer.</para>
    /// \endif
    /// </param><param name="offset">
    /// \if KO
    /// <para>읽기 오프셋입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The read offset.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>읽은 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value read.</para>
    /// \endif
    /// </returns>
    private static ushort ReadUInt16LittleEndian(ReadOnlySpan<byte> buffer, int offset)
    {
        return (ushort)(buffer[offset] | (buffer[offset + 1] << 8));
    }
}
