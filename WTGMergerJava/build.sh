#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WC3LIBS_DIR="$SCRIPT_DIR/../wc3libs"

echo "╔═══════════════════════════════════════════════════════════╗"
echo "║          Building WTGMerger (Pure Java Edition)           ║"
echo "╚═══════════════════════════════════════════════════════════╝"
echo

# Create output directory
mkdir -p "$SCRIPT_DIR/bin"

# Build classpath
CP="$WC3LIBS_DIR/*"

echo "Compiling WTGMerger.java..."
javac -cp "$CP" -d "$SCRIPT_DIR/bin" "$SCRIPT_DIR/src/WTGMerger.java"

echo
echo "✓ Build complete!"
echo
echo "To run:"
echo "  ./run.sh <source.w3x> <target.w3x> <output.w3x>"
