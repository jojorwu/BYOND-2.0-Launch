; NSIS Script for BYOND 2.0 Installer
!include "MUI2.nsh"
!include "FileFunc.nsh"

!define PRODUCT_NAME "BYOND 2.0"
!define PRODUCT_VERSION "1.0"
!define LAUNCHER_EXECUTABLE_NAME "Launcher.exe"
!define BYOND2_EXECUTABLE_NAME "Client.exe"

!define LAUNCHER_URL "https://github.com/jojorwu/BYOND-2.0-Launch/archive/refs/heads/feature/game-launcher.zip"
!define LAUNCHER_ZIP_NAME "launcher.zip"
!define LAUNCHER_SRC_DIR "BYOND-2.0-Launch-feature-game-launcher"

!define BYOND2_URL "https://github.com/jojorwu/BYOND-2.0/archive/refs/heads/main.zip"
!define BYOND2_ZIP_NAME "byond2.zip"
!define BYOND2_SRC_DIR "BYOND-2.0-main"

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
  ; Check for .NET SDK
  nsExec::ExecToLog 'dotnet --version'
  Pop $0
  IfErrors 0 +2
    MessageBox MB_OK|MB_ICONSTOP "Microsoft .NET SDK is not installed. Please install it before running this installer."
    Abort
FunctionEnd

Function un.onInit
  !insertmacro MUI_UNGETLANGUAGE
FunctionEnd

; --- Sections ---
Section "BYOND Launch" SEC_LAUNCHER
  SetOutPath "$INSTDIR"
  DetailPrint "Downloading BYOND Launch..."
  inetc::get /POPUP "Downloading BYOND Launch" "${LAUNCHER_URL}" "$PLUGINSDIR\${LAUNCHER_ZIP_NAME}"
  Pop $0
  StrCmp $0 "OK" 0 +2
    MessageBox MB_OK|MB_ICONSTOP "Failed to download BYOND Launch: $0"
    Abort

  DetailPrint "Unpacking BYOND Launch..."
  nsisunz::Unzip "$PLUGINSDIR\${LAUNCHER_ZIP_NAME}" "$INSTDIR"
  Pop $0
  StrCmp $0 "success" 0 +2
    MessageBox MB_OK|MB_ICONSTOP "Failed to unpack BYOND Launch: $0"
    Abort

  DetailPrint "Compiling BYOND Launch..."
  nsExec::ExecToLog '"$INSTDIR\${LAUNCHER_SRC_DIR}\dotnet-install.sh" --version 8.0.100'
  nsExec::ExecToLog '"$INSTDIR\${LAUNCHER_SRC_DIR}\.dotnet\dotnet" build "$INSTDIR\${LAUNCHER_SRC_DIR}\Launcher\Launcher.csproj" -c Release -o "$INSTDIR\Launcher"'
  Pop $0
  IfErrors 0 +2
    MessageBox MB_OK|MB_ICONSTOP "Failed to compile BYOND Launch."
    Abort

  RMDir /r "$INSTDIR\${LAUNCHER_SRC_DIR}"
  Delete "$PLUGINSDIR\${LAUNCHER_ZIP_NAME}"

  CreateDirectory "$SMPROGRAMS\${PRODUCT_NAME}"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\BYOND Launch.lnk" "$INSTDIR\Launcher\${LAUNCHER_EXECUTABLE_NAME}"
SectionEnd

Section "BYOND 2" SEC_BYOND2
  SetOutPath "$INSTDIR"
  DetailPrint "Downloading BYOND 2.0..."
  inetc::get /POPUP "Downloading BYOND 2.0" "${BYOND2_URL}" "$PLUGINSDIR\${BYOND2_ZIP_NAME}"
  Pop $0
  StrCmp $0 "OK" 0 +2
    MessageBox MB_OK|MB_ICONSTOP "Failed to download BYOND 2.0: $0"
    Abort

  DetailPrint "Unpacking BYOND 2.0..."
  nsisunz::Unzip "$PLUGINSDIR\${BYOND2_ZIP_NAME}" "$INSTDIR"
  Pop $0
  StrCmp $0 "success" 0 +2
    MessageBox MB_OK|MB_ICONSTOP "Failed to unpack BYOND 2.0: $0"
    Abort

  DetailPrint "Compiling BYOND 2.0..."
  nsExec::ExecToLog '"$INSTDIR\${BYOND2_SRC_DIR}\dotnet-install.sh" --version 8.0.100'
  nsExec::ExecToLog '"$INSTDIR\${BYOND2_SRC_DIR}\.dotnet\dotnet" build "$INSTDIR\${BYOND2_SRC_DIR}\Client\Client.csproj" -c Release -o "$INSTDIR\BYOND2"'
  Pop $0
  IfErrors 0 +2
    MessageBox MB_OK|MB_ICONSTOP "Failed to compile BYOND 2.0."
    Abort

  RMDir /r "$INSTDIR\${BYOND2_SRC_DIR}"
  Delete "$PLUGINSDIR\${BYOND2_ZIP_NAME}"

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
