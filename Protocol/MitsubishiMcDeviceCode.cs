namespace Dreamine.PLC.Mitsubishi.MC.Protocol;

/// <summary>
/// Defines Mitsubishi MC protocol device codes for binary frames.
/// </summary>
public enum MitsubishiMcDeviceCode : byte
{
    /// <summary>
    /// Special relay.
    /// </summary>
    SM = 0x91,

    /// <summary>
    /// Special register.
    /// </summary>
    SD = 0xA9,

    /// <summary>
    /// Input relay.
    /// </summary>
    X = 0x9C,

    /// <summary>
    /// Output relay.
    /// </summary>
    Y = 0x9D,

    /// <summary>
    /// Internal relay.
    /// </summary>
    M = 0x90,

    /// <summary>
    /// Data register.
    /// </summary>
    D = 0xA8,

    /// <summary>
    /// Link relay.
    /// </summary>
    B = 0xA0,

    /// <summary>
    /// Link register.
    /// </summary>
    W = 0xB4,

    /// <summary>
    /// File register.
    /// </summary>
    R = 0xAF,

    /// <summary>
    /// File register ZR.
    /// </summary>
    ZR = 0xB0
}