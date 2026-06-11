using System.Net;
using System.Net.Sockets;
using Dreamine.PLC.Core.Memory;

namespace Dreamine.PLC.Mitsubishi.MC.Simulation;

/// <summary>
/// Provides a minimal Mitsubishi MC Binary 3E TCP simulator server for local and cross-PC tests.
/// </summary>
public sealed class MitsubishiMcTcpSimulatorServer : IAsyncDisposable
{
    private readonly MitsubishiMcSimulatorServerOptions _options;
    private readonly MitsubishiMcBinary3ESimulatorProtocol _protocol;
    private readonly List<Task> _clientTasks = [];
    private readonly object _syncRoot = new();
    private CancellationTokenSource? _cts;
    private TcpListener? _listener;
    private Task? _acceptTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="MitsubishiMcTcpSimulatorServer"/> class.
    /// </summary>
    /// <param name="options">The simulator options.</param>
    public MitsubishiMcTcpSimulatorServer(MitsubishiMcSimulatorServerOptions options)
        : this(options, new InMemoryPlcMemory())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MitsubishiMcTcpSimulatorServer"/> class.
    /// </summary>
    /// <param name="options">The simulator options.</param>
    /// <param name="memory">The shared PLC memory.</param>
    public MitsubishiMcTcpSimulatorServer(MitsubishiMcSimulatorServerOptions options, InMemoryPlcMemory memory)
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
    public bool IsRunning => _listener is not null;

    /// <summary>
    /// Starts the MC TCP simulator server.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
    /// Stops the MC TCP simulator server.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _protocol.StatusChanged -= OnProtocolStatusChanged;
        await StopAsync().ConfigureAwait(false);
    }

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
