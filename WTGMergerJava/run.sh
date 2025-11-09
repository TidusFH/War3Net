#!/bin/bash

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WC3LIBS_DIR="$SCRIPT_DIR/../wc3libs"

# Build classpath
CP="$SCRIPT_DIR/bin:$WC3LIBS_DIR/*"

# Run WTGMerger
java -cp "$CP" WTGMerger "$@"
