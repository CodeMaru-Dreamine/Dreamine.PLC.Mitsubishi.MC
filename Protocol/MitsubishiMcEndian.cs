namespace Dreamine.PLC.Mitsubishi.MC.Protocol;

/// <summary>
/// Provides little-endian byte writing helpers for Mitsubishi MC binary frames.
/// </summary>
public static class MitsubishiMcEndian
{
    /// <summary>
    /// Writes a 16-bit unsigned integer as little-endian bytes.
    /// </summary>
    /// <param name="buffer">The target buffer.</param>
    /// <param name="value">The value to write.</param>
    public static void WriteUInt16LittleEndian(ICollection<byte> buffer, ushort value)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        buffer.Add((byte)(value & 0xFF));
        buffer.Add((byte)((value >> 8) & 0xFF));
    }

    /// <summary>
    /// Writes a 24-bit unsigned integer as little-endian bytes.
    /// </summary>
    /// <param name="buffer">The target buffer.</param>
    /// <param name="value">The value to write.</param>
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