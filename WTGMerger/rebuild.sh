#!/bin/bash

echo "==================================="
echo "   Rebuilding WTGMerger"
echo "==================================="
echo ""

cd "$(dirname "$0")"

echo "Cleaning old build files..."
rm -rf bin obj

echo ""
echo "Building project with latest changes..."
dotnet build --configuration Release

if [ $? -ne 0 ]; then
    echo ""
    echo "ERROR: Build failed!"
    echo "Please check the error messages above."
    exit 1
fi

echo ""
echo "==================================="
echo "Build successful!"
echo "You can now run: dotnet run"
echo "==================================="
