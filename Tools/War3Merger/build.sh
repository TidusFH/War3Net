#!/bin/bash

# War3Merger Build Script
# This script builds the TriggerMerger tool

echo "========================================"
echo "War3Merger Build Script"
echo "========================================"
echo ""

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null
then
    echo "❌ ERROR: .NET SDK is not installed"
    echo ""
    echo "Please install .NET SDK first:"
    echo ""
    echo "Ubuntu/Debian:"
    echo "  wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb"
    echo "  sudo dpkg -i packages-microsoft-prod.deb"
    echo "  sudo apt-get update"
    echo "  sudo apt-get install -y dotnet-sdk-8.0"
    echo ""
    echo "Or download from: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "✓ .NET SDK found: $(dotnet --version)"
echo ""

# Navigate to the project directory
cd "$(dirname "$0")"

echo "Building TriggerMerger..."
echo ""

# Build the project
dotnet build -c Release

if [ $? -eq 0 ]; then
    echo ""
    echo "========================================"
    echo "✓ Build successful!"
    echo "========================================"
    echo ""
    echo "The executable is located at:"
    echo "  bin/Release/net8.0/TriggerMerger"
    echo ""
    echo "Run it with:"
    echo "  cd bin/Release/net8.0"
    echo "  ./TriggerMerger --help"
    echo ""
else
    echo ""
    echo "========================================"
    echo "❌ Build failed!"
    echo "========================================"
    echo ""
    echo "Please check the error messages above."
    exit 1
fi
