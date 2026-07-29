# Dreamine.PLC.Mitsubishi.MC

[![CI](https://github.com/CodeMaru-Dreamine/Dreamine.PLC.Mitsubishi.MC/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/CodeMaru-Dreamine/Dreamine.PLC.Mitsubishi.MC/actions/workflows/ci.yml)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=CodeMaru-Dreamine_Dreamine.PLC.Mitsubishi.MC&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=CodeMaru-Dreamine_Dreamine.PLC.Mitsubishi.MC)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=CodeMaru-Dreamine_Dreamine.PLC.Mitsubishi.MC&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=CodeMaru-Dreamine_Dreamine.PLC.Mitsubishi.MC)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=CodeMaru-Dreamine_Dreamine.PLC.Mitsubishi.MC&metric=coverage)](https://sonarcloud.io/summary/new_code?id=CodeMaru-Dreamine_Dreamine.PLC.Mitsubishi.MC)

[![License](https://img.shields.io/badge/license-MIT-2496ED.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![NuGet](https://img.shields.io/nuget/v/Dreamine.PLC.Mitsubishi.MC.svg)](https://www.nuget.org/packages/Dreamine.PLC.Mitsubishi.MC)
[![Downloads](https://img.shields.io/nuget/dt/Dreamine.PLC.Mitsubishi.MC.svg)](https://www.nuget.org/packages/Dreamine.PLC.Mitsubishi.MC)

[![Docs](https://img.shields.io/badge/%F0%9F%93%98%20Docs-dreamine.kr-2496ED)](https://dreamine.kr/libraries?lang=en)
[![Guide](https://img.shields.io/badge/%F0%9F%93%98%20Guide-dreamine.kr-2496ED)](https://dreamine.kr/guide?lang=en)
[![Playground](https://img.shields.io/badge/%F0%9F%8E%AE%20Playground-dreamine.kr-7B2CBF)](https://dreamine.kr/playground?lang=en)
[![Book](https://img.shields.io/badge/%F0%9F%93%96%20Book-Practical%20MVVM%20Architecture-black)](https://bookk.co.kr/bookStore/69c0f1b41461ec1ae849a0f6)

[Korean documentation](./README_KO.md)

Mitsubishi MC protocol adapter for Dreamine PLC communication.

This package provides Mitsubishi MC TCP/UDP client support and built-in MC protocol simulator servers for local and PC-to-PC validation.

## Features

- Mitsubishi MC TCP client
- Mitsubishi MC UDP client
- MC TCP simulator server
- MC UDP simulator server
- Binary 3E frame-based read/write flow
- Word read/write diagnostics
- Repeated handshake validation flow
- Timeout and retry support for UDP
- Integration with `IPlcClient`

## Supported simulator test modes

The SampleSmart PLC Protocol page supports:

```text
McTcp ↔ McTcp
McUdp ↔ McUdp
```

The server and client modes must match. A `SimulatorTcp` server cannot be used with an `McTcp` or `McUdp` client.

## 1PC test

Use this flow for local validation.

```text
Mode: McTcp or McUdp
Host: 127.0.0.1
Port: 55000
Start Server
Use Client
Connect
Write Words
Read Words
Run Handshake
```

## 2PC test

Server PC:

```text
Mode: McTcp or McUdp
Host: 0.0.0.0
Port: 55000
Start Server
```

Client PC:

```text
Mode: same as server
Host: server PC IP
Port: 55000
Use Client
Connect
Read/Write or Handshake
```

## Firewall requirement for PC-to-PC tests

Open the inbound port on the server PC.

For TCP:

```powershell
New-NetFirewallRule -DisplayName "Dreamine PLC MC TCP 55000" -Direction Inbound -Protocol TCP -LocalPort 55000 -Action Allow
```

For UDP:

```powershell
New-NetFirewallRule -DisplayName "Dreamine PLC MC UDP 55000" -Direction Inbound -Protocol UDP -LocalPort 55000 -Action Allow
```

Run PowerShell as Administrator. Without these rules, the same application can pass 1PC tests but fail 2PC tests.

## Physical PLC test notice

The built-in MC simulator verifies the Dreamine MC client/server flow, but physical Mitsubishi PLC testing must still be performed.

Before connecting to a real Mitsubishi PLC, verify:

- PLC model and Ethernet module support
- MC protocol TCP/UDP setting
- Port number
- Device memory mapping
- Binary/ASCII frame setting if applicable
- Network firewall and routing
- Safe polling interval

## Polling and write safety

Do not use 1ms polling against a physical PLC.

Recommended physical PLC values:

- Monitoring: 100ms to 500ms
- UI display refresh: 250ms to 1000ms
- Write: event-driven only
- Handshake stress test: simulator only unless explicitly approved for a real machine

## Vendor runtime policy

This package does not include Mitsubishi MX Component or any Mitsubishi runtime DLL.

This package implements MC protocol communication directly. MX Component integration, if needed, must remain in a separate adapter package without redistributing vendor DLLs.

## Validation status

Validated:

- 1PC MC TCP read/write and handshake
- 1PC MC UDP read/write and handshake
- 2PC MC TCP read/write and handshake
- 2PC MC UDP read/write and handshake
- WPF monitor integration

Pending:

- Physical Mitsubishi PLC validation

## License

MIT License.
