; NSIS Script for BYOND 2.0 Installer
!include "MUI2.nsh"
!include "Sections.nsh"
!include "LogicLib.nsh"
!include "WordFunc.nsh"

; --- Add Plugins ---
!addplugindir "nsis_plugins"
!addplugindir "nsisunz"

; --- Compression ---
SetCompressor /SOLID lzma

; --- Defines ---
!define PRODUCT_NAME "BYOND 2.0"
!define PRODUCT_VERSION "1.0"
!define DOTNET_INSTALL_SCRIPT_URL "https://dot.net/v1/dotnet-install.ps1"
!define BYOND2_ZIP_URL "https://github.com/jojorwu/BYOND-2.0/archive/refs/heads/main.zip"
!define BYOND2_ZIP_NAME "byond2-main.zip"
!define BYOND2_SRC_DIR "BYOND-2.0-main"

; --- Installer Settings ---
Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "BYOND2_Installer.exe"
InstallDir "$PROGRAMFILES64\${PRODUCT_NAME}"
InstallDirRegKey HKLM "Software\${PRODUCT_NAME}" "Install_Dir"
RequestExecutionLevel admin

; --- UI Settings ---
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "English.txt"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; --- Languages ---
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "Russian"

; --- Language Strings ---
LangString DOTNET_INSTALL_PROMPT ${LANG_ENGLISH} "This application requires .NET 9 SDK. It was not found on your system. Do you want to download and install it now?"
LangString DOTNET_INSTALL_PROMPT ${LANG_RUSSIAN} "Этому приложению требуется .NET 9 SDK. Он не был найден в вашей системе. Вы хотите скачать и установить его сейчас?"
LangString DOTNET_DOWNLOADING ${LANG_ENGLISH} "Downloading .NET 9 Install Script..."
LangString DOTNET_DOWNLOADING ${LANG_RUSSIAN} "Загрузка скрипта установки .NET 9..."
LangString DOTNET_INSTALLING ${LANG_ENGLISH} "Installing .NET 9 SDK..."
LangString DOTNET_INSTALLING ${LANG_RUSSIAN} "Установка .NET 9 SDK..."
LangString DOTNET_INSTALL_FAILED ${LANG_ENGLISH} ".NET 9 SDK installation failed. Please install it manually and try again."
LangString DOTNET_INSTALL_FAILED ${LANG_RUSSIAN} "Не удалось установить .NET 9 SDK. Пожалуйста, установите его вручную и попробуйте снова."
LangString DOWNLOADING_BYOND2 ${LANG_ENGLISH} "Downloading BYOND 2.0 source code..."
LangString DOWNLOADING_BYOND2 ${LANG_RUSSIAN} "Загрузка исходного кода BYOND 2.0..."
LangString EXTRACTING_BYOND2 ${LANG_ENGLISH} "Extracting BYOND 2.0 source code..."
LangString EXTRACTING_BYOND2 ${LANG_RUSSIAN} "Извлечение исходного кода BYOND 2.0..."
LangString BUILDING_BYOND2 ${LANG_ENGLISH} "Building BYOND 2.0... This may take a while."
LangString BUILDING_BYOND2 ${LANG_RUSSIAN} "Сборка BYOND 2.0... Это может занять некоторое время."

; --- Functions ---
Function .onInit
  ; Check for .NET 9 SDK
  Var /GLOBAL DotNet9SdkInstalled
  StrCpy $DotNet9SdkInstalled 0

  ; Check HKLM
  Push HKLM
  Call CheckDotNetSdkInHive
  Pop $0
  ${If} $0 = 1
    StrCpy $DotNet9SdkInstalled 1
    Goto Done
  ${EndIf}

  ; Check HKCU
  Push HKCU
  Call CheckDotNetSdkInHive
  Pop $0
  ${If} $0 = 1
    StrCpy $DotNet9SdkInstalled 1
    Goto Done
  ${EndIf}

  Done:
  ${If} $DotNet9SdkInstalled = 0
    MessageBox MB_YESNO|MB_ICONQUESTION "$(DOTNET_INSTALL_PROMPT)" IDYES InstallDotNet
    Abort

    InstallDotNet:
      InitPluginsDir
      DetailPrint "$(DOTNET_DOWNLOADING)"
      InetC::get /POPUP /CAPTION "$(DOTNET_DOWNLOADING)" /BANNER_TEXT "$(DOTNET_DOWNLOADING)" "${DOTNET_INSTALL_SCRIPT_URL}" "$PLUGINSDIR\dotnet-install.ps1"
      Pop $0
      ${If} $0 != "OK"
        MessageBox MB_OK|MB_ICONEXCLAMATION "Download failed: $0"
        Abort
      ${EndIf}

      DetailPrint "$(DOTNET_INSTALLING)"
      ExecWait 'powershell -ExecutionPolicy Bypass -File "$PLUGINSDIR\dotnet-install.ps1" -Channel 9.0' $0

      ${If} $0 != 0
        MessageBox MB_OK|MB_ICONEXCLAMATION "$(DOTNET_INSTALL_FAILED)"
        Abort
      ${EndIf}
  ${EndIf}
FunctionEnd

Function CheckDotNetSdkInHive
  Exch $R3 ; Hive Root
  StrCpy $R1 0
  Loop:
    EnumRegKey $R0 $R3 "SOFTWARE\dotnet\Setup\InstalledVersions\x64\sdk" $R1
    ${If} $R0 == ""
      Push 0
      Exch $R3
      Return
    ${EndIf}

    ${If} $R0 S~ "9.*"
      Push 1
      Exch $R3
      Return
    ${EndIf}

    IntOp $R1 $R1 + 1
    Goto Loop
FunctionEnd

; --- Main Section ---
Section "BYOND 2.0"
  SetOutPath "$INSTDIR"

  InitPluginsDir

  ; Download and extract source code to a temporary directory
  DetailPrint "$(DOWNLOADING_BYOND2)"
  InetC::get /POPUP /CAPTION "$(DOWNLOADING_BYOND2)" /BANNER_TEXT "$(DOWNLOADING_BYOND2)" "${BYOND2_ZIP_URL}" "$PLUGINSDIR\${BYOND2_ZIP_NAME}"
  Pop $0
  ${If} $0 != "OK"
    MessageBox MB_OK|MB_ICONEXCLAMATION "Download failed: $0"
    Abort
  ${EndIf}

  DetailPrint "$(EXTRACTING_BYOND2)"
  nsisunz::UnzipToLog "$PLUGinsDIR\${BYOND2_ZIP_NAME}" "$PLUGINSDIR"

  ; Build the solution
  DetailPrint "$(BUILDING_BYOND2)"
  ExecWait '"$PROFILE\.dotnet\dotnet.exe" publish "$PLUGINSDIR\${BYOND2_SRC_DIR}\BYOND2.0.sln" -c Release -o "$PLUGINSDIR\dist"' $0

  ; Copy only the necessary files to the installation directory
  CopyFiles /S /SILENT "$PLUGINSDIR\dist\*.*" "$INSTDIR"

  ; Create shortcuts
  CreateDirectory "$SMPROGRAMS\${PRODUCT_NAME}"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\BYOND Launcher.lnk" "$INSTDIR\Launcher.exe"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\BYOND Client.lnk" "$INSTDIR\Client.exe"

SectionEnd

Section -Post
  WriteUninstaller "$INSTDIR\uninstall.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "DisplayName" "${PRODUCT_NAME}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegStr HKLM "Software\${PRODUCT_NAME}" "Install_Dir" "$INSTDIR"
SectionEnd

; --- Uninstaller ---
Section "Uninstall"
  ; Remove Start Menu shortcuts
  Delete "$SMPROGRAMS\${PRODUCT_NAME}\BYOND Launcher.lnk"
  Delete "$SMPROGRAMS\${PRODUCT_NAME}\BYOND Client.lnk"
  RMDir "$SMPROGRAMS\${PRODUCT_NAME}"

  ; Remove main directory
  RMDir /r "$INSTDIR"

  ; Remove registry keys
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
  DeleteRegKey HKLM "Software\${PRODUCT_NAME}"
SectionEnd
