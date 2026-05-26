using System.Net.Sockets;
using Dreamine.PLC.Abstractions.Results;

namespace Dreamine.PLC.Mitsubishi.MC.Transport;

/// <summary>
/// Provides a TCP transport implementation for Mitsubishi MC protocol communication.
/// </summary>
public sealed class TcpMitsubishiMcTransport : IMitsubishiMcTransport
{
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private bool _disposed;

    /// <inheritdoc />
    public bool IsConnected => _tcpClient?.Connected == true && _stream is not null;

    /// <inheritdoc />
    public async Task<PlcResult> ConnectAsync(
        string host,
        int port,
        int timeoutMs,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);

        if (port is <= 0 or > 65535)
        {
            return PlcResult.Failure($"Invalid TCP port: {port}");
        }

        if (timeoutMs <= 0)
        {
            return PlcResult.Failure("The connection timeout must be greater than zero.");
        }

        await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            ThrowIfDisposed();

            if (IsConnected)
            {
                return PlcResult.Success();
            }

            await CloseCoreAsync().ConfigureAwait(false);

            _tcpClient = new TcpClient
            {
                NoDelay = true
            };

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeoutMs);

            await _tcpClient.ConnectAsync(host, port, timeoutCts.Token).ConfigureAwait(false);
            _stream = _tcpClient.GetStream();

            return PlcResult.Success();
        }
        catch (OperationCanceledException)
        {
            await CloseCoreAsync().ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            await CloseCoreAsync().ConfigureAwait(false);
            return PlcResult.Failure(ex.Message);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<PlcResult> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await CloseCoreAsync().ConfigureAwait(false);
            return PlcResult.Success();
        }
        catch (Exception ex)
        {
            return PlcResult.Failure(ex.Message);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<PlcResult<byte[]>> SendAndReceiveAsync(
        IReadOnlyList<byte> requestFrame,
        int receiveTimeoutMs,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestFrame);

        if (requestFrame.Count == 0)
        {
            return PlcResult<byte[]>.Failure("The request frame must not be empty.");
        }

        if (receiveTimeoutMs <= 0)
        {
            return PlcResult<byte[]>.Failure("The receive timeout must be greater than zero.");
        }

        await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            ThrowIfDisposed();

            if (_stream is null || _tcpClient is null || !_tcpClient.Connected)
            {
                return PlcResult<byte[]>.Failure("The TCP transport is not connected.");
            }

            var requestBuffer = requestFrame as byte[] ?? requestFrame.ToArray();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(receiveTimeoutMs);

            await _stream.WriteAsync(requestBuffer, timeoutCts.Token).ConfigureAwait(false);
            await _stream.FlushAsync(timeoutCts.Token).ConfigureAwait(false);

            var response = await ReceiveBinary3EFrameAsync(_stream, timeoutCts.Token).ConfigureAwait(false);

            return PlcResult<byte[]>.Success(response);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return PlcResult<byte[]>.Failure(ex.Message);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await DisconnectAsync().ConfigureAwait(false);

        _syncLock.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    private static async Task<byte[]> ReceiveBinary3EFrameAsync(
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        var header = new byte[9];

        await ReadExactlyAsync(stream, header, cancellationToken).ConfigureAwait(false);

        var responseDataLength = header[7] | (header[8] << 8);
        if (responseDataLength < 2)
        {
            throw new InvalidOperationException(
                $"Invalid Mitsubishi MC response data length: {responseDataLength}");
        }

        var body = new byte[responseDataLength];

        await ReadExactlyAsync(stream, body, cancellationToken).ConfigureAwait(false);

        var frame = new byte[header.Length + body.Length];

        Buffer.BlockCopy(header, 0, frame, 0, header.Length);
        Buffer.BlockCopy(body, 0, frame, header.Length, body.Length);

        return frame;
    }

    private static async Task ReadExactlyAsync(
        NetworkStream stream,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        var offset = 0;

        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(
                buffer.AsMemory(offset, buffer.Length - offset),
                cancellationToken).ConfigureAwait(false);

            if (read == 0)
            {
                throw new IOException("The remote PLC closed the TCP connection.");
            }

            offset += read;
        }
    }

    private Task CloseCoreAsync()
    {
        try
        {
            _stream?.Close();
            _stream?.Dispose();

            _tcpClient?.Close();
            _tcpClient?.Dispose();
        }
        finally
        {
            _stream = null;
            _tcpClient = null;
        }

        return Task.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, GetType().Name);
    }
}