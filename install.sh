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

# --- Functions ---
install_package() {
    local package_name=$1
    zenity --question --text="The required package '$package_name' is not installed. Do you want to try to install it automatically? (Requires sudo)"
    if [ $? -eq 0 ]; then
        if command -v apt-get &> /dev/null; then
            sudo apt-get update && sudo apt-get install -y $package_name
        elif command -v dnf &> /dev/null; then
            sudo dnf install -y $package_name
        elif command -v yum &> /dev/null; then
            sudo yum install -y $package_name
        elif command -v pacman &> /dev/null; then
            sudo pacman -S --noconfirm $package_name
        else
            zenity --error --text="Could not find a supported package manager. Please install '$package_name' manually."
            exit 1
        fi
    else
        exit 1
    fi
}

ensure_dotnet_install_script() {
    if [ ! -f "dotnet-install.sh" ]; then
        echo "Downloading dotnet-install.sh..."
        wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
        chmod +x dotnet-install.sh
    fi
}

check_dependencies() {
    echo "Checking for dependencies..."
    if ! command -v git &> /dev/null; then
        install_package "git"
    fi
    if ! command -v zenity &> /dev/null; then
        install_package "zenity"
    fi
    if ! command -v $HOME/.dotnet/dotnet &> /dev/null || ! $HOME/.dotnet/dotnet --list-sdks | grep "8."; then
        echo "dotnet 8 could not be found. Installing the .NET 8 SDK..."
        ensure_dotnet_install_script
        ./dotnet-install.sh --version 8.0.100
    fi
}

select_components() {
    choices=$(zenity --list \
        --title="Select Components" \
        --text="Select the components to install:" \
        --checklist \
        --column="Select" --column="Component" \
        TRUE "BYOND Launch" \
        TRUE "BYOND 2.0" \
        --separator=" ")
}

select_install_dir() {
    user_install_dir=$(zenity --file-selection --directory --title="Select Installation Directory")
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
    $HOME/.dotnet/dotnet add Launcher/Launcher.csproj package Avalonia.Controls.DataGrid -v 11.0.0
    $HOME/.dotnet/dotnet build Launcher/Launcher.csproj -c Release -o $INSTALL_DIR/Launcher
    cd ..
    rm -rf $LAUNCHER_SRC_DIR
    create_desktop_file "BYOND Launch" "$INSTALL_DIR/Launcher/Launcher"
}

install_byond2() {
    if ! $HOME/.dotnet/dotnet --list-sdks | grep "9."; then
        echo "dotnet 9 could not be found. Installing..."
        ensure_dotnet_install_script
        ./dotnet-install.sh --version 9.0.304
    fi
    echo "Installing BYOND 2.0..."
    cd $INSTALL_DIR
    git clone --branch $BYOND2_BRANCH $BYOND2_URL
    cd $BYOND2_SRC_DIR
    $HOME/.dotnet/dotnet build BYOND2.0.sln -c Release
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
select_components
select_install_dir

LOG_FILE="/tmp/byond_installer.log"
rm -f $LOG_FILE

for choice in $choices; do
    if [ "$choice" == "BYOND Launch" ]; then
        (
            install_launcher &> $LOG_FILE
        ) | zenity --progress --title="Installing..." --text="Installing BYOND Launch..." --pulsate --auto-close
        if [ $? -ne 0 ]; then
            zenity --error --text="Failed to install BYOND Launch. See $LOG_FILE for details."
            zenity --text-info --filename=$LOG_FILE --title="Installation Log" --width=800 --height=600
            exit 1
        fi
    fi
    if [ "$choice" == "BYOND 2.0" ]; then
        (
            install_byond2 &> $LOG_FILE
        ) | zenity --progress --title="Installing..." --text="Installing BYOND 2.0..." --pulsate --auto-close
        if [ $? -ne 0 ]; then
            zenity --error --text="Failed to install BYOND 2.0. See $LOG_FILE for details."
            zenity --text-info --filename=$LOG_FILE --title="Installation Log" --width=800 --height=600
            exit 1
        fi
    fi
done
zenity --info --text="Installation complete."
