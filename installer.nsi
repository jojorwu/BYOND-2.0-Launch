; NSIS Script for BYOND 2.0 Installer
!include "MUI2.nsh"
!include "Sections.nsh"
!include "LogicLib.nsh"

; --- Add Plugins ---
!addplugindir "nsis_plugins"

; --- Compression ---
SetCompressor /SOLID lzma

; --- Defines ---
!define PRODUCT_NAME "BYOND 2.0"
!define PRODUCT_VERSION "1.0"
!define LAUNCHER_EXECUTABLE_NAME "Launcher.exe"
!define CLIENT_EXECUTABLE_NAME "Client.exe"
!define DOTNET_RUNTIME_URL "https://download.visualstudio.microsoft.com/download/pr/040c5f26-e17f-442b-a7da-359f1f0a28f8/112e4f073a0c56f2b74abe03693e5066/windowsdesktop-runtime-8.0.5-win-x64.exe"
!define DOTNET_INSTALLER_NAME "dotnet-desktop-runtime-8.exe"

; --- Installer Settings ---
Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "BYOND2_Installer.exe"
InstallDir "$PROGRAMFILES64\${PRODUCT_NAME}"
InstallDirRegKey HKLM "Software\${PRODUCT_NAME}" "Install_Dir"
RequestExecutionLevel admin

; --- UI Settings ---
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "English.txt"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_RUN "$INSTDIR\Launcher\${LAUNCHER_EXECUTABLE_NAME}"
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; --- Languages ---
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "Russian"

; --- Language Strings ---
LangString SEC_LAUNCHER_NAME ${LANG_ENGLISH} "BYOND Launch"
LangString SEC_LAUNCHER_NAME ${LANG_RUSSIAN} "BYOND Launch"
LangString SEC_LAUNCHER_DESC ${LANG_ENGLISH} "The official launcher for BYOND 2.0. Allows you to manage game servers and launch the client."
LangString SEC_LAUNCHER_DESC ${LANG_RUSSIAN} "Официальный лаунчер для BYOND 2.0. Позволяет управлять игровыми серверами и запускать клиент."

LangString SEC_CLIENT_NAME ${LANG_ENGLISH} "BYOND 2.0 Client"
LangString SEC_CLIENT_NAME ${LANG_RUSSIAN} "Клиент BYOND 2.0"
LangString SEC_CLIENT_DESC ${LANG_ENGLISH} "The main game client for connecting to BYOND 2.0 servers."
LangString SEC_CLIENT_DESC ${LANG_RUSSIAN} "Основной игровой клиент для подключения к серверам BYOND 2.0."

LangString DOTNET_INSTALL_PROMPT ${LANG_ENGLISH} "This application requires .NET 8.0 Desktop Runtime. It was not found on your system. Do you want to download and install it now?"
LangString DOTNET_INSTALL_PROMPT ${LANG_RUSSIAN} "Этому приложению требуется .NET 8.0 Desktop Runtime. Он не был найден в вашей системе. Вы хотите скачать и установить его сейчас?"
LangString DOTNET_DOWNLOADING ${LANG_ENGLISH} "Downloading .NET 8.0 Desktop Runtime..."
LangString DOTNET_DOWNLOADING ${LANG_RUSSIAN} "Загрузка .NET 8.0 Desktop Runtime..."
LangString DOTNET_INSTALLING ${LANG_ENGLISH} "Installing .NET 8.0 Desktop Runtime..."
LangString DOTNET_INSTALLING ${LANG_RUSSIAN} "Установка .NET 8.0 Desktop Runtime..."
LangString DOTNET_INSTALL_FAILED ${LANG_ENGLISH} ".NET 8.0 Desktop Runtime installation failed. Please install it manually and try again."
LangString DOTNET_INSTALL_FAILED ${LANG_RUSSIAN} "Не удалось установить .NET 8.0 Desktop Runtime. Пожалуйста, установите его вручную и попробуйте снова."


; --- Functions ---
Function .onInit
  ; Check for .NET 8 Desktop Runtime
  Var /GLOBAL DotNetInstalled
  StrCpy $DotNetInstalled 0

  EnumRegKey $R0 HKLM "SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App" 0
  ${If} $R0 != ""
    ; A version is installed, now let's check if it's 8.x
    StrCpy $R1 0
    Loop:
      EnumRegKey $R0 HKLM "SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App" $R1
      ${If} $R0 == ""
        Goto Done
      ${EndIf}

      ${If} $R0 S~ "8.*"
        StrCpy $DotNetInstalled 1
        Goto Done
      ${EndIf}

      IntOp $R1 $R1 + 1
      Goto Loop
  ${EndIf}

  Done:
  ${If} $DotNetInstalled = 0
    MessageBox MB_YESNO|MB_ICONQUESTION "$(DOTNET_INSTALL_PROMPT)" IDYES InstallDotNet
    Abort

    InstallDotNet:
      InitPluginsDir
      DetailPrint "$(DOTNET_DOWNLOADING)"
      InetC::get /POPUP /CAPTION "$(DOTNET_DOWNLOADING)" /BANNER_TEXT "$(DOTNET_DOWNLOADING)" "${DOTNET_RUNTIME_URL}" "$PLUGINSDIR\${DOTNET_INSTALLER_NAME}"
      Pop $0
      ${If} $0 != "OK"
        MessageBox MB_OK|MB_ICONEXCLAMATION "Download failed: $0"
        Abort
      ${EndIf}

      DetailPrint "$(DOTNET_INSTALLING)"
      ExecWait '"$PLUGINSDIR\${DOTNET_INSTALLER_NAME}" /quiet /norestart' $0

      ${If} $0 != 0
        MessageBox MB_OK|MB_ICONEXCLAMATION "$(DOTNET_INSTALL_FAILED)"
        Abort
      ${EndIf}
  ${EndIf}
FunctionEnd

; --- Sections ---
Section "$(SEC_LAUNCHER_NAME)" SEC_LAUNCHER
  SectionIn RO
  SetOutPath "$INSTDIR\Launcher"
  File /r "dist\Launcher\"
SectionEnd

Section "$(SEC_CLIENT_NAME)" SEC_CLIENT
  SetOutPath "$INSTDIR\Client"
  File /r "dist\Client\"
SectionEnd

; Section descriptions
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC_LAUNCHER} $(SEC_LAUNCHER_DESC)
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC_CLIENT} $(SEC_CLIENT_DESC)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

Section -Post
  WriteUninstaller "$INSTDIR\uninstall.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "DisplayName" "${PRODUCT_NAME}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegStr HKLM "Software\${PRODUCT_NAME}" "Install_Dir" "$INSTDIR"
SectionEnd

; --- Uninstaller ---
Section "Uninstall"
  Delete "$INSTDIR\uninstall.exe"
  RMDir /r "$INSTDIR"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
  DeleteRegKey HKLM "Software\${PRODUCT_NAME}"
SectionEnd
