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
    if dotnet build "$project" -c Release -v quiet; then
        echo "       SUCCESS${description:+ - $description}"
    else
        echo "FAILED: $name"
        FAILED=1
        return 1
    fi
}

build_project 1 8 "War3Net.Common" "src/War3Net.Common/War3Net.Common.csproj" || exit 1
build_project 2 8 "War3Net.IO.Compression" "src/War3Net.IO.Compression/War3Net.IO.Compression.csproj" || exit 1
build_project 3 8 "War3Net.IO.Mpq" "src/War3Net.IO.Mpq/War3Net.IO.Mpq.csproj" || exit 1
build_project 4 8 "War3Net.IO.Slk" "src/War3Net.IO.Slk/War3Net.IO.Slk.csproj" || exit 1
build_project 5 8 "War3Net.CodeAnalysis" "src/War3Net.CodeAnalysis/War3Net.CodeAnalysis.csproj" || exit 1
build_project 6 8 "War3Net.CodeAnalysis.Jass" "src/War3Net.CodeAnalysis.Jass/War3Net.CodeAnalysis.Jass.csproj" || exit 1
build_project 7 8 "War3Net.Build.Core" "src/War3Net.Build.Core/War3Net.Build.Core.csproj" "Contains YDWE, dzapi, dzapi2, kkapi, YDTrigger, bzapi" || exit 1
build_project 8 8 "War3Net.Build" "src/War3Net.Build/War3Net.Build.csproj" || exit 1

echo ""
echo "========================================"
if [ $FAILED -eq 0 ]; then
    echo "BUILD SUCCESSFUL!"
    echo "========================================"
    echo ""
    echo "Your DLLs are ready at:"
    echo "  src/War3Net.Build.Core/bin/Release/net5.0/"
    echo "  src/War3Net.Build/bin/Release/net5.0/"
    echo ""
    echo "Key Files:"
    ls -lh src/War3Net.Build.Core/bin/Release/net5.0/War3Net.Build.Core.dll 2>/dev/null
    ls -lh src/War3Net.Build/bin/Release/net5.0/War3Net.Build.dll 2>/dev/null
    echo ""
    echo "These DLLs include:"
    echo "  - Patches 1.20, 1.24, 1.26, 1.27 through 2.0.3"
    echo "  - Custom APIs: YDWE, dzapi, dzapi2, kkapi, YDTrigger, bzapi"
else
    echo "BUILD FAILED!"
    echo "========================================"
    echo "Check the error messages above."
fi
echo ""

exit $FAILED
