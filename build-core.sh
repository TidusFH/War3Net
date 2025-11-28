#!/bin/bash

echo "========================================"
echo "Building War3Net Core Libraries"
echo "With Custom API Support (YDWE, dzapi, etc.)"
echo "========================================"
echo ""

cd "$(dirname "$0")"

FAILED=0

build_project() {
    local step=$1
    local total=$2
    local name=$3
    local project=$4
    local description=$5

    echo "[$step/$total] Building $name..."
    if dotnet build "$project" -c Debug -v quiet; then
        echo "       SUCCESS${description:+ - $description}"
    else
        echo "FAILED: $name"
        FAILED=1
        return 1
    fi
}

build_project 1 10 "CSharp.lua submodule" "submodules/CSharp.lua/CSharp.lua/CSharp.lua.csproj" || exit 1
build_project 2 10 "War3Net.Common" "src/War3Net.Common/War3Net.Common.csproj" || exit 1
build_project 3 10 "War3Net.IO.Compression" "src/War3Net.IO.Compression/War3Net.IO.Compression.csproj" || exit 1
build_project 4 10 "War3Net.IO.Mpq" "src/War3Net.IO.Mpq/War3Net.IO.Mpq.csproj" || exit 1
build_project 5 10 "War3Net.IO.Slk" "src/War3Net.IO.Slk/War3Net.IO.Slk.csproj" || exit 1
build_project 6 10 "War3Net.CodeAnalysis" "src/War3Net.CodeAnalysis/War3Net.CodeAnalysis.csproj" || exit 1
build_project 7 10 "War3Net.CodeAnalysis.Jass" "src/War3Net.CodeAnalysis.Jass/War3Net.CodeAnalysis.Jass.csproj" || exit 1
build_project 8 10 "War3Net.CodeAnalysis.Transpilers" "src/War3Net.CodeAnalysis.Transpilers/War3Net.CodeAnalysis.Transpilers.csproj" || exit 1
build_project 9 10 "War3Net.Build.Core" "src/War3Net.Build.Core/War3Net.Build.Core.csproj" "Contains YDWE, dzapi, dzapi2, kkapi, YDTrigger, bzapi" || exit 1
build_project 10 10 "War3Net.Build" "src/War3Net.Build/War3Net.Build.csproj" || exit 1

echo ""
echo "========================================"
if [ $FAILED -eq 0 ]; then
    echo "BUILD SUCCESSFUL!"
    echo "========================================"
    echo ""
    echo "Copying DLLs to Dlls folder..."

    # Create Dlls folder
    mkdir -p Dlls

    # Copy all War3Net DLLs and their dependencies
    cp -f src/War3Net.Build.Core/bin/Debug/net5.0/*.dll Dlls/ 2>/dev/null
    cp -f src/War3Net.Build/bin/Debug/net5.0/*.dll Dlls/ 2>/dev/null

    echo ""
    echo "========================================"
    echo "DLLs copied to: ./Dlls/"
    echo "========================================"
    echo ""
    echo "Copied files:"
    ls -1 Dlls/War3Net.*.dll 2>/dev/null
    echo ""
    echo "These DLLs include:"
    echo "  - Patches 1.20, 1.24, 1.26, 1.27 through 2.0.3"
    echo "  - Custom APIs: YDWE, dzapi, dzapi2, kkapi, YDTrigger, bzapi"
    echo ""
    echo "All files are in the Dlls folder and ready to use!"
    echo ""

    # Also copy to Libs folder if it exists (for WTGMerger and other tools)
    if [ -d "Libs" ]; then
        echo "========================================"
        echo "Updating Libs folder for WTGMerger..."
        echo "========================================"
        cp -f Dlls/*.dll Libs/ 2>/dev/null
        echo "✓ Libs folder updated"
        echo ""
    fi
else
    echo "BUILD FAILED!"
    echo "========================================"
    echo "Check the error messages above."
fi
echo ""

exit $FAILED
