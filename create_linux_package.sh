#!/bin/bash
set -e

# Define directories
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
OUTPUT_DIR="$SCRIPT_DIR/linux_package"
LAUNCHER_PUBLISH_DIR="$SCRIPT_DIR/Launcher/bin/Release/net8.0/linux-x64/publish"
CLIENT_PUBLISH_DIR="$SCRIPT_DIR/Client/bin/Release/net8.0/linux-x64/publish"

# Clean up previous package
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR/Client"

# Copy Launcher files
cp -r "$LAUNCHER_PUBLISH_DIR/"* "$OUTPUT_DIR/"

# Copy Client files
cp -r "$CLIENT_PUBLISH_DIR/"* "$OUTPUT_DIR/Client/"

# Set execute permissions
chmod +x "$OUTPUT_DIR/Launcher"
chmod +x "$OUTPUT_DIR/Client/Client"

# Create the archive
tar -czvf BYOND_2.0_Linux.tar.gz -C "$OUTPUT_DIR" .

echo "Linux package created at BYOND_2.0_Linux.tar.gz"
