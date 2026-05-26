using Dreamine.PLC.Mitsubishi.MC.Protocol;

namespace Dreamine.PLC.Mitsubishi.MC.Options;

/// <summary>
/// Represents connection options for Mitsubishi MC protocol communication.
/// </summary>
public sealed class MitsubishiMcConnectionOptions
{
    /// <summary>
    /// Gets or sets the PLC host address.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the PLC port.
    /// </summary>
    public int Port { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the network number.
    /// </summary>
    public byte NetworkNumber { get; set; } = 0x00;

    /// <summary>
    /// Gets or sets the PLC number.
    /// </summary>
    public byte PlcNumber { get; set; } = 0xFF;

    /// <summary>
    /// Gets or sets the request destination module I/O number.
    /// </summary>
    public ushort DestinationModuleIoNumber { get; set; } = 0x03FF;

    /// <summary>
    /// Gets or sets the request destination module station number.
    /// </summary>
    public byte DestinationModuleStationNumber { get; set; } = 0x00;

    /// <summary>
    /// Gets or sets the monitoring timer.
    /// </summary>
    public ushort MonitoringTimer { get; set; } = 0x0010;

    /// <summary>
    /// Gets or sets the connection timeout in milliseconds.
    /// </summary>
    public int ConnectTimeoutMs { get; set; } = 3000;

    /// <summary>
    /// Gets or sets the send timeout in milliseconds.
    /// </summary>
    public int SendTimeoutMs { get; set; } = 3000;

    /// <summary>
    /// Gets or sets the receive timeout in milliseconds.
    /// </summary>
    public int ReceiveTimeoutMs { get; set; } = 3000;

    /// <summary>
    /// Gets or sets the MC frame format.
    /// </summary>
    public MitsubishiMcFrameFormat FrameFormat { get; set; } = MitsubishiMcFrameFormat.Binary3E;
}