namespace Dreamine.PLC.Mitsubishi.MC.Options;

/// <summary>
/// Defines the transport type used by Mitsubishi MC protocol communication.
/// </summary>
public enum MitsubishiMcTransportType
{
    /// <summary>
    /// Uses TCP transport.
    /// </summary>
    Tcp = 0,

    /// <summary>
    /// Uses UDP transport.
    /// </summary>
    Udp = 1
}
