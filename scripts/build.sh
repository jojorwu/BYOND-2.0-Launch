#!/bin/bash
set -e

# --- Configuration ---
DIST_DIR="dist"
LAUNCHER_PROJ="Launcher/Launcher.csproj"
CLIENT_PROJ="Client/Client.csproj"
LAUNCHER_OUT="$DIST_DIR/Launcher"
CLIENT_OUT="$DIST_DIR/Client"
DOTNET_EXEC="$HOME/.dotnet/dotnet"

# --- Main ---
echo "Starting build process..."

# Check for dotnet
if ! command -v $DOTNET_EXEC &> /dev/null; then
    echo ".NET SDK not found. Please run the installer first."
    exit 1
fi

# Clean previous builds
if [ -d "$DIST_DIR" ]; then
    echo "Removing previous build directory..."
    rm -rf "$DIST_DIR"
fi
mkdir -p "$LAUNCHER_OUT"
mkdir -p "$CLIENT_OUT"

# Build Launcher
echo "Building Launcher..."
$DOTNET_EXEC publish "$LAUNCHER_PROJ" -c Release -o "$LAUNCHER_OUT" --nologo

# Build Client
echo "Building Client..."
$DOTNET_EXEC publish "$CLIENT_PROJ" -c Release -o "$CLIENT_OUT" --nologo

echo "Build process completed successfully."
echo "Output can be found in the '$DIST_DIR' directory."
