# Dreamine.PLC.Mitsubishi.MC

This package provides a Mitsubishi MC protocol adapter for the Dreamine PLC communication stack.

## Purpose

`Dreamine.PLC.Mitsubishi.MC` is part of the Dreamine PLC package family.

The package is designed to keep PLC communication code separated by responsibility:

- Abstractions define contracts.
- Core provides shared runtime infrastructure.
- Vendor adapters implement device-specific communication.
- WPF provides monitoring and diagnostic UI components.

## Features

- Mitsubishi MC protocol adapter boundary
- Vendor-specific address mapping structure
- Connection and request handling integration with Dreamine.PLC.Core
- MC protocol read/write implementation entry points
- Testable adapter design through Dreamine.PLC.Abstractions


## Project References

- `Dreamine.PLC.Abstractions`
- `Dreamine.PLC.Core`

## Target Framework

```xml
<TargetFramework>net8.0</TargetFramework>
```

## Package Metadata

| Item | Value |
|---|---|
| PackageId | `Dreamine.PLC.Mitsubishi.MC` |
| Version | `1.0.0` |
| License | `MIT` |
| Repository | `https://github.com/CodeMaru-Dreamine/Dreamine.PLC.Mitsubishi.MC` |
| Project URL | `https://github.com/CodeMaru-Dreamine/Dreamine.PLC.FullKit` |

## Architecture Rule

This repository must not reference application-level projects.

Dependency direction must remain one-way:

```text
Abstractions
    ▲
    │
Core
    ▲
    │
Vendor Adapter / WPF UI Component
```

## License

This project is licensed under the MIT License.
