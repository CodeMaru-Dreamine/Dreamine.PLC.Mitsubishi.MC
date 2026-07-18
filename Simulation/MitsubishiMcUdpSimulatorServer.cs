using System.Net;
using System.Net.Sockets;
using Dreamine.PLC.Core.Memory;

namespace Dreamine.PLC.Mitsubishi.MC.Simulation;

/// <summary>
/// \if KO
/// <para>로컬 및 PC 간 테스트용 최소 Mitsubishi MC Binary 3E UDP 서버를 제공합니다.</para>
/// \endif
/// \if EN
/// <para>Provides a minimal Mitsubishi MC Binary 3E UDP server for local and cross-PC tests.</para>
/// \endif
/// </summary>
public sealed class MitsubishiMcUdpSimulatorServer : IAsyncDisposable
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
    /// <para>cts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the cts value.</para>
    /// \endif
    /// </summary>
    private CancellationTokenSource? _cts;
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
    /// <para>receive Task 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the receive task value.</para>
    /// \endif
    /// </summary>
    private Task? _receiveTask;

    /// <summary>
    /// \if KO
    /// <para>새 메모리와 옵션으로 UDP 서버를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes the UDP server with new memory and options.</para>
    /// \endif
    /// </summary>
    /// <param name="options">
    /// \if KO
    /// <para>옵션입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The options.</para>
    /// \endif
    /// </param><exception cref="ArgumentNullException">
    /// \if KO
    /// <para>옵션이 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when options is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public MitsubishiMcUdpSimulatorServer(MitsubishiMcSimulatorServerOptions options)
        : this(options, new InMemoryPlcMemory())
    {
    }

    /// <summary>
    /// \if KO
    /// <para>공유 메모리와 옵션으로 UDP 서버를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes the UDP server with shared memory and options.</para>
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
    public MitsubishiMcUdpSimulatorServer(MitsubishiMcSimulatorServerOptions options, InMemoryPlcMemory memory)
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
    public bool IsRunning => _udpClient is not null;

    /// <summary>
    /// \if KO
    /// <para>UDP 수신기와 수신 루프를 시작합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Starts the UDP receiver and receive loop.</para>
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
        if (_udpClient is not null)
        {
            return Task.CompletedTask;
        }

        var address = ParseAddress(_options.Host);
        _udpClient = new UdpClient(new IPEndPoint(address, _options.Port));
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _receiveTask = Task.Run(() => ReceiveLoopAsync(_cts.Token), CancellationToken.None);
        StatusChanged?.Invoke(this, $"MC UDP simulator server started. {_options.Host}:{_options.Port}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// \if KO
    /// <para>UDP 수신기를 중지하고 수신 작업 종료를 기다립니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stops the UDP receiver and waits for the receive task.</para>
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
        if (_udpClient is null)
        {
            return;
        }

        _cts?.Cancel();
        _udpClient.Close();
        _udpClient.Dispose();
        _udpClient = null;

        if (_receiveTask is not null)
        {
            try
            {
                await _receiveTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException)
            {
            }
        }

        _cts?.Dispose();
        _cts = null;
        StatusChanged?.Invoke(this, "MC UDP simulator server stopped.");
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
    /// <para>UDP 요청 데이터그램을 받아 실행하고 원격 끝점에 응답합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Receives UDP request datagrams, executes them, and replies to remote endpoints.</para>
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
    /// <para>수신 루프입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The receive loop.</para>
    /// \endif
    /// </returns>
    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _udpClient is not null)
        {
            UdpReceiveResult request;
            try
            {
                request = await _udpClient.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }

            var response = _protocol.Execute(request.Buffer);
            await _udpClient.SendAsync(response, response.Length, request.RemoteEndPoint).ConfigureAwait(false);
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
