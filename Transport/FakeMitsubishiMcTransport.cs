using Dreamine.PLC.Abstractions.Results;

namespace Dreamine.PLC.Mitsubishi.MC.Transport;

/// <summary>
/// Provides a fake Mitsubishi MC transport for tests and protocol verification.
/// </summary>
public sealed class FakeMitsubishiMcTransport : IMitsubishiMcTransport
{
    private readonly Queue<byte[]> _responses = new();
    private readonly List<byte[]> _sentFrames = new();

    /// <inheritdoc />
    public bool IsConnected { get; private set; }

    /// <summary>
    /// Gets the frames sent through this fake transport.
    /// </summary>
    public IReadOnlyList<byte[]> SentFrames => _sentFrames;

    /// <summary>
    /// Enqueues a response frame to return from the next send/receive operation.
    /// </summary>
    /// <param name="responseFrame">The response frame bytes.</param>
    public void EnqueueResponse(byte[] responseFrame)
    {
        ArgumentNullException.ThrowIfNull(responseFrame);

        _responses.Enqueue(responseFrame);
    }

    /// <inheritdoc />
    public Task<PlcResult> ConnectAsync(
        string host,
        int port,
        int timeoutMs,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IsConnected = true;

        return Task.FromResult(PlcResult.Success());
    }

    /// <inheritdoc />
    public Task<PlcResult> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IsConnected = false;

        return Task.FromResult(PlcResult.Success());
    }

    /// <inheritdoc />
    public Task<PlcResult<byte[]>> SendAndReceiveAsync(
        IReadOnlyList<byte> requestFrame,
        int receiveTimeoutMs,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestFrame);

        cancellationToken.ThrowIfCancellationRequested();

        if (!IsConnected)
        {
            return Task.FromResult(
                PlcResult<byte[]>.Failure("The fake Mitsubishi MC transport is not connected."));
        }

        if (requestFrame.Count == 0)
        {
            return Task.FromResult(
                PlcResult<byte[]>.Failure("The request frame must not be empty."));
        }

        _sentFrames.Add(requestFrame as byte[] ?? requestFrame.ToArray());

        if (_responses.Count == 0)
        {
            return Task.FromResult(
                PlcResult<byte[]>.Failure("No fake Mitsubishi MC response frame was queued."));
        }

        return Task.FromResult(
            PlcResult<byte[]>.Success(_responses.Dequeue()));
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        IsConnected = false;
        _responses.Clear();
        _sentFrames.Clear();

        return ValueTask.CompletedTask;
    }
}