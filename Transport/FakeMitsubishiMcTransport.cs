using Dreamine.PLC.Abstractions.Results;

namespace Dreamine.PLC.Mitsubishi.MC.Transport;

/// <summary>
/// \if KO
/// <para>테스트와 프로토콜 검증용 가짜 Mitsubishi MC 전송을 제공합니다.</para>
/// \endif
/// \if EN
/// <para>Provides a fake Mitsubishi MC transport for tests and protocol verification.</para>
/// \endif
/// </summary>
public sealed class FakeMitsubishiMcTransport : IMitsubishiMcTransport
{
    /// <summary>
    /// \if KO
    /// <para>responses 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the responses value.</para>
    /// \endif
    /// </summary>
    private readonly Queue<byte[]> _responses = new();
    /// <summary>
    /// \if KO
    /// <para>sent Frames 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the sent frames value.</para>
    /// \endif
    /// </summary>
    private readonly List<byte[]> _sentFrames = new();

    /// <summary>
    /// \if KO
    /// <para>가짜 전송 연결 상태를 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the fake transport connection state.</para>
    /// \endif
    /// </summary>
    public bool IsConnected { get; private set; }

    /// <summary>
    /// \if KO
    /// <para>가짜 전송을 통해 보낸 프레임을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets frames sent through this fake transport.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<byte[]> SentFrames => _sentFrames;

    /// <summary>
    /// \if KO
    /// <para>다음 송수신에서 반환할 응답 프레임을 큐에 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Enqueues a response frame for the next send/receive operation.</para>
    /// \endif
    /// </summary>
    /// <param name="responseFrame">
    /// \if KO
    /// <para>응답 프레임입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The response frame.</para>
    /// \endif
    /// </param><exception cref="ArgumentNullException">
    /// \if KO
    /// <para>프레임이 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the frame is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public void EnqueueResponse(byte[] responseFrame)
    {
        ArgumentNullException.ThrowIfNull(responseFrame);

        _responses.Enqueue(responseFrame);
    }

    /// <summary>
    /// \if KO
    /// <para>가짜 전송을 연결 상태로 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Marks the fake transport as connected.</para>
    /// \endif
    /// </summary><param name="host">
    /// \if KO
    /// <para>사용하지 않는 호스트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The unused host.</para>
    /// \endif
    /// </param><param name="port">
    /// \if KO
    /// <para>사용하지 않는 포트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The unused port.</para>
    /// \endif
    /// </param><param name="timeoutMs">
    /// \if KO
    /// <para>사용하지 않는 제한 시간입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The unused timeout.</para>
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
    /// </returns><exception cref="OperationCanceledException">
    /// \if KO
    /// <para>취소된 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when canceled.</para>
    /// \endif
    /// </exception>
    public Task<PlcResult> ConnectAsync(
        string host,
        int port,
        int timeoutMs,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IsConnected = true;

        return Task.FromResult(PlcResult.Success());
    }

    /// <summary>
    /// \if KO
    /// <para>가짜 전송을 연결 해제 상태로 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Marks the fake transport as disconnected.</para>
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
    /// <para>취소된 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when canceled.</para>
    /// \endif
    /// </exception>
    public Task<PlcResult> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IsConnected = false;

        return Task.FromResult(PlcResult.Success());
    }

    /// <summary>
    /// \if KO
    /// <para>요청을 기록하고 큐에 있는 다음 응답을 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Records the request and returns the next queued response.</para>
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
    /// <para>사용하지 않는 제한 시간입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The unused timeout.</para>
    /// \endif
    /// </param><param name="retryCount">
    /// \if KO
    /// <para>사용하지 않는 재시도 수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The unused retry count.</para>
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
    /// <para>큐의 응답 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The queued response result.</para>
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
    /// <para>취소된 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when canceled.</para>
    /// \endif
    /// </exception>
    public Task<PlcResult<byte[]>> SendAndReceiveAsync(
        IReadOnlyList<byte> requestFrame,
        int receiveTimeoutMs,
        int retryCount,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestFrame);

        cancellationToken.ThrowIfCancellationRequested();

        if (!IsConnected)
        {
            return Task.FromResult(
                PlcResult<byte[]>.Failure("The fake Mitsubishi MC transport is not connected."));
        }

        if (requestFrame.Count == 0)
        {
            return Task.FromResult(
                PlcResult<byte[]>.Failure("The request frame must not be empty."));
        }

        _sentFrames.Add(requestFrame as byte[] ?? requestFrame.ToArray());

        if (_responses.Count == 0)
        {
            return Task.FromResult(
                PlcResult<byte[]>.Failure("No fake Mitsubishi MC response frame was queued."));
        }

        return Task.FromResult(
            PlcResult<byte[]>.Success(_responses.Dequeue()));
    }

    /// <summary>
    /// \if KO
    /// <para>연결 상태와 저장된 프레임을 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Clears connection state and stored frames.</para>
    /// \endif
    /// </summary><returns>
    /// \if KO
    /// <para>완료된 해제 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A completed disposal operation.</para>
    /// \endif
    /// </returns>
    public ValueTask DisposeAsync()
    {
        IsConnected = false;
        _responses.Clear();
        _sentFrames.Clear();

        return ValueTask.CompletedTask;
    }
}
