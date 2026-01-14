# How to Build

## Prerequisites

- .NET 9.0 SDK
- Windows (or enable `EnableWindowsTargeting` for cross-platform build)

## Build Commands

### x86 (32-bit)

```bash
dotnet publish RunCat365/RunCat365.csproj -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -p:EnableWindowsTargeting=true -p:Platform=x86 -o ./publish-x86
```

### x64 (64-bit)

```bash
dotnet publish RunCat365/RunCat365.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableWindowsTargeting=true -p:Platform=x64 -o ./publish-x64
```

### ARM64

```bash
dotnet publish RunCat365/RunCat365.csproj -c Release -r win-arm64 --self-contained true -p:PublishSingleFile=true -p:EnableWindowsTargeting=true -p:Platform=ARM64 -o ./publish-arm64
```

## Build Options

| Option | Description |
|--------|-------------|
| `-c Release` | Release configuration |
| `-r win-x86` | Target runtime (win-x86, win-x64, win-arm64) |
| `--self-contained true` | Include .NET runtime in output |
| `-p:PublishSingleFile=true` | Package as single executable |
| `-p:EnableWindowsTargeting=true` | Enable Windows targeting on non-Windows |
| `-p:Platform=x86` | Target platform |
| `-o ./publish-x86` | Output directory |

## Output

The build outputs a single executable file:
- `publish-x86/RunCat365.exe`
- `publish-x64/RunCat365.exe`
- `publish-arm64/RunCat365.exe`
