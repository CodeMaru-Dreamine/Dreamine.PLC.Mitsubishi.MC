using Dreamine.PLC.Abstractions.Results;

namespace Dreamine.PLC.Mitsubishi.MC.Protocol;

/// <summary>
/// Parses Mitsubishi MC protocol Binary 3E response frames.
/// </summary>
public sealed class MitsubishiMcBinary3EResponseParser
{
    private const int MinimumResponseLength = 11;

    /// <summary>
    /// Parses a raw Binary 3E response frame.
    /// </summary>
    /// <param name="frame">The response frame bytes.</param>
    /// <returns>The parsed Binary 3E response.</returns>
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
    /// Parses word values from a Binary 3E read response frame.
    /// </summary>
    /// <param name="frame">The response frame bytes.</param>
    /// <param name="count">The expected word count.</param>
    /// <returns>The parsed word values.</returns>
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
    /// Parses bit values from a Binary 3E read response frame.
    /// </summary>
    /// <param name="frame">The response frame bytes.</param>
    /// <param name="count">The expected bit count.</param>
    /// <returns>The parsed bit values.</returns>
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

    private static ushort ReadUInt16LittleEndian(ReadOnlySpan<byte> buffer, int offset)
    {
        return (ushort)(buffer[offset] | (buffer[offset + 1] << 8));
    }
}