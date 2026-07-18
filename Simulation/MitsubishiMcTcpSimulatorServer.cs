using System.Net;
using System.Net.Sockets;
using Dreamine.PLC.Core.Memory;

namespace Dreamine.PLC.Mitsubishi.MC.Simulation;

/// <summary>
/// \if KO
/// <para>로컬 및 PC 간 테스트용 최소 Mitsubishi MC Binary 3E TCP 서버를 제공합니다.</para>
/// \endif
/// \if EN
/// <para>Provides a minimal Mitsubishi MC Binary 3E TCP server for local and cross-PC tests.</para>
/// \endif
/// </summary>
public sealed class MitsubishiMcTcpSimulatorServer : IAsyncDisposable
{
    /// <summary>
    /// \if KO
    /// <para>options 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the options value.</para>
    /// \endif
    /// </summary>
    private readonly MitsubishiMcSimulatorServerOptions _options;
    /// <summary>
    /// \if KO
    /// <para>protocol 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the protocol value.</para>
    /// \endif
    /// </summary>
    private readonly MitsubishiMcBinary3ESimulatorProtocol _protocol;
    /// <summary>
    /// \if KO
    /// <para>client Tasks 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the client tasks value.</para>
    /// \endif
    /// </summary>
    private readonly List<Task> _clientTasks = [];
    /// <summary>
    /// \if KO
    /// <para>sync Root 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the sync root value.</para>
    /// \endif
    /// </summary>
    private readonly object _syncRoot = new();
    /// <summary>
    /// \if KO
    /// <para>cts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the cts value.</para>
    /// \endif
    /// </summary>
    private CancellationTokenSource? _cts;
    /// <summary>
    /// \if KO
    /// <para>listener 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the listener value.</para>
    /// \endif
    /// </summary>
    private TcpListener? _listener;
    /// <summary>
    /// \if KO
    /// <para>accept Task 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the accept task value.</para>
    /// \endif
    /// </summary>
    private Task? _acceptTask;

    /// <summary>
    /// \if KO
    /// <para>새 메모리와 지정한 옵션으로 TCP 서버를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes the TCP server with new memory and specified options.</para>
    /// \endif
    /// </summary>
    /// <param name="options">
    /// \if KO
    /// <para>시뮬레이터 옵션입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The simulator options.</para>
    /// \endif
    /// </param><exception cref="ArgumentNullException">
    /// \if KO
    /// <para>옵션이 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when options is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public MitsubishiMcTcpSimulatorServer(MitsubishiMcSimulatorServerOptions options)
        : this(options, new InMemoryPlcMemory())
    {
    }

    /// <summary>
    /// \if KO
    /// <para>공유 메모리와 옵션으로 TCP 서버를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes the TCP server with shared memory and options.</para>
    /// \endif
    /// </summary>
    /// <param name="options">
    /// \if KO
    /// <para>옵션입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The options.</para>
    /// \endif
    /// </param><param name="memory">
    /// \if KO
    /// <para>공유 메모리입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The shared memory.</para>
    /// \endif
    /// </param><exception cref="ArgumentNullException">
    /// \if KO
    /// <para>옵션 또는 메모리가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when options or memory is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public MitsubishiMcTcpSimulatorServer(MitsubishiMcSimulatorServerOptions options, InMemoryPlcMemory memory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _protocol = new MitsubishiMcBinary3ESimulatorProtocol(memory ?? throw new ArgumentNullException(nameof(memory)), options);
        _protocol.StatusChanged += OnProtocolStatusChanged;
    }

    /// <summary>
    /// \if KO
    /// <para>서버 상태 메시지가 변경될 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when the server status message changes.</para>
    /// \endif
    /// </summary>
    public event EventHandler<string>? StatusChanged;

    /// <summary>
    /// \if KO
    /// <para>서버 실행 여부를 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets whether the server is running.</para>
    /// \endif
    /// </summary>
    public bool IsRunning => _listener is not null;

    /// <summary>
    /// \if KO
    /// <para>TCP 수신기와 수락 루프를 시작합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Starts the TCP listener and acceptance loop.</para>
    /// \endif
    /// </summary>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>서버 수명 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The server-lifetime token.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>시작 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The start task.</para>
    /// \endif
    /// </returns>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_listener is not null)
        {
            return Task.CompletedTask;
        }

        var address = ParseAddress(_options.Host);
        _listener = new TcpListener(address, _options.Port);
        _listener.Start();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _acceptTask = Task.Run(() => AcceptLoopAsync(_cts.Token), CancellationToken.None);
        StatusChanged?.Invoke(this, $"MC TCP simulator server started. {_options.Host}:{_options.Port}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// \if KO
    /// <para>TCP 수신기를 중지하고 클라이언트 작업 종료를 기다립니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stops the TCP listener and waits for client tasks.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>중지 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The stop task.</para>
    /// \endif
    /// </returns>
    public async Task StopAsync()
    {
        if (_listener is null)
        {
            return;
        }

        _cts?.Cancel();
        _listener.Stop();
        _listener = null;

        if (_acceptTask is not null)
        {
            try
            {
                await _acceptTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        Task[] clientTasks;
        lock (_syncRoot)
        {
            clientTasks = _clientTasks.ToArray();
            _clientTasks.Clear();
        }

        try
        {
            await Task.WhenAll(clientTasks).ConfigureAwait(false);
        }
        catch
        {
        }

        _cts?.Dispose();
        _cts = null;
        StatusChanged?.Invoke(this, "MC TCP simulator server stopped.");
    }

    /// <summary>
    /// \if KO
    /// <para>프로토콜 이벤트 구독을 해제하고 서버를 중지합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Unsubscribes protocol events and stops the server.</para>
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
        _protocol.StatusChanged -= OnProtocolStatusChanged;
        await StopAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// \if KO
    /// <para>TCP 클라이언트를 수락하고 처리 작업을 추적합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Accepts TCP clients and tracks handling tasks.</para>
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
    /// <para>수락 루프입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The acceptance loop.</para>
    /// \endif
    /// </returns>
    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener is not null)
        {
            TcpClient client;

            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            var task = Task.Run(() => HandleClientAsync(client, cancellationToken), CancellationToken.None);
            lock (_syncRoot)
            {
                _clientTasks.RemoveAll(static item => item.IsCompleted);
                _clientTasks.Add(task);
            }
        }
    }

    /// <summary>
    /// \if KO
    /// <para>한 클라이언트의 Binary 3E 프레임을 반복 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Repeatedly handles Binary 3E frames for one client.</para>
    /// \endif
    /// </summary><param name="client">
    /// \if KO
    /// <para>클라이언트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The client.</para>
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
    /// <para>처리 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The handling task.</para>
    /// \endif
    /// </returns>
    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        using (var stream = client.GetStream())
        {
            StatusChanged?.Invoke(this, "MC TCP simulator client connected.");

            while (!cancellationToken.IsCancellationRequested)
            {
                byte[] request;
                try
                {
                    request = await ReceiveBinary3EFrameAsync(stream, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (IOException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                var response = _protocol.Execute(request);
                await stream.WriteAsync(response, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        StatusChanged?.Invoke(this, "MC TCP simulator client disconnected.");
    }

    /// <summary>
    /// \if KO
    /// <para>헤더의 데이터 길이에 맞춰 완전한 Binary 3E 요청을 읽습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Reads a complete Binary 3E request using its header length.</para>
    /// \endif
    /// </summary><param name="stream">
    /// \if KO
    /// <para>스트림입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The stream.</para>
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
    /// <para>요청 프레임입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The request frame.</para>
    /// \endif
    /// </returns><exception cref="InvalidOperationException">
    /// \if KO
    /// <para>데이터 길이가 잘못된 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when data length is invalid.</para>
    /// \endif
    /// </exception><exception cref="IOException">
    /// \if KO
    /// <para>프레임 수신 중 연결이 닫힌 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the connection closes during receipt.</para>
    /// \endif
    /// </exception>
    private static async Task<byte[]> ReceiveBinary3EFrameAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var header = new byte[9];
        await ReadExactlyAsync(stream, header, cancellationToken).ConfigureAwait(false);

        var requestDataLength = header[7] | (header[8] << 8);
        if (requestDataLength < 2)
        {
            throw new InvalidOperationException($"Invalid MC request data length: {requestDataLength}");
        }

        var body = new byte[requestDataLength];
        await ReadExactlyAsync(stream, body, cancellationToken).ConfigureAwait(false);

        var frame = new byte[header.Length + body.Length];
        Buffer.BlockCopy(header, 0, frame, 0, header.Length);
        Buffer.BlockCopy(body, 0, frame, header.Length, body.Length);
        return frame;
    }

    /// <summary>
    /// \if KO
    /// <para>대상 버퍼가 찰 때까지 스트림을 읽습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Reads until the destination buffer is full.</para>
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
    /// <para>버퍼입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The buffer.</para>
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
    /// <para>읽기 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The read task.</para>
    /// \endif
    /// </returns><exception cref="IOException">
    /// \if KO
    /// <para>버퍼가 차기 전에 연결이 닫힌 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when connection closes before the buffer is full.</para>
    /// \endif
    /// </exception>
    private static async Task ReadExactlyAsync(NetworkStream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                throw new IOException("The MC TCP simulator client closed the connection.");
            }

            offset += read;
        }
    }

    /// <summary>
    /// \if KO
    /// <para>바인딩 호스트를 IP 주소로 변환하며 잘못되면 Any를 사용합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Converts a bind host to an IP address, using Any when invalid.</para>
    /// \endif
    /// </summary><param name="host">
    /// \if KO
    /// <para>호스트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The host.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>IP 주소입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The IP address.</para>
    /// \endif
    /// </returns>
    private static IPAddress ParseAddress(string host)
    {
        if (string.IsNullOrWhiteSpace(host) || host == "*" || host == "+")
        {
            return IPAddress.Any;
        }

        return IPAddress.TryParse(host, out var address) ? address : IPAddress.Any;
    }

    /// <summary>
    /// \if KO
    /// <para>프로토콜 상태 메시지를 서버 이벤트로 전달합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Forwards a protocol status message through the server event.</para>
    /// \endif
    /// </summary><param name="sender">
    /// \if KO
    /// <para>이벤트 원본입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The source.</para>
    /// \endif
    /// </param><param name="e">
    /// \if KO
    /// <para>상태 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The status message.</para>
    /// \endif
    /// </param>
    private void OnProtocolStatusChanged(object? sender, string e)
    {
        StatusChanged?.Invoke(this, e);
    }
}
