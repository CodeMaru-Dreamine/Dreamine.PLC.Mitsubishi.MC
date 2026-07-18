using Dreamine.PLC.Mitsubishi.MC.Protocol;

namespace Dreamine.PLC.Mitsubishi.MC.Options;

/// <summary>
/// \if KO
/// <para>Mitsubishi MC 프로토콜 통신 연결 옵션을 나타냅니다.</para>
/// \endif
/// \if EN
/// <para>Represents connection options for Mitsubishi MC protocol communication.</para>
/// \endif
/// </summary>
public sealed class MitsubishiMcConnectionOptions
{
    /// <summary>
    /// \if KO
    /// <para>PLC 호스트 주소를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the PLC host address.</para>
    /// \endif
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>PLC 포트를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the PLC port.</para>
    /// \endif
    /// </summary>
    public int Port { get; set; } = 5000;

    /// <summary>
    /// \if KO
    /// <para>네트워크 번호를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the network number.</para>
    /// \endif
    /// </summary>
    public byte NetworkNumber { get; set; } = 0x00;

    /// <summary>
    /// \if KO
    /// <para>PLC 번호를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the PLC number.</para>
    /// \endif
    /// </summary>
    public byte PlcNumber { get; set; } = 0xFF;

    /// <summary>
    /// \if KO
    /// <para>요청 대상 모듈 I/O 번호를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the request destination module I/O number.</para>
    /// \endif
    /// </summary>
    public ushort DestinationModuleIoNumber { get; set; } = 0x03FF;

    /// <summary>
    /// \if KO
    /// <para>요청 대상 모듈 스테이션 번호를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the request destination module station number.</para>
    /// \endif
    /// </summary>
    public byte DestinationModuleStationNumber { get; set; } = 0x00;

    /// <summary>
    /// \if KO
    /// <para>모니터링 타이머를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the monitoring timer.</para>
    /// \endif
    /// </summary>
    public ushort MonitoringTimer { get; set; } = 0x0010;

    /// <summary>
    /// \if KO
    /// <para>밀리초 단위 연결 제한 시간을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the connection timeout in milliseconds.</para>
    /// \endif
    /// </summary>
    public int ConnectTimeoutMs { get; set; } = 3000;

    /// <summary>
    /// \if KO
    /// <para>밀리초 단위 송신 제한 시간을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the send timeout in milliseconds.</para>
    /// \endif
    /// </summary>
    public int SendTimeoutMs { get; set; } = 3000;

    /// <summary>
    /// \if KO
    /// <para>밀리초 단위 수신 제한 시간을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the receive timeout in milliseconds.</para>
    /// \endif
    /// </summary>
    public int ReceiveTimeoutMs { get; set; } = 3000;

    /// <summary>
    /// \if KO
    /// <para>MC 프레임 형식을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the MC frame format.</para>
    /// \endif
    /// </summary>
    public MitsubishiMcFrameFormat FrameFormat { get; set; } = MitsubishiMcFrameFormat.Binary3E;

    /// <summary>
    /// \if KO
    /// <para>MC 전송 타입을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the MC transport type.</para>
    /// \endif
    /// </summary>
    public MitsubishiMcTransportType TransportType { get; set; } = MitsubishiMcTransportType.Tcp;

    /// <summary>
    /// \if KO
    /// <para>송수신 재시도 횟수를 가져오거나 설정합니다. UDP는 제한 시간 재전송에 사용하고 TCP는 1 미만을 한 번으로 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets send/receive retries; UDP retransmits on timeout and TCP treats values below one as one attempt.</para>
    /// \endif
    /// </summary>
    public int RetryCount { get; set; } = 1;
}
