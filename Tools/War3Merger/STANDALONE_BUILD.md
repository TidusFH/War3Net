# War3Merger - Standalone Build Configuration

## Changes Made

### Problem
War3Merger was failing to build in Release mode due to:
1. Dependency on `War3Net.Build` which required `War3Net.CodeAnalysis.Transpilers`
2. `War3Net.CodeAnalysis.Transpilers` required uninitialized git submodules (CSharp.lua)
3. These dependencies were completely unnecessary for TriggerMerger's functionality

### Solution
Made War3Merger **completely independent and self-contained**:

1. **Removed War3Net.Build dependency** - Transpilation (JASS to C#/Lua) is not needed
2. **Kept only essential dependencies**:
   - `War3Net.Build.Core` - For MapTriggers parsing and serialization (.wtg files)
   - `War3Net.IO.Mpq` - For MPQ archive reading/writing (.w3x/.w3m files)
3. **Updated to .NET 8.0** - net5.0 is obsolete and unsupported
4. **Fixed all project references** - Always use local source code, never outdated NuGet packages

### Dependency Chain (After Fix)

```
TriggerMerger
├── War3Net.Build.Core (trigger file parsing)
│   ├── War3Net.CodeAnalysis.Jass (JASS syntax definitions)
│   │   └── War3Net.CodeAnalysis (parsing utilities)
│   │       └── Pidgin (parser library) ✅
│   ├── War3Net.IO.Mpq (MPQ archives)
│   │   └── War3Net.IO.Compression
│   │       └── War3Net.Common
│   └── War3Net.IO.Slk (object data files)
│       └── War3Net.Common
└── System.CommandLine (CLI framework)
```

**No Roslyn, No CSharp.lua, No git submodules needed!**

### What Was Removed

- ❌ `War3Net.Build` (transpilation functionality)
- ❌ `War3Net.CodeAnalysis.Transpilers` (JASS to C#/Lua conversion)
- ❌ CSharp.lua submodule dependency
- ❌ Microsoft.CodeAnalysis (Roslyn) dependencies

### Build Commands

**Windows:**
```powershell
cd Tools\War3Merger
dotnet build -c Release
```

**Linux/Mac:**
```bash
cd Tools/War3Merger
dotnet build -c Release
```

**Output:** `bin/Release/net8.0/TriggerMerger.exe` (Windows) or `bin/Release/net8.0/TriggerMerger` (Linux/Mac)

### Requirements

- .NET 8.0 SDK or later
- No git submodules needed
- No additional dependencies

### Benefits

1. ✅ **Faster builds** - Fewer dependencies to compile
2. ✅ **No submodule issues** - Self-contained
3. ✅ **Smaller output** - Only necessary assemblies
4. ✅ **Easier maintenance** - Minimal dependency surface
5. ✅ **Future-proof** - Uses latest .NET 8.0 LTS

### Testing

The tool functionality remains 100% identical:
- Reads .wtg trigger files from maps
- Copies trigger categories between maps
- Writes modified .wtg files back to maps
- Supports all Warcraft 3 versions (v1.27+)

### Files Modified

1. `Tools/War3Merger/War3Net.Tools.TriggerMerger.csproj` - Removed War3Net.Build dependency
2. `Tools/War3Merger/Services/TriggerService.cs` - Removed unused using directive
3. Multiple `*.csproj` files - Changed to always use ProjectReference instead of PackageReference

## Verification

Build should complete successfully with **0 errors**:

```
War3Net.Common → bin/...
War3Net.CodeAnalysis → bin/...
War3Net.CodeAnalysis.Jass → bin/...
War3Net.IO.Compression → bin/...
War3Net.IO.Mpq → bin/...
War3Net.IO.Slk → bin/...
War3Net.Build.Core → bin/...
War3Net.Tools.TriggerMerger → bin/Release/net8.0/TriggerMerger.exe ✓
```

No transpiler or submodule errors!
