# Building War3Net Core Libraries

This branch includes enhanced support for:
- **Warcraft 3 patches 1.35.0 - 2.0.3** (latest)
- **Custom World Editor APIs**: YDWE, dzapi, dzapi2, kkapi, YDTrigger, bzapi

## Quick Build

### Windows:
```batch
build-core.bat
```

### Linux/Mac:
```bash
chmod +x build-core.sh
./build-core.sh
```

## Prerequisites

- **.NET 5.0 SDK** or later
- Download: https://dotnet.microsoft.com/download/dotnet/5.0

Verify installation:
```bash
dotnet --version
```

## Output

After successful build, DLLs will be located at:
```
src/War3Net.Build.Core/bin/Release/net5.0/War3Net.Build.Core.dll
src/War3Net.Build/bin/Release/net5.0/War3Net.Build.dll
```

## What's Included

### Extended Patch Support
- v1.35.0 (January 2023)
- v1.36.0, v1.36.1, v1.36.2 (2023-2024)
- v2.0.0, v2.0.1, v2.0.2, v2.0.3 (2024-2025)

### Custom API Support (~2,000 functions)
- **YDWE** (YouDao World Editor) - ~1,300 functions
- **dzapi2** - ~162 functions
- **kkapi** (KKWE) - ~365 functions
- **YDTrigger** - ~29 functions
- **bzapi** - ~142 functions

## Manual Build

If you prefer to build manually:

```bash
# Build dependencies first
dotnet build src/War3Net.Common/War3Net.Common.csproj -c Release
dotnet build src/War3Net.IO.Compression/War3Net.IO.Compression.csproj -c Release
dotnet build src/War3Net.IO.Mpq/War3Net.IO.Mpq.csproj -c Release
dotnet build src/War3Net.IO.Slk/War3Net.IO.Slk.csproj -c Release
dotnet build src/War3Net.CodeAnalysis/War3Net.CodeAnalysis.csproj -c Release
dotnet build src/War3Net.CodeAnalysis.Jass/War3Net.CodeAnalysis.Jass.csproj -c Release

# Build main libraries
dotnet build src/War3Net.Build.Core/War3Net.Build.Core.csproj -c Release
dotnet build src/War3Net.Build/War3Net.Build.csproj -c Release
```

## Troubleshooting

### Missing Submodules
If you get errors about missing CSharp.lua or FastMDX:
```bash
git submodule update --init --recursive
```

### NuGet Package Errors
If you get PackageSourceMapping errors, use the build scripts which avoid test projects, or temporarily rename `nuget.config`:
```bash
mv nuget.config nuget.config.backup
dotnet restore
mv nuget.config.backup nuget.config
```

## Changes in This Branch

### Commits:
1. **Add support for Warcraft 3 patches 1.35-2.0.3**
   - Updated GamePatch.cs with new patch enums
   - Updated GameBuilds.json with version information

2. **Add support for custom World Editor APIs**
   - Extended TriggerData.txt from 12,141 to 29,500 lines
   - Integrated YDWE, dzapi, dzapi2, kkapi, YDTrigger, bzapi
   - Total: ~17,359 lines of custom API functions added

## License

MIT License - See LICENSE file for details
