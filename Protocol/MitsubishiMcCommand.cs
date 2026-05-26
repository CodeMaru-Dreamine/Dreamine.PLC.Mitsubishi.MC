namespace Dreamine.PLC.Mitsubishi.MC.Protocol;

/// <summary>
/// Defines Mitsubishi MC protocol command codes.
/// </summary>
public enum MitsubishiMcCommand : ushort
{
    /// <summary>
    /// Batch read command.
    /// </summary>
    BatchRead = 0x0401,

    /// <summary>
    /// Batch write command.
    /// </summary>
    BatchWrite = 0x1401
}