namespace Dreamine.PLC.Mitsubishi.MC.Protocol;

/// <summary>
/// Defines Mitsubishi MC protocol frame formats.
/// </summary>
public enum MitsubishiMcFrameFormat
{
    /// <summary>
    /// Binary 3E frame.
    /// </summary>
    Binary3E = 0,

    /// <summary>
    /// ASCII 3E frame.
    /// </summary>
    Ascii3E = 1
}