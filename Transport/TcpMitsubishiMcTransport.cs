using System.Net.Sockets;
using Dreamine.PLC.Abstractions.Results;

namespace Dreamine.PLC.Mitsubishi.MC.Transport;

/// <summary>
/// \if KO
/// <para>Mitsubishi MC 프로토콜용 TCP 전송을 제공합니다.</para>
/// \endif
/// \if EN
/// <para>Provides TCP transport for Mitsubishi MC communication.</para>
/// \endif
/// </summary>
public sealed class TcpMitsubishiMcTransport : IMitsubishiMcTransport
{
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
    /// <para>tcp Client 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the tcp client value.</para>
    /// \endif
    /// </summary>
    private TcpClient? _tcpClient;
    /// <summary>
    /// \if KO
    /// <para>stream 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the stream value.</para>
    /// \endif
    /// </summary>
    private NetworkStream? _stream;
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
    /// <para>TCP 클라이언트와 스트림의 연결 여부를 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets whether the TCP client and stream are connected.</para>
    /// \endif
    /// </summary>
    public bool IsConnected => _tcpClient?.Connected == true && _stream is not null;

    /// <summary>
    /// \if KO
    /// <para>제한 시간 내 대상 TCP 끝점에 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Connects to the target TCP endpoint within the timeout.</para>
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
    /// <para>제한 시간입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The timeout.</para>
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
    /// <para>연결 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The connection result.</para>
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
    /// <para>동기화 잠금 대기 중 취소된 경우 발생합니다.</para>
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
    /// <para>TCP 스트림과 클라이언트를 닫습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Closes the TCP stream and client.</para>
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
    /// <para>Binary 3E 요청을 보내고 길이 헤더에 따라 완전한 응답을 읽습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sends a Binary 3E request and reads the complete length-prefixed response.</para>
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
    /// <para>TCP에서 사용하지 않는 재시도 수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The retry count unused by TCP.</para>
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
    /// <para>응답 프레임 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The response-frame result.</para>
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
    /// <para>연결과 동기화 리소스를 비동기 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Asynchronously releases connection and synchronization resources.</para>
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
    /// <para>Binary 3E 헤더와 선언된 길이의 본문을 정확히 읽습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Reads the Binary 3E header and exactly its declared body length.</para>
    /// \endif
    /// </summary><param name="stream">
    /// \if KO
    /// <para>네트워크 스트림입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The network stream.</para>
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
    /// <para>완전한 응답 프레임입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The complete response frame.</para>
    /// \endif
    /// </returns><exception cref="InvalidOperationException">
    /// \if KO
    /// <para>응답 데이터 길이가 2 미만인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when response data length is below two.</para>
    /// \endif
    /// </exception><exception cref="IOException">
    /// \if KO
    /// <para>응답 도중 원격 연결이 닫힌 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the remote closes during response.</para>
    /// \endif
    /// </exception>
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

    /// <summary>
    /// \if KO
    /// <para>버퍼가 찰 때까지 스트림을 반복해서 읽습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Repeatedly reads until the buffer is full.</para>
    /// \endif
    /// </summary><param name="stream">
    /// \if KO
    /// <para>스트림입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The stream.</para>
    /// \endif
    /// </param><param name="buffer">
    /// \if KO
    /// <para>대상 버퍼입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The destination buffer.</para>
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
    /// <para>비동기 읽기 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The asynchronous read operation.</para>
    /// \endif
    /// </returns><exception cref="IOException">
    /// \if KO
    /// <para>버퍼가 차기 전에 연결이 닫힌 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the connection closes before the buffer is full.</para>
    /// \endif
    /// </exception>
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

    /// <summary>
    /// \if KO
    /// <para>내부 TCP 스트림과 클라이언트를 닫고 참조를 지웁니다.</para>
    /// \endif
    /// \if EN
    /// <para>Closes the internal TCP stream and client and clears references.</para>
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
