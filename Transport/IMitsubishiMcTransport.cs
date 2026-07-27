using Dreamine.PLC.Abstractions.Results;

namespace Dreamine.PLC.Mitsubishi.MC.Transport;

/// <summary>
/// \if KO
/// <para>Mitsubishi MC 프로토콜 통신 전송 경계를 정의합니다.</para>
/// \endif
/// \if EN
/// <para>Defines a transport boundary for Mitsubishi MC communication.</para>
/// \endif
/// </summary>
public interface IMitsubishiMcTransport : IAsyncDisposable
{
    /// <summary>
    /// \if KO
    /// <para>전송이 연결 또는 준비되었는지 여부를 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets whether the transport is connected or ready.</para>
    /// \endif
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// \if KO
    /// <para>전송을 연결하거나 준비합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Connects or prepares the transport.</para>
    /// \endif
    /// </summary>
    /// <param name="host">
    /// \if KO
    /// <para>대상 호스트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target host.</para>
    /// \endif
    /// </param><param name="port">
    /// \if KO
    /// <para>대상 포트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target port.</para>
    /// \endif
    /// </param><param name="timeoutMs">
    /// \if KO
    /// <para>연결 제한 시간입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The connection timeout.</para>
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
    /// </returns>
    Task<PlcResult> ConnectAsync(
        string host,
        int port,
        int timeoutMs,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// \if KO
    /// <para>전송 연결을 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Disconnects the transport.</para>
    /// \endif
    /// </summary>
    /// <param name="cancellationToken">
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
    Task<PlcResult> DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// \if KO
    /// <para>요청 프레임을 보내고 응답 프레임을 받습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sends a request frame and receives the response frame.</para>
    /// \endif
    /// </summary>
    /// <param name="requestFrame">
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
    /// <para>응답 프레임 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The response-frame result.</para>
    /// \endif
    /// </returns>
    Task<PlcResult<byte[]>> SendAndReceiveAsync(
        IReadOnlyList<byte> requestFrame,
        int receiveTimeoutMs,
        int retryCount,
        CancellationToken cancellationToken = default);
}
