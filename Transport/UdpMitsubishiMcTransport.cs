using System.Net;
using System.Net.Sockets;
using Dreamine.PLC.Abstractions.Results;

namespace Dreamine.PLC.Mitsubishi.MC.Transport;

/// <summary>
/// Provides a UDP transport implementation for Mitsubishi MC protocol communication.
/// </summary>
public sealed class UdpMitsubishiMcTransport : IMitsubishiMcTransport
{
    private const int MaximumMcResponseFrameLength = 8192;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private UdpClient? _udpClient;
    private IPEndPoint? _remoteEndPoint;
    private bool _disposed;

    /// <inheritdoc />
    public bool IsConnected => _udpClient is not null && _remoteEndPoint is not null;

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
            return PlcResult.Failure($"Invalid UDP port: {port}");
        }

        if (timeoutMs <= 0)
        {
            return PlcResult.Failure("The connection timeout must be greater than zero.");
        }

        await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            ThrowIfDisposed();

            await CloseCoreAsync().ConfigureAwait(false);

            var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
            var address = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                ?? addresses.FirstOrDefault();

            if (address is null)
            {
                return PlcResult.Failure($"Failed to resolve UDP host: {host}");
            }

            _remoteEndPoint = new IPEndPoint(address, port);
            _udpClient = new UdpClient(address.AddressFamily);
            _udpClient.Connect(_remoteEndPoint);

            return PlcResult.Success();
        }
        catch (OperationCanceledException ex)
        {
            await CloseCoreAsync().ConfigureAwait(false);
            return PlcResult.Failure(ex.Message);
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
        int retryCount,
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

            if (_udpClient is null || _remoteEndPoint is null)
            {
                return PlcResult<byte[]>.Failure("The UDP transport is not ready.");
            }

            var attempts = Math.Max(1, retryCount);
            var requestBuffer = requestFrame as byte[] ?? requestFrame.ToArray();
            Exception? lastException = null;

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(receiveTimeoutMs);

                try
                {
                    await _udpClient.SendAsync(requestBuffer, requestBuffer.Length).WaitAsync(timeoutCts.Token).ConfigureAwait(false);
                    var response = await _udpClient.ReceiveAsync().WaitAsync(timeoutCts.Token).ConfigureAwait(false);

                    if (response.Buffer.Length == 0)
                    {
                        lastException = new IOException("The UDP PLC response was empty.");
                        continue;
                    }

                    if (response.Buffer.Length > MaximumMcResponseFrameLength)
                    {
                        return PlcResult<byte[]>.Failure(
                            $"The UDP PLC response frame is too large. Length: {response.Buffer.Length}");
                    }

                    return PlcResult<byte[]>.Success(response.Buffer);
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    lastException = ex;
                }
                catch (SocketException ex)
                {
                    lastException = ex;
                }
                catch (IOException ex)
                {
                    lastException = ex;
                }
            }

            return PlcResult<byte[]>.Failure(
                lastException?.Message ?? "The UDP PLC request timed out.");
        }
        catch (OperationCanceledException ex)
        {
            return PlcResult<byte[]>.Failure(ex.Message);
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

    private Task CloseCoreAsync()
    {
        try
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
        }
        finally
        {
            _udpClient = null;
            _remoteEndPoint = null;
        }

        return Task.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, GetType().Name);
    }
}
