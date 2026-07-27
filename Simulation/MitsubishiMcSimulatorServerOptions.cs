namespace Dreamine.PLC.Mitsubishi.MC.Simulation;

/// <summary>
/// \if KO
/// <para>Mitsubishi MC 프로토콜 시뮬레이터 서버 옵션을 정의합니다.</para>
/// \endif
/// \if EN
/// <para>Defines options for the Mitsubishi MC simulator server.</para>
/// \endif
/// </summary>
public sealed class MitsubishiMcSimulatorServerOptions
{
    /// <summary>
    /// \if KO
    /// <para>서버 바인딩 주소를 가져오거나 설정합니다. 동일 PC는 127.0.0.1, PC 간 테스트는 0.0.0.0을 사용합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the bind address; use 127.0.0.1 locally and 0.0.0.0 across PCs.</para>
    /// \endif
    /// </summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>
    /// \if KO
    /// <para>서버 포트를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the server port.</para>
    /// \endif
    /// </summary>
    public int Port { get; set; } = 55000;

    /// <summary>
    /// \if KO
    /// <para>트리거 주소 단일 워드 쓰기가 응답 주소를 갱신할지 여부를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets whether one trigger-word write updates the response address.</para>
    /// \endif
    /// </summary>
    public bool EnableAutoWordResponse { get; set; } = true;

    /// <summary>
    /// \if KO
    /// <para>핸드셰이크 트리거 디바이스 코드를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the handshake trigger device code.</para>
    /// \endif
    /// </summary>
    public byte AutoResponseTriggerDeviceCode { get; set; } = 0xA8;

    /// <summary>
    /// \if KO
    /// <para>트리거 워드 오프셋을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the trigger word offset.</para>
    /// \endif
    /// </summary>
    public int AutoResponseTriggerOffset { get; set; } = 100;

    /// <summary>
    /// \if KO
    /// <para>핸드셰이크 응답 디바이스 코드를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the handshake response device code.</para>
    /// \endif
    /// </summary>
    public byte AutoResponseDeviceCode { get; set; } = 0xA8;

    /// <summary>
    /// \if KO
    /// <para>응답 워드 오프셋을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the response word offset.</para>
    /// \endif
    /// </summary>
    public int AutoResponseOffset { get; set; } = 101;

    /// <summary>
    /// \if KO
    /// <para>자동 핸드셰이크 응답 증가값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the automatic handshake response increment.</para>
    /// \endif
    /// </summary>
    public short AutoResponseIncrement { get; set; } = 1;
}
