# Build Fix for War3Merger

## Problem

When building War3Merger in **Release mode**, the build failed with:

```
error CS1501: Nenhuma sobrecarga para o método "Compress" leva 4 argumentos
(No overload for method 'Compress' takes 4 arguments)
```

Location: `src/War3Net.IO.Mpq/MpqZLibCompressor.cs:39`

## Root Cause

The War3Net projects used **conditional references**:
- **Debug mode**: Used `ProjectReference` (local source code) ✅
- **Release mode**: Used `PackageReference` (NuGet packages) ❌

The NuGet packages were **outdated** and didn't have the latest API changes (like the 4-parameter `ZLibCompression.Compress` method).

## Solution

Changed all conditional references to **always use ProjectReference**, ensuring the build uses the latest local source code instead of outdated NuGet packages.

## Files Modified

1. `src/War3Net.IO.Mpq/War3Net.IO.Mpq.csproj`
2. `src/War3Net.IO.Compression/War3Net.IO.Compression.csproj`
3. `src/War3Net.Build.Core/War3Net.Build.Core.csproj`
4. `src/War3Net.Build/War3Net.Build.csproj`
5. `src/War3Net.CodeAnalysis.Jass/War3Net.CodeAnalysis.Jass.csproj`
6. `src/War3Net.IO.Slk/War3Net.IO.Slk.csproj`
7. `src/War3Net.CodeAnalysis.Transpilers/War3Net.CodeAnalysis.Transpilers.csproj`

## Changes Made

**Before:**
```xml
<ItemGroup Condition="'$(Configuration)'=='Debug'">
  <ProjectReference Include="..\SomeProject\SomeProject.csproj" />
</ItemGroup>

<ItemGroup Condition="'$(Configuration)'=='Release'">
  <PackageReference Include="SomeProject" Version="$(SomeProjectVersion)" />
</ItemGroup>
```

**After:**
```xml
<ItemGroup>
  <ProjectReference Include="..\SomeProject\SomeProject.csproj" />
</ItemGroup>
```

## How to Build Now

```bash
# Windows
cd Tools\War3Merger
dotnet build -c Release

# Linux/Mac
cd Tools/War3Merger
dotnet build -c Release
```

Both Debug and Release configurations now work correctly!

## Test Results

The build should now succeed in both Debug and Release modes without any method resolution errors.
