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
        --title="Component Selection" \
        --text="Please select the components you want to install:" \
        --checklist \
        --column="Install?" --column="Component Name" \
        TRUE "BYOND Launch" \
        TRUE "BYOND 2.0" \
        --separator=" ")
}

select_install_dir() {
    user_install_dir=$(zenity --file-selection --directory --title="Choose Installation Directory")
    if [ ! -z "$user_install_dir" ]; then
        INSTALL_DIR=$user_install_dir
    fi
    rm -rf $INSTALL_DIR
    mkdir -p $INSTALL_DIR
}

install_component() {
    local component_name="$1"
    local git_url="$2"
    local git_branch="$3"
    local src_dir="$4"
    local output_dir="$5"
    local exec_name="$6"

    echo "Installing $component_name..."
    cd "$INSTALL_DIR"
    git clone --branch "$git_branch" "$git_url" "$src_dir"
    cd "$src_dir"

    if [ "$component_name" == "BYOND Launch" ]; then
        $HOME/.dotnet/dotnet add Launcher/Launcher.csproj package Avalonia.Controls.DataGrid -v 11.0.0
        $HOME/.dotnet/dotnet build Launcher/Launcher.csproj -c Release -o "$INSTALL_DIR/$output_dir"
    elif [ "$component_name" == "BYOND 2.0" ]; then
        if ! $HOME/.dotnet/dotnet --list-sdks | grep "9."; then
            echo "dotnet 9 could not be found. Installing..."
            ensure_dotnet_install_script
            ./dotnet-install.sh --version 9.0.304
        fi
        $HOME/.dotnet/dotnet build BYOND2.0.sln -c Release
        mkdir -p "$INSTALL_DIR/$output_dir"
        cp Client/bin/Release/net9.0/* "$INSTALL_DIR/$output_dir/"
    fi

    cd ..
    rm -rf "$src_dir"
    create_desktop_file "$component_name" "$INSTALL_DIR/$output_dir/$exec_name"
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
zenity --info --title="Welcome to the BYOND Installer" --text="This wizard will guide you through the installation of BYOND 2.0 and/or BYOND Launch."
check_dependencies
select_components
select_install_dir

summary="The following will be installed:\n\n<b>Components:</b>\n- $(echo $choices | sed 's/ /\\n- /g')\n\n<b>Installation Directory:</b>\n$INSTALL_DIR"
zenity --question --title="Installation Summary" --text="$summary\n\nDo you want to proceed?" --ok-label="Install" --cancel-label="Cancel"
if [ $? -ne 0 ]; then
    exit 0
fi

TMP_DIR=$(mktemp -d)
LOG_FILE="$TMP_DIR/byond_installer.log"
trap 'rm -rf "$TMP_DIR"' EXIT

for choice in $choices; do
    (
        if [ "$choice" == "BYOND Launch" ]; then
            install_component "BYOND Launch" "$LAUNCHER_URL" "$LAUNCHER_BRANCH" "$LAUNCHER_SRC_DIR" "Launcher" "Launcher"
        elif [ "$choice" == "BYOND 2.0" ]; then
            install_component "BYOND 2.0" "$BYOND2_URL" "$BYOND2_BRANCH" "$BYOND2_SRC_DIR" "BYOND2" "Client"
        fi
    ) &> "$LOG_FILE" | zenity --progress --title="Installation in Progress" --text="Installing $choice..." --pulsate --auto-close

    if [ ${PIPESTATUS[0]} -ne 0 ]; then
        zenity --error --text="An error occurred while installing $choice. Please see the log for details."
        zenity --text-info --filename="$LOG_FILE" --title="Error Log: $choice" --width=800 --height=600
        exit 1
    fi
done
zenity --info --title="Installation Successful" --text="The selected components have been successfully installed."
