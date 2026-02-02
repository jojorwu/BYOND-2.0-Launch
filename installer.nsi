; NSIS Script for BYOND 2.0 Installer
!addplugindir "nsis_plugins"
!include "MUI2.nsh"
!include "FileFunc.nsh"
!include "Sections.nsh"

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

!define GIT_URL "https://github.com/git-for-windows/git/releases/download/v2.33.0.windows.2/Git-2.33.0.2-64-bit.exe"
!define GIT_INSTALLER_NAME "git_installer.exe"

; --- UI Settings ---
!define MUI_ABORTWARNING
!define MUI_ICON "installer_files/icon.ico"
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_BITMAP "installer_files/header.bmp"
!define MUI_WELCOMEFINISHPAGE_BITMAP "installer_files/welcome.bmp"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "$(MUI_LANG).txt"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_CONFIRM
!define MUI_FINISHPAGE_RUN
!define MUI_FINISHPAGE_RUN_FUNCTION "LaunchApplications"
!insertmacro MUI_PAGE_FINISH

Function PageShowReady
  FindWindow $R0 "#32770" "" $HWNDPARENT
  GetDlgItem $R1 $R0 1028 ; Static control for summary
  SendMessage $R1 ${WM_SETTEXT} 0 "$(ReadyText)"

  ${ForEachSection} SectionCallback
FunctionEnd

Function SectionCallback
  ${If} ${SectionIsSelected} $0
    FindWindow $R0 "#32770" "" $HWNDPARENT
    GetDlgItem $R1 $R0 1028
    SendMessage $R1 ${EM_REPLACESEL} 0 "STR: - ${SEC_NAME}\r\n"
  ${EndIf}
FunctionEnd

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "Russian"
!insertmacro MUI_LANGUAGE "English"

; --- Language Strings ---
LangString ReadyText ${LANG_RUSSIAN} "Установка будет произведена в:$\r$\n$INSTDIR$\r$\n$\r$\nВыбранные компоненты:"
LangString ReadyText ${LANG_ENGLISH} "Setup will install to:$\r$\n$INSTDIR$\r$\n$\r$\nSelected components:"
LangString DescLauncher ${LANG_RUSSIAN} "Игровой лаунчер для подключения к серверам."
LangString DescLauncher ${LANG_ENGLISH} "Game launcher to connect to servers."
LangString DescByond2 ${LANG_RUSSIAN} "Игровой клиент BYOND 2.0."
LangString DescByond2 ${LANG_ENGLISH} "The BYOND 2.0 game client."

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
  IfErrors install_git
  Goto dotnet8_check

install_git:
  DetailPrint "Downloading Git..."
  inetc::get /POPUP "Downloading Git" "${GIT_URL}" "$PLUGINSDIR\${GIT_INSTALLER_NAME}"
  Pop $0
  StrCmp $0 "OK" 0 +2
    MessageBox MB_OK|MB_ICONSTOP "Failed to download Git: $0"
    Abort
  DetailPrint "Installing Git..."
  ExecWait '"$PLUGINSDIR\${GIT_INSTALLER_NAME}" /SILENT'
  ; Add git to path for this session
  ReadEnvStr $0 "PATH"
  Push "$0;$PROGRAMFILES\Git\cmd"
  Call AddToPath

dotnet8_check:

  ; Check for .NET 8 SDK
  nsExec::ExecToLog '"$PROGRAMFILES\dotnet\dotnet.exe" --list-sdks | findstr "8."'
  Pop $0
  IfErrors install_dotnet8
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

Function AddToPath
  Exch $0
  System::Call 'Kernel32::SetEnvironmentVariableA(t, t) i("PATH", $0).r0'
FunctionEnd

Function LaunchApplications
  ${If} ${SectionIsSelected} ${SEC_LAUNCHER}
    Exec '"$INSTDIR\Launcher\${LAUNCHER_EXECUTABLE_NAME}"'
  ${EndIf}
  ${If} ${SectionIsSelected} ${SEC_BYOND2}
    Exec '"$INSTDIR\BYOND2\${BYOND2_EXECUTABLE_NAME}"'
  ${EndIf}
FunctionEnd

; --- Macros ---
!macro InstallComponent NAME URL BRANCH SRC_DIR CSPROJ_PATH OUTPUT_DIR EXEC_NAME
  SetOutPath "$INSTDIR"
  SetDetailsPrint textonly
  DetailPrint "Installing ${NAME}..."
  SetDetailsPrint listonly
  DetailPrint "Cloning repository..."
  nsExec::ExecToLog 'git clone --branch "${BRANCH}" "${URL}" "$INSTDIR\${SRC_DIR}"'
  Pop $0
  IfErrors 0 +2
    MessageBox MB_OK|MB_ICONSTOP "Failed to clone ${NAME}."
    Abort

  ${If} ${NAME} == "BYOND Launch"
    DetailPrint "Adding required packages..."
    nsExec::ExecToLog '"$PROGRAMFILES\dotnet\dotnet.exe" add "$INSTDIR\${SRC_DIR}\${CSPROJ_PATH}" package Avalonia.Controls.DataGrid -v 11.0.0'
    DetailPrint "Compiling project..."
    nsExec::ExecToLog '"$PROGRAMFILES\dotnet\dotnet.exe" build "$INSTDIR\${SRC_DIR}\${CSPROJ_PATH}" -c Release -o "$INSTDIR\${OUTPUT_DIR}"'
  ${ElseIf} ${NAME} == "BYOND 2"
    DetailPrint "Checking for .NET 9 SDK..."
    nsExec::ExecToLog '"$PROGRAMFILES\dotnet\dotnet.exe" --list-sdks | findstr "9."'
    Pop $0
    IfErrors install_dotnet9_sec
    Goto publish_byond2

install_dotnet9_sec:
    DetailPrint "Downloading .NET 9 SDK..."
    inetc::get /POPUP "Downloading .NET 9 SDK" "https://dot.net/v1/dotnet-install.ps1" "$PLUGINSDIR\dotnet-install.ps1"
    Pop $0
    StrCmp $0 "OK" 0 +2
      MessageBox MB_OK|MB_ICONSTOP "Failed to download .NET 9 SDK installer script: $0"
      Abort
    DetailPrint "Installing .NET 9 SDK..."
    nsExec::ExecToLog 'powershell -ExecutionPolicy Bypass -File "$PLUGINSDIR\dotnet-install.ps1" -Version 9.0.304 -InstallDir "$PROGRAMFILES\dotnet"'

publish_byond2:
    DetailPrint "Publishing project..."
    nsExec::ExecToLog '"$PROGRAMFILES\dotnet\dotnet.exe" publish "$INSTDIR\${SRC_DIR}\${CSPROJ_PATH}" -c Release -o "$INSTDIR\${OUTPUT_DIR}"'
  ${EndIf}
  Pop $0
  IfErrors 0 +2
    MessageBox MB_OK|MB_ICONSTOP "Failed to compile/publish ${NAME}."
    Abort

  DetailPrint "Cleaning up..."
  RMDir /r "$INSTDIR\${SRC_DIR}"

  DetailPrint "Creating shortcuts..."
  CreateDirectory "$SMPROGRAMS\${PRODUCT_NAME}"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\${NAME}.lnk" "$INSTDIR\${OUTPUT_DIR}\${EXEC_NAME}"
!macroend

; --- Sections ---
Section "BYOND Launch" SEC_LAUNCHER
  !insertmacro InstallComponent "BYOND Launch" "${LAUNCHER_URL}" "${LAUNCHER_BRANCH}" "${LAUNCHER_SRC_DIR}" "Launcher\Launcher.csproj" "Launcher" "${LAUNCHER_EXECUTABLE_NAME}"
SectionEnd

Section "BYOND 2" SEC_BYOND2
  !insertmacro InstallComponent "BYOND 2" "${BYOND2_URL}" "${BYOND2_BRANCH}" "${BYOND2_SRC_DIR}" "Client\Client.csproj" "BYOND2" "${BYOND2_EXECUTABLE_NAME}"
SectionEnd

; --- Descriptions ---
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC_LAUNCHER} $(DescLauncher)
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC_BYOND2} $(DescByond2)
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
  RMDir /r "$INSTDIR\Launcher"
  RMDir /r "$INSTDIR\BYOND2"
  RMDir /r "$INSTDIR"
  Delete "$SMPROGRAMS\${PRODUCT_NAME}\*.*"
  RMDir "$SMPROGRAMS\${PRODUCT_NAME}"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
  DeleteRegKey HKLM "Software\${PRODUCT_NAME}"
SectionEnd
