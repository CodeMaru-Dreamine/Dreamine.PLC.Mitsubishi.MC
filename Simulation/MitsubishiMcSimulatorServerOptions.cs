namespace Dreamine.PLC.Mitsubishi.MC.Simulation;

/// <summary>
/// Defines options for the Mitsubishi MC protocol simulator server.
/// </summary>
public sealed class MitsubishiMcSimulatorServerOptions
{
    /// <summary>
    /// Gets or sets the server bind address.
    /// Use 127.0.0.1 for same-PC tests and 0.0.0.0 for cross-PC tests.
    /// </summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>
    /// Gets or sets the server port.
    /// </summary>
    public int Port { get; set; } = 55000;

    /// <summary>
    /// Gets or sets whether a single word write to the trigger address updates the response address.
    /// </summary>
    public bool EnableAutoWordResponse { get; set; } = true;

    /// <summary>
    /// Gets or sets the device code used as the handshake trigger device.
    /// </summary>
    public byte AutoResponseTriggerDeviceCode { get; set; } = 0xA8;

    /// <summary>
    /// Gets or sets the trigger word offset.
    /// </summary>
    public int AutoResponseTriggerOffset { get; set; } = 100;

    /// <summary>
    /// Gets or sets the device code used as the handshake response device.
    /// </summary>
    public byte AutoResponseDeviceCode { get; set; } = 0xA8;

    /// <summary>
    /// Gets or sets the response word offset.
    /// </summary>
    public int AutoResponseOffset { get; set; } = 101;

    /// <summary>
    /// Gets or sets the value increment used for the automatic handshake response.
    /// </summary>
    public short AutoResponseIncrement { get; set; } = 1;
}
