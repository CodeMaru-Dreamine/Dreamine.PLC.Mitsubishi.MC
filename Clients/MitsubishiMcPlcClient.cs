using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Abstractions.Results;
using Dreamine.PLC.Core.Clients;
using Dreamine.PLC.Mitsubishi.MC.Options;
using Dreamine.PLC.Mitsubishi.MC.Protocol;
using Dreamine.PLC.Mitsubishi.MC.Transport;

namespace Dreamine.PLC.Mitsubishi.MC.Clients;

/// <summary>
/// Provides a Mitsubishi MC protocol PLC client implementation.
/// </summary>
public sealed class MitsubishiMcPlcClient : PlcClientBase
{
    private readonly MitsubishiMcConnectionOptions _options;
    private readonly IMitsubishiMcTransport _transport;
    private readonly MitsubishiMcBinary3EFrameBuilder _frameBuilder;
    private readonly MitsubishiMcBinary3EResponseParser _responseParser;

    /// <summary>
    /// Initializes a new instance of the <see cref="MitsubishiMcPlcClient"/> class.
    /// </summary>
    /// <param name="options">The Mitsubishi MC connection options.</param>
    public MitsubishiMcPlcClient(MitsubishiMcConnectionOptions options)
        : this(
            options,
            CreateTransport(options),
            new MitsubishiMcBinary3EFrameBuilder(),
            new MitsubishiMcBinary3EResponseParser())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MitsubishiMcPlcClient"/> class.
    /// </summary>
    /// <param name="options">The Mitsubishi MC connection options.</param>
    /// <param name="transport">The Mitsubishi MC transport.</param>
    /// <param name="frameBuilder">The Mitsubishi MC Binary 3E frame builder.</param>
    /// <param name="responseParser">The Mitsubishi MC Binary 3E response parser.</param>
    public MitsubishiMcPlcClient(
        MitsubishiMcConnectionOptions options,
        IMitsubishiMcTransport transport,
        MitsubishiMcBinary3EFrameBuilder frameBuilder,
        MitsubishiMcBinary3EResponseParser responseParser)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _frameBuilder = frameBuilder ?? throw new ArgumentNullException(nameof(frameBuilder));
        _responseParser = responseParser ?? throw new ArgumentNullException(nameof(responseParser));
    }

    /// <summary>
    /// Gets the Mitsubishi MC connection options.
    /// </summary>
    public MitsubishiMcConnectionOptions Options => _options;

    private static IMitsubishiMcTransport CreateTransport(MitsubishiMcConnectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.TransportType switch
        {
            MitsubishiMcTransportType.Tcp => new TcpMitsubishiMcTransport(),
            MitsubishiMcTransportType.Udp => new UdpMitsubishiMcTransport(),
            _ => throw new ArgumentOutOfRangeException(nameof(options), options.TransportType, "Unsupported Mitsubishi MC transport type.")
        };
    }

    /// <inheritdoc />
    protected override Task<PlcResult> ConnectCoreAsync(CancellationToken cancellationToken)
    {
        return _transport.ConnectAsync(
            _options.Host,
            _options.Port,
            _options.ConnectTimeoutMs,
            cancellationToken);
    }

    /// <inheritdoc />
    protected override Task<PlcResult> DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        return _transport.DisconnectAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task<PlcResult<bool[]>> ReadBitsCoreAsync(
        PlcAddress address,
        int count,
        CancellationToken cancellationToken)
    {
        var frameResult = _frameBuilder.BuildBatchReadFrame(
            _options,
            address,
            count,
            isBitAccess: true);

        if (!frameResult.IsSuccess || frameResult.Value is null)
        {
            return PlcResult<bool[]>.Failure(
                frameResult.Message ?? "Failed to build Mitsubishi MC bit read frame.",
                frameResult.ErrorCode);
        }

        var responseResult = await _transport.SendAndReceiveAsync(
            frameResult.Value,
            _options.ReceiveTimeoutMs,
            _options.RetryCount,
            cancellationToken).ConfigureAwait(false);

        if (!responseResult.IsSuccess || responseResult.Value is null)
        {
            return PlcResult<bool[]>.Failure(
                responseResult.Message ?? "Failed to receive Mitsubishi MC bit read response.",
                responseResult.ErrorCode);
        }

        return _responseParser.ParseReadBits(responseResult.Value, count);
    }

    /// <inheritdoc />
    protected override async Task<PlcResult<short[]>> ReadWordsCoreAsync(
        PlcAddress address,
        int count,
        CancellationToken cancellationToken)
    {
        var frameResult = _frameBuilder.BuildBatchReadFrame(
            _options,
            address,
            count,
            isBitAccess: false);

        if (!frameResult.IsSuccess || frameResult.Value is null)
        {
            return PlcResult<short[]>.Failure(
                frameResult.Message ?? "Failed to build Mitsubishi MC word read frame.",
                frameResult.ErrorCode);
        }

        var responseResult = await _transport.SendAndReceiveAsync(
            frameResult.Value,
            _options.ReceiveTimeoutMs,
            _options.RetryCount,
            cancellationToken).ConfigureAwait(false);

        if (!responseResult.IsSuccess || responseResult.Value is null)
        {
            return PlcResult<short[]>.Failure(
                responseResult.Message ?? "Failed to receive Mitsubishi MC word read response.",
                responseResult.ErrorCode);
        }

        return _responseParser.ParseReadWords(responseResult.Value, count);
    }

    /// <inheritdoc />
    protected override async Task<PlcResult> WriteBitsCoreAsync(
        PlcAddress address,
        IReadOnlyList<bool> values,
        CancellationToken cancellationToken)
    {
        var frameResult = _frameBuilder.BuildBatchWriteBitsFrame(
            _options,
            address,
            values);

        if (!frameResult.IsSuccess || frameResult.Value is null)
        {
            return PlcResult.Failure(
                frameResult.Message ?? "Failed to build Mitsubishi MC bit write frame.",
                frameResult.ErrorCode);
        }

        var responseResult = await _transport.SendAndReceiveAsync(
            frameResult.Value,
            _options.ReceiveTimeoutMs,
            _options.RetryCount,
            cancellationToken).ConfigureAwait(false);

        if (!responseResult.IsSuccess || responseResult.Value is null)
        {
            return PlcResult.Failure(
                responseResult.Message ?? "Failed to receive Mitsubishi MC bit write response.",
                responseResult.ErrorCode);
        }

        var parseResult = _responseParser.Parse(responseResult.Value);
        if (!parseResult.IsSuccess)
        {
            return PlcResult.Failure(
                parseResult.Message ?? "Failed to parse Mitsubishi MC bit write response.",
                parseResult.ErrorCode);
        }

        return PlcResult.Success();
    }

    /// <inheritdoc />
    protected override async Task<PlcResult> WriteWordsCoreAsync(
        PlcAddress address,
        IReadOnlyList<short> values,
        CancellationToken cancellationToken)
    {
        var frameResult = _frameBuilder.BuildBatchWriteWordsFrame(
            _options,
            address,
            values);

        if (!frameResult.IsSuccess || frameResult.Value is null)
        {
            return PlcResult.Failure(
                frameResult.Message ?? "Failed to build Mitsubishi MC word write frame.",
                frameResult.ErrorCode);
        }

        var responseResult = await _transport.SendAndReceiveAsync(
            frameResult.Value,
            _options.ReceiveTimeoutMs,
            _options.RetryCount,
            cancellationToken).ConfigureAwait(false);

        if (!responseResult.IsSuccess || responseResult.Value is null)
        {
            return PlcResult.Failure(
                responseResult.Message ?? "Failed to receive Mitsubishi MC word write response.",
                responseResult.ErrorCode);
        }

        var parseResult = _responseParser.Parse(responseResult.Value);
        if (!parseResult.IsSuccess)
        {
            return PlcResult.Failure(
                parseResult.Message ?? "Failed to parse Mitsubishi MC word write response.",
                parseResult.ErrorCode);
        }

        return PlcResult.Success();
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync().ConfigureAwait(false);
        await _transport.DisposeAsync().ConfigureAwait(false);
    }
}