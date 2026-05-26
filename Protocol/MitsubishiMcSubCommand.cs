namespace Dreamine.PLC.Mitsubishi.MC.Protocol;

/// <summary>
/// Defines Mitsubishi MC protocol sub-command codes.
/// </summary>
public enum MitsubishiMcSubCommand : ushort
{
    /// <summary>
    /// Word device access.
    /// </summary>
    Word = 0x0000,

    /// <summary>
    /// Bit device access.
    /// </summary>
    Bit = 0x0001
}