; NSIS Script for BYOND 2.0 Installer
!addplugindir "nsis_plugins"
!include "MUI2.nsh"
!include "FileFunc.nsh"

!define PRODUCT_NAME "BYOND 2.0"
!define PRODUCT_VERSION "1.0"
!define LAUNCHER_EXECUTABLE_NAME "Launcher.exe"
!define BYOND2_EXECUTABLE_NAME "Client.exe"

!define LAUNCHER_URL "https://github.com/jojorwu/BYOND-2.0-Launch.git"
!define LAUNCHER_BRANCH "feature/game-launcher"
!define LAUNCHER_SRC_DIR "BYOND-2.0-Launch"

!define BYOND2_URL "https://github.com/jojorwu/BYOND-2.0.git"
!define BYOND2_BRANCH "main"
!define BYOND2_SRC_DIR "BYOND-2.0"

; --- UI Settings ---
!define MUI_ABORTWARNING
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "Russian"
!insertmacro MUI_LANGUAGE "English"

; --- Installer Settings ---
Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "BYOND2_Installer.exe"
InstallDir "$PROGRAMFILES64\${PRODUCT_NAME}"
InstallDirRegKey HKLM "Software\${PRODUCT_NAME}" "Install_Dir"
RequestExecutionLevel admin

; --- Functions ---
Function .onInit
  !insertmacro MUI_LANGDLL_DISPLAY
  ; Check for git
  nsExec::ExecToLog 'git --version'
  Pop $0
  IfErrors 0 +2
    MessageBox MB_OK|MB_ICONSTOP "Git is not installed. Please install it before running this installer."
    Abort

  ; Check for .NET 8 SDK
  nsExec::ExecToLog 'dotnet --list-sdks | findstr "8."'
  Pop $0
  IfErrors install_dotnet8

dotnet9_check:
  ; Check for .NET 9 SDK
  nsExec::ExecToLog 'dotnet --list-sdks | findstr "9."'
  Pop $0
  IfErrors install_dotnet9
  Goto done

install_dotnet8:
  DetailPrint "Downloading .NET 8 SDK..."
  inetc::get /POPUP "Downloading .NET 8 SDK" "https://dot.net/v1/dotnet-install.ps1" "$PLUGINSDIR\dotnet-install.ps1"
  Pop $0
  StrCmp $0 "OK" 0 +2
    MessageBox MB_OK|MB_ICONSTOP "Failed to download .NET 8 SDK installer script: $0"
    Abort
  nsExec::ExecToLog 'powershell -ExecutionPolicy Bypass -File "$PLUGINSDIR\dotnet-install.ps1" -Version 8.0.100 -InstallDir "$PROGRAMFILES\dotnet"'
  Goto dotnet9_check

install_dotnet9:
  DetailPrint "Downloading .NET 9 SDK..."
  inetc::get /POPUP "Downloading .NET 9 SDK" "https://dot.net/v1/dotnet-install.ps1" "$PLUGINSDIR\dotnet-install.ps1"
  Pop $0
  StrCmp $0 "OK" 0 +2
    MessageBox MB_OK|MB_ICONSTOP "Failed to download .NET 9 SDK installer script: $0"
    Abort
  nsExec::ExecToLog 'powershell -ExecutionPolicy Bypass -File "$PLUGINSDIR\dotnet-install.ps1" -Version 9.0.304 -InstallDir "$PROGRAMFILES\dotnet"'

done:
FunctionEnd

Function un.onInit
  !insertmacro MUI_UNGETLANGUAGE
FunctionEnd

; --- Sections ---
Section "BYOND Launch" SEC_LAUNCHER
  SetOutPath "$INSTDIR"
  DetailPrint "Cloning BYOND Launch..."
  nsExec::ExecToLog 'git clone --branch "${LAUNCHER_BRANCH}" "${LAUNCHER_URL}" "$INSTDIR\${LAUNCHER_SRC_DIR}"'
  Pop $0
  IfErrors 0 +2
    MessageBox MB_OK|MB_ICONSTOP "Failed to clone BYOND Launch."
    Abort

  DetailPrint "Compiling BYOND Launch..."
  nsExec::ExecToLog '"$PROGRAMFILES\dotnet\dotnet" build "$INSTDIR\${LAUNCHER_SRC_DIR}\Launcher\Launcher.csproj" -c Release -o "$INSTDIR\Launcher"'
  Pop $0
  IfErrors 0 +2
    MessageBox MB_OK|MB_ICONSTOP "Failed to compile BYOND Launch."
    Abort

  RMDir /r "$INSTDIR\${LAUNCHER_SRC_DIR}"

  CreateDirectory "$SMPROGRAMS\${PRODUCT_NAME}"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\BYOND Launch.lnk" "$INSTDIR\Launcher\${LAUNCHER_EXECUTABLE_NAME}"
SectionEnd

Section "BYOND 2" SEC_BYOND2
  SetOutPath "$INSTDIR"
  DetailPrint "Cloning BYOND 2.0..."
  nsExec::ExecToLog 'git clone --branch "${BYOND2_BRANCH}" "${BYOND2_URL}" "$INSTDIR\${BYOND2_SRC_DIR}"'
  Pop $0
  IfErrors 0 +2
    MessageBox MB_OK|MB_ICONSTOP "Failed to clone BYOND 2.0."
    Abort

  DetailPrint "Compiling BYOND 2.0..."
  nsExec::ExecToLog '"$PROGRAMFILES\dotnet\dotnet" build "$INSTDIR\${BYOND2_SRC_DIR}\BYOND2.0.sln" -c Release'
  Pop $0
  IfErrors 0 +2
    MessageBox MB_OK|MB_ICONSTOP "Failed to compile BYOND 2.0."
    Abort

  CreateDirectory "$INSTDIR\BYOND2"
  CopyFiles /SILENT "$INSTDIR\${BYOND2_SRC_DIR}\Client\bin\Release\net9.0\*.*" "$INSTDIR\BYOND2"

  RMDir /r "$INSTDIR\${BYOND2_SRC_DIR}"

  CreateDirectory "$SMPROGRAMS\${PRODUCT_NAME}"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\BYOND 2.0.lnk" "$INSTDIR\BYOND2\${BYOND2_EXECUTABLE_NAME}"
SectionEnd

Section -Post
  WriteUninstaller "$INSTDIR\uninstall.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "DisplayName" "${PRODUCT_NAME}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegStr HKLM "Software\${PRODUCT_NAME}" "Install_Dir" "$INSTDIR"
SectionEnd

; --- Uninstaller ---
Section "Uninstall"
  Delete "$INSTDIR\uninstall.exe"
  RMDir /r "$INSTDIR\Launcher"
  RMDir /r "$INSTDIR\BYOND2"
  RMDir /r "$INSTDIR"
  Delete "$SMPROGRAMS\${PRODUCT_NAME}\*.*"
  RMDir "$SMPROGRAMS\${PRODUCT_NAME}"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
  DeleteRegKey HKLM "Software\${PRODUCT_NAME}"
SectionEnd
