using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Abstractions.Results;
using Dreamine.PLC.Core.Clients;
using Dreamine.PLC.Mitsubishi.MC.Options;
using Dreamine.PLC.Mitsubishi.MC.Protocol;
using Dreamine.PLC.Mitsubishi.MC.Transport;

namespace Dreamine.PLC.Mitsubishi.MC.Clients;

/// <summary>
/// \if KO
/// <para>Mitsubishi MC 프로토콜 PLC 클라이언트를 제공합니다.</para>
/// \endif
/// \if EN
/// <para>Provides a Mitsubishi MC protocol PLC client.</para>
/// \endif
/// </summary>
public sealed class MitsubishiMcPlcClient : PlcClientBase
{
    /// <summary>
    /// \if KO
    /// <para>options 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the options value.</para>
    /// \endif
    /// </summary>
    private readonly MitsubishiMcConnectionOptions _options;
    /// <summary>
    /// \if KO
    /// <para>transport 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the transport value.</para>
    /// \endif
    /// </summary>
    private readonly IMitsubishiMcTransport _transport;
    /// <summary>
    /// \if KO
    /// <para>frame Builder 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the frame builder value.</para>
    /// \endif
    /// </summary>
    private readonly MitsubishiMcBinary3EFrameBuilder _frameBuilder;
    /// <summary>
    /// \if KO
    /// <para>response Parser 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the response parser value.</para>
    /// \endif
    /// </summary>
    private readonly MitsubishiMcBinary3EResponseParser _responseParser;

    /// <summary>
    /// \if KO
    /// <para>옵션에 맞는 기본 전송과 프레임 구성 요소로 클라이언트를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes the client with default transport and frame components for the options.</para>
    /// \endif
    /// </summary>
    /// <param name="options">
    /// \if KO
    /// <para>연결 옵션입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The connection options.</para>
    /// \endif
    /// </param><exception cref="ArgumentNullException">
    /// \if KO
    /// <para>옵션이 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when options is <see langword="null"/>.</para>
    /// \endif
    /// </exception><exception cref="ArgumentOutOfRangeException">
    /// \if KO
    /// <para>전송 타입이 지원되지 않는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the transport type is unsupported.</para>
    /// \endif
    /// </exception>
    public MitsubishiMcPlcClient(MitsubishiMcConnectionOptions options)
        : this(
            options,
            CreateTransport(options),
            new MitsubishiMcBinary3EFrameBuilder(),
            new MitsubishiMcBinary3EResponseParser())
    {
    }

    /// <summary>
    /// \if KO
    /// <para>지정한 옵션, 전송, 프레임 빌더 및 응답 파서로 클라이언트를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes the client with options, transport, frame builder, and response parser.</para>
    /// \endif
    /// </summary>
    /// <param name="options">
    /// \if KO
    /// <para>연결 옵션입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The connection options.</para>
    /// \endif
    /// </param><param name="transport">
    /// \if KO
    /// <para>MC 전송입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The MC transport.</para>
    /// \endif
    /// </param><param name="frameBuilder">
    /// \if KO
    /// <para>Binary 3E 빌더입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The Binary 3E builder.</para>
    /// \endif
    /// </param><param name="responseParser">
    /// \if KO
    /// <para>Binary 3E 파서입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The Binary 3E parser.</para>
    /// \endif
    /// </param><exception cref="ArgumentNullException">
    /// \if KO
    /// <para>인수 중 하나가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when any argument is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
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
    /// \if KO
    /// <para>Mitsubishi MC 연결 옵션을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the Mitsubishi MC connection options.</para>
    /// \endif
    /// </summary>
    public MitsubishiMcConnectionOptions Options => _options;

    /// <summary>
    /// \if KO
    /// <para>옵션의 전송 타입에 맞는 TCP 또는 UDP 전송을 만듭니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates TCP or UDP transport matching the configured transport type.</para>
    /// \endif
    /// </summary><param name="options">
    /// \if KO
    /// <para>연결 옵션입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The connection options.</para>
    /// \endif
    /// </param><returns>
    /// \if KO
    /// <para>생성된 전송입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The created transport.</para>
    /// \endif
    /// </returns><exception cref="ArgumentNullException">
    /// \if KO
    /// <para>옵션이 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when options is <see langword="null"/>.</para>
    /// \endif
    /// </exception><exception cref="ArgumentOutOfRangeException">
    /// \if KO
    /// <para>전송 타입이 지원되지 않는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the transport type is unsupported.</para>
    /// \endif
    /// </exception>
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

    /// <summary>
    /// \if KO
    /// <para>구성된 호스트와 포트로 MC 전송을 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Connects the MC transport to the configured host and port.</para>
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
    /// <para>연결 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The connection result.</para>
    /// \endif
    /// </returns>
    protected override Task<PlcResult> ConnectCoreAsync(CancellationToken cancellationToken)
    {
        return _transport.ConnectAsync(
            _options.Host,
            _options.Port,
            _options.ConnectTimeoutMs,
            cancellationToken);
    }

    /// <summary>
    /// \if KO
    /// <para>MC 전송 연결을 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Disconnects the MC transport.</para>
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
    /// </returns>
    protected override Task<PlcResult> DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        return _transport.DisconnectAsync(cancellationToken);
    }

    /// <summary>
    /// \if KO
    /// <para>비트 읽기 프레임을 송수신하고 응답 비트를 구문 분석합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sends a bit-read frame and parses response bits.</para>
    /// \endif
    /// </summary><param name="address">
    /// \if KO
    /// <para>시작 주소입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The start address.</para>
    /// \endif
    /// </param><param name="count">
    /// \if KO
    /// <para>비트 수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The bit count.</para>
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
    /// <para>비트 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The bit result.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>워드 읽기 프레임을 송수신하고 응답 워드를 구문 분석합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sends a word-read frame and parses response words.</para>
    /// \endif
    /// </summary><param name="address">
    /// \if KO
    /// <para>시작 주소입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The start address.</para>
    /// \endif
    /// </param><param name="count">
    /// \if KO
    /// <para>워드 수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The word count.</para>
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
    /// <para>워드 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The word result.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>비트 쓰기 프레임을 송수신하고 종료 코드를 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sends a bit-write frame and validates its end code.</para>
    /// \endif
    /// </summary><param name="address">
    /// \if KO
    /// <para>시작 주소입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The start address.</para>
    /// \endif
    /// </param><param name="values">
    /// \if KO
    /// <para>비트 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The bit values.</para>
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
    /// <para>쓰기 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The write result.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>워드 쓰기 프레임을 송수신하고 종료 코드를 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sends a word-write frame and validates its end code.</para>
    /// \endif
    /// </summary><param name="address">
    /// \if KO
    /// <para>시작 주소입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The start address.</para>
    /// \endif
    /// </param><param name="values">
    /// \if KO
    /// <para>워드 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The word values.</para>
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
    /// <para>쓰기 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The write result.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>기본 클라이언트와 MC 전송을 순서대로 비동기 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Asynchronously disposes the base client and MC transport in order.</para>
    /// \endif
    /// </summary><returns>
    /// \if KO
    /// <para>비동기 해제 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The asynchronous disposal operation.</para>
    /// \endif
    /// </returns>
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync().ConfigureAwait(false);
        await _transport.DisposeAsync().ConfigureAwait(false);
    }
}
