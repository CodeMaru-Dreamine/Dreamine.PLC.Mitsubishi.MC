using Dreamine.PLC.Abstractions.Results;

namespace Dreamine.PLC.Mitsubishi.MC.Transport;

/// <summary>
/// Defines a transport boundary for Mitsubishi MC protocol communication.
/// </summary>
public interface IMitsubishiMcTransport : IAsyncDisposable
{
    /// <summary>
    /// Gets whether the transport is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects the transport.
    /// </summary>
    /// <param name="host">The target host address.</param>
    /// <param name="port">The target port.</param>
    /// <param name="timeoutMs">The connection timeout in milliseconds.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The PLC operation result.</returns>
    Task<PlcResult> ConnectAsync(
        string host,
        int port,
        int timeoutMs,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects the transport.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The PLC operation result.</returns>
    Task<PlcResult> DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a request frame and receives the response frame.
    /// </summary>
    /// <param name="requestFrame">The request frame bytes.</param>
    /// <param name="receiveTimeoutMs">The receive timeout in milliseconds.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The received response frame bytes.</returns>
    Task<PlcResult<byte[]>> SendAndReceiveAsync(
        IReadOnlyList<byte> requestFrame,
        int receiveTimeoutMs,
        CancellationToken cancellationToken = default);
}