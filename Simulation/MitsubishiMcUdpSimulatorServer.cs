using System.Net;
using System.Net.Sockets;
using Dreamine.PLC.Core.Memory;

namespace Dreamine.PLC.Mitsubishi.MC.Simulation;

/// <summary>
/// Provides a minimal Mitsubishi MC Binary 3E UDP simulator server for local and cross-PC tests.
/// </summary>
public sealed class MitsubishiMcUdpSimulatorServer : IAsyncDisposable
{
    private readonly MitsubishiMcSimulatorServerOptions _options;
    private readonly MitsubishiMcBinary3ESimulatorProtocol _protocol;
    private CancellationTokenSource? _cts;
    private UdpClient? _udpClient;
    private Task? _receiveTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="MitsubishiMcUdpSimulatorServer"/> class.
    /// </summary>
    /// <param name="options">The simulator options.</param>
    public MitsubishiMcUdpSimulatorServer(MitsubishiMcSimulatorServerOptions options)
        : this(options, new InMemoryPlcMemory())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MitsubishiMcUdpSimulatorServer"/> class.
    /// </summary>
    /// <param name="options">The simulator options.</param>
    /// <param name="memory">The shared PLC memory.</param>
    public MitsubishiMcUdpSimulatorServer(MitsubishiMcSimulatorServerOptions options, InMemoryPlcMemory memory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _protocol = new MitsubishiMcBinary3ESimulatorProtocol(memory ?? throw new ArgumentNullException(nameof(memory)), options);
        _protocol.StatusChanged += OnProtocolStatusChanged;
    }

    /// <summary>
    /// Occurs when the server status changes.
    /// </summary>
    public event EventHandler<string>? StatusChanged;

    /// <summary>
    /// Gets whether the server is running.
    /// </summary>
    public bool IsRunning => _udpClient is not null;

    /// <summary>
    /// Starts the MC UDP simulator server.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
    /// Stops the MC UDP simulator server.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _protocol.StatusChanged -= OnProtocolStatusChanged;
        await StopAsync().ConfigureAwait(false);
    }

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

    private static IPAddress ParseAddress(string host)
    {
        if (string.IsNullOrWhiteSpace(host) || host == "*" || host == "+")
        {
            return IPAddress.Any;
        }

        return IPAddress.TryParse(host, out var address) ? address : IPAddress.Any;
    }

    private void OnProtocolStatusChanged(object? sender, string e)
    {
        StatusChanged?.Invoke(this, e);
    }
}
