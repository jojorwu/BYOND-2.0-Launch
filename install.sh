#!/bin/bash
# Installer script for BYOND 2.0 and BYOND Launch on Linux

# --- Configuration ---
INSTALL_DIR="$HOME/BYOND2.0"
DIST_DIR="dist"

# --- Functions ---
install_package() {
    local package_name=$1
    zenity --question --text="$(printf "$TEXT_PKG_NOT_FOUND" "$package_name")" --window-icon="installer_files/icon.ico"
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
            zenity --error --text="Could not find a supported package manager. Please install '$package_name' manually." --window-icon="installer_files/icon.ico"
            exit 1
        fi
    else
        exit 1
    fi
}

check_dependencies() {
    echo "Checking for dependencies..."
    if ! command -v zenity &> /dev/null; then
        install_package "zenity"
    fi
}

select_components() {
    choices=$(zenity --list \
        --title="$TITLE_COMP_SELECT" \
        --text="$TEXT_COMP_SELECT" \
        --checklist \
        --column="$COL_INSTALL" --column="$COL_COMP_NAME" \
        TRUE "BYOND Launch" \
        TRUE "BYOND 2.0" \
        --separator=" " --window-icon="installer_files/icon.ico")
}

select_install_dir() {
    user_install_dir=$(zenity --file-selection --directory --title="$TITLE_DIR_SELECT" --window-icon="installer_files/icon.ico")
    if [ ! -z "$user_install_dir" ]; then
        INSTALL_DIR=$user_install_dir
    fi
    rm -rf $INSTALL_DIR
    mkdir -p $INSTALL_DIR
}

install_component() {
    local component_name="$1"
    local src_dir="$2"
    local output_dir="$3"
    local exec_name="$4"

    echo "Installing $component_name..."
    mkdir -p "$INSTALL_DIR/$output_dir"
    cp -r "$src_dir"/* "$INSTALL_DIR/$output_dir/"

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
if [ -z "$DISPLAY" ]; then
    echo "This installer requires a graphical environment (X11). Please run it in a desktop session."
    exit 1
fi

if [[ "$LANG" == "ru"* ]]; then
    source <(cat <<'EOF'
_LANG="ru"
TITLE_WELCOME="Добро пожаловать в установщик BYOND"
TEXT_WELCOME="Этот мастер поможет вам установить BYOND 2.0 и/или BYOND Launch."
TEXT_PKG_NOT_FOUND="Требуемый пакет '%s' не установлен. Попробовать установить его автоматически? (Требуется sudo)"
TITLE_COMP_SELECT="Выбор компонентов"
TEXT_COMP_SELECT="Пожалуйста, выберите компоненты для установки:"
COL_INSTALL="Установить?"
COL_COMP_NAME="Имя компонента"
TITLE_DIR_SELECT="Выберите каталог для установки"
TITLE_SUMMARY="Итоги установки"
TEXT_SUMMARY="Будет установлено следующее:\n\n<b>Компоненты:</b>\n- %s\n\n<b>Каталог установки:</b>\n%s"
TEXT_PROCEED="\n\nВы хотите продолжить?"
BTN_INSTALL="Установить"
BTN_CANCEL="Отмена"
TITLE_INSTALL_PROGRESS="Идет установка"
TEXT_INSTALL_PROGRESS="Установка %s..."
TEXT_ERROR="Произошла ошибка при установке %s."
TITLE_ERROR_LOG="Лог ошибок: %s"
TITLE_SUCCESS="Установка успешно завершена"
TEXT_SUCCESS="Выбранные компоненты были успешно установлены."
EOF
)
else
    source <(cat <<'EOF'
_LANG="en"
TITLE_WELCOME="Welcome to the BYOND Installer"
TEXT_WELCOME="This wizard will guide you through the installation of BYOND 2.0 and/or BYOND Launch."
TEXT_PKG_NOT_FOUND="The required package '%s' is not installed. Do you want to try to install it automatically? (Requires sudo)"
TITLE_COMP_SELECT="Component Selection"
TEXT_COMP_SELECT="Please select the components you want to install:"
COL_INSTALL="Install?"
COL_COMP_NAME="Component Name"
TITLE_DIR_SELECT="Choose Installation Directory"
TITLE_SUMMARY="Installation Summary"
TEXT_SUMMARY="The following will be installed:\n\n<b>Components:</b>\n- %s\n\n<b>Installation Directory:</b>\n%s"
TEXT_PROCEED="\n\nDo you want to proceed?"
BTN_INSTALL="Install"
BTN_CANCEL="Cancel"
TITLE_INSTALL_PROGRESS="Installation in Progress"
TEXT_INSTALL_PROGRESS="Installing %s..."
TEXT_ERROR="An error occurred while installing %s."
TITLE_ERROR_LOG="Error Log: %s"
TITLE_SUCCESS="Installation Successful"
TEXT_SUCCESS="The selected components have been successfully installed."
EOF
)
fi

zenity --info --title="$TITLE_WELCOME" --text="$TEXT_WELCOME" --window-icon="installer_files/icon.ico"
check_dependencies
select_components
select_install_dir

summary=$(printf "$TEXT_SUMMARY" "$(echo $choices | sed 's/ /\\n- /g')" "$INSTALL_DIR")
zenity --question --title="$TITLE_SUMMARY" --text="$summary$TEXT_PROCEED" --ok-label="$BTN_INSTALL" --cancel-label="$BTN_CANCEL" --window-icon="installer_files/icon.ico"
if [ $? -ne 0 ]; then
    exit 0
fi

for choice in $choices; do
    (
        if [ "$choice" == "BYOND Launch" ]; then
            install_component "BYOND Launch" "$DIST_DIR/Launcher" "Launcher" "Launcher"
        elif [ "$choice" == "BYOND 2.0" ]; then
            install_component "BYOND 2.0" "$DIST_DIR/Client" "BYOND2" "Client"
        fi
    ) | zenity --progress --title="$TITLE_INSTALL_PROGRESS" --text="$(printf "$TEXT_INSTALL_PROGRESS" "$choice")" --pulsate --auto-close --window-icon="installer_files/icon.ico"

    if [ ${PIPESTATUS[0]} -ne 0 ]; then
        zenity --error --text="$(printf "$TEXT_ERROR" "$choice")" --window-icon="installer_files/icon.ico"
        exit 1
    fi
done

zenity --info --title="$TITLE_SUCCESS" --text="$TEXT_SUCCESS" --window-icon="installer_files/icon.ico"
