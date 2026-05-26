namespace Dreamine.PLC.Mitsubishi.MC.Protocol;

/// <summary>
/// Represents a parsed Mitsubishi MC Binary 3E response.
/// </summary>
public sealed class MitsubishiMcBinary3EResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MitsubishiMcBinary3EResponse"/> class.
    /// </summary>
    /// <param name="endCode">The MC protocol end code.</param>
    /// <param name="data">The response data payload.</param>
    public MitsubishiMcBinary3EResponse(ushort endCode, byte[] data)
    {
        EndCode = endCode;
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    /// <summary>
    /// Gets the MC protocol end code.
    /// </summary>
    public ushort EndCode { get; }

    /// <summary>
    /// Gets the response data payload.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Gets whether the response indicates success.
    /// </summary>
    public bool IsSuccess => EndCode == 0x0000;
}