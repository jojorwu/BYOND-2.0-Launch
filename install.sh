#!/bin/bash
# Installer script for BYOND 2.0 and BYOND Launch on Linux

# --- Configuration ---
LAUNCHER_URL="https://github.com/jojorwu/BYOND-2.0-Launch.git"
LAUNCHER_BRANCH="feature/game-launcher"
LAUNCHER_SRC_DIR="BYOND-2.0-Launch"

BYOND2_URL="https://github.com/jojorwu/BYOND-2.0.git"
BYOND2_BRANCH="main"
BYOND2_SRC_DIR="BYOND-2.0"

INSTALL_DIR="$HOME/BYOND2.0"
DOTNET8_PATH="$HOME/.dotnet/dotnet"
DOTNET9_PATH="$HOME/.dotnet9/dotnet"

# --- Functions ---
check_dependencies() {
    echo "Checking for dependencies..."
    if ! command -v git &> /dev/null; then
        echo "git could not be found. Please install git."
        exit 1
    fi
    if [ ! -f "$DOTNET8_PATH" ]; then
        echo "dotnet 8 could not be found. Please install the .NET 8 SDK."
        exit 1
    fi
}

check_dotnet9() {
    if [ ! -f "$DOTNET9_PATH" ]; then
        echo "dotnet 9 could not be found. Installing..."
        ./dotnet-install.sh --version 9.0.304 --install-dir "$HOME/.dotnet9"
    fi
}

select_component() {
    echo "Select the component to install:"
    echo "1) BYOND Launch"
    echo "2) BYOND 2.0"
    read -p "Enter your choice [1-2]: " choice
    case $choice in
        1)
            COMPONENT="Launcher"
            ;;
        2)
            COMPONENT="BYOND2"
            ;;
        *)
            echo "Invalid choice. Exiting."
            exit 1
            ;;
    esac
}

select_install_dir() {
    read -p "Enter installation directory [$INSTALL_DIR]: " user_install_dir
    if [ ! -z "$user_install_dir" ]; then
        INSTALL_DIR=$user_install_dir
    fi
    rm -rf $INSTALL_DIR
    mkdir -p $INSTALL_DIR
}

install_launcher() {
    echo "Installing BYOND Launch..."
    cd $INSTALL_DIR
    git clone --branch $LAUNCHER_BRANCH $LAUNCHER_URL
    cd $LAUNCHER_SRC_DIR
    $DOTNET8_PATH add Launcher/Launcher.csproj package Avalonia.Controls.DataGrid -v 11.0.0
    $DOTNET8_PATH build Launcher/Launcher.csproj -c Release -o $INSTALL_DIR/Launcher
    cd ..
    rm -rf $LAUNCHER_SRC_DIR
    create_desktop_file "BYOND Launch" "$INSTALL_DIR/Launcher/Launcher"
}

install_byond2() {
    check_dotnet9
    echo "Installing BYOND 2.0..."
    cd $INSTALL_DIR
    git clone --branch $BYOND2_BRANCH $BYOND2_URL
    cd $BYOND2_SRC_DIR
    $DOTNET9_PATH build BYOND2.0.sln -c Release
    mkdir -p $INSTALL_DIR/BYOND2
    cp Client/bin/Release/net9.0/* $INSTALL_DIR/BYOND2/
    cd ..
    rm -rf $BYOND2_SRC_DIR
    create_desktop_file "BYOND 2.0" "$INSTALL_DIR/BYOND2/Client"
}

create_desktop_file() {
    local name=$1
    local exec_path=$2
    local desktop_dir="$HOME/.local/share/applications"
    mkdir -p "$desktop_dir"
    local desktop_file="$desktop_dir/byond2.desktop"
    echo "[Desktop Entry]
Name=$name
Exec=$exec_path
Type=Application
Terminal=false" > $desktop_file
}

# --- Main ---
check_dependencies
select_component
select_install_dir

if [ "$COMPONENT" == "Launcher" ]; then
    install_launcher
else
    install_byond2
fi

echo "Installation complete."
