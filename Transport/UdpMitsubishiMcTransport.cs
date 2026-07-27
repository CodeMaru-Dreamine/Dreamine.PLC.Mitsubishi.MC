using System.Net;
using System.Net.Sockets;
using Dreamine.PLC.Abstractions.Results;

namespace Dreamine.PLC.Mitsubishi.MC.Transport;

/// <summary>
/// \if KO
/// <para>Mitsubishi MC 프로토콜용 UDP 전송을 제공합니다.</para>
/// \endif
/// \if EN
/// <para>Provides UDP transport for Mitsubishi MC communication.</para>
/// \endif
/// </summary>
public sealed class UdpMitsubishiMcTransport : IMitsubishiMcTransport
{
    /// <summary>
    /// \if KO
    /// <para>Maximum Mc Response Frame Length 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the maximum mc response frame length value.</para>
    /// \endif
    /// </summary>
    private const int MaximumMcResponseFrameLength = 8192;
    /// <summary>
    /// \if KO
    /// <para>sync Lock 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the sync lock value.</para>
    /// \endif
    /// </summary>
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    /// <summary>
    /// \if KO
    /// <para>udp Client 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the udp client value.</para>
    /// \endif
    /// </summary>
    private UdpClient? _udpClient;
    /// <summary>
    /// \if KO
    /// <para>remote End Point 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the remote end point value.</para>
    /// \endif
    /// </summary>
    private IPEndPoint? _remoteEndPoint;
    /// <summary>
    /// \if KO
    /// <para>disposed 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the disposed value.</para>
    /// \endif
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// \if KO
    /// <para>UDP 클라이언트와 원격 끝점 준비 여부를 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets whether the UDP client and remote endpoint are ready.</para>
    /// \endif
    /// </summary>
    public bool IsConnected => _udpClient is not null && _remoteEndPoint is not null;

    /// <summary>
    /// \if KO
    /// <para>호스트를 확인하고 연결된 UDP 클라이언트를 준비합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Resolves the host and prepares a connected UDP client.</para>
    /// \endif
    /// </summary><param name="host">
    /// \if KO
    /// <para>호스트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The host.</para>
    /// \endif
    /// </param><param name="port">
    /// \if KO
    /// <para>포트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The port.</para>
    /// \endif
    /// </param><param name="timeoutMs">
    /// \if KO
    /// <para>유효성 검사에 사용하는 연결 제한 시간입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The connection timeout used for validation.</para>
    /// \endif
    /// </param><param name="cancellationToken">
    /// \if KO
    /// <para>취소 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The cancellation token.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>준비 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The preparation result.</para>
    /// \endif
    /// </returns><exception cref="ArgumentException">
    /// \if KO
    /// <para>호스트가 비어 있는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the host is empty.</para>
    /// \endif
    /// </exception><exception cref="OperationCanceledException">
    /// \if KO
    /// <para>잠금 대기 중 취소된 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when canceled while waiting for the lock.</para>
    /// \endif
    /// </exception>
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

    /// <summary>
    /// \if KO
    /// <para>UDP 클라이언트와 원격 끝점 정보를 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Releases the UDP client and remote endpoint information.</para>
    /// \endif
    /// </summary><param name="cancellationToken">
    /// \if KO
    /// <para>취소 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The cancellation token.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>연결 해제 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The disconnection result.</para>
    /// \endif
    /// </returns><exception cref="OperationCanceledException">
    /// \if KO
    /// <para>잠금 대기 중 취소된 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when canceled while waiting for the lock.</para>
    /// \endif
    /// </exception>
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

    /// <summary>
    /// \if KO
    /// <para>UDP 요청을 보내고 제한 시간 실패 시 구성된 횟수만큼 재시도합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sends a UDP request and retries timeout failures as configured.</para>
    /// \endif
    /// </summary><param name="requestFrame">
    /// \if KO
    /// <para>요청 프레임입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The request frame.</para>
    /// \endif
    /// </param><param name="receiveTimeoutMs">
    /// \if KO
    /// <para>수신 제한 시간입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The receive timeout.</para>
    /// \endif
    /// </param><param name="retryCount">
    /// \if KO
    /// <para>재시도 횟수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The retry count.</para>
    /// \endif
    /// </param><param name="cancellationToken">
    /// \if KO
    /// <para>취소 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The cancellation token.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>응답 데이터그램 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The response-datagram result.</para>
    /// \endif
    /// </returns><exception cref="ArgumentNullException">
    /// \if KO
    /// <para>요청이 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the request is <see langword="null"/>.</para>
    /// \endif
    /// </exception><exception cref="OperationCanceledException">
    /// \if KO
    /// <para>잠금 대기 중 취소된 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when canceled while waiting for the lock.</para>
    /// \endif
    /// </exception>
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

    /// <summary>
    /// \if KO
    /// <para>UDP 클라이언트와 동기화 리소스를 비동기 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Asynchronously releases the UDP client and synchronization resources.</para>
    /// \endif
    /// </summary><returns>
    /// \if KO
    /// <para>비동기 해제 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The asynchronous disposal operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>내부 UDP 클라이언트를 닫고 참조를 지웁니다.</para>
    /// \endif
    /// \if EN
    /// <para>Closes the internal UDP client and clears references.</para>
    /// \endif
    /// </summary><returns>
    /// \if KO
    /// <para>완료된 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A completed task.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>전송이 이미 해제되었으면 예외를 발생시킵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Throws when the transport is disposed.</para>
    /// \endif
    /// </summary><exception cref="ObjectDisposedException">
    /// \if KO
    /// <para>해제된 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when disposed.</para>
    /// \endif
    /// </exception>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, GetType().Name);
    }
}
