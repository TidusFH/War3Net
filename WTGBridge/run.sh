#!/bin/bash
# Run WTGBridge with all required classpath dependencies

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WC3LIBS_DIR="$SCRIPT_DIR/../wc3libs"

# Build classpath
CP="$SCRIPT_DIR:$WC3LIBS_DIR/*"

java -cp "$CP" WTGBridge "$@"
