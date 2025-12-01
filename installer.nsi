; NSIS Script for BYOND 2.0 Launcher Installer
!include "MUI2.nsh"
!include "Sections.nsh"

; --- Compression ---
SetCompressor /SOLID lzma

; --- Defines ---
!define PRODUCT_NAME "BYOND 2.0 Launcher"
!define PRODUCT_VERSION "1.0"
!define LAUNCHER_EXECUTABLE_NAME "Launcher.exe"

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
!define MUI_FINISHPAGE_RUN "$INSTDIR\${LAUNCHER_EXECUTABLE_NAME}"
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; --- Languages ---
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "Russian"

; --- Section ---
Section "BYOND Launch" SEC_LAUNCHER
  SetOutPath "$INSTDIR"
  File /r "dist\Launcher\"
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
  RMDir /r "$INSTDIR"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
  DeleteRegKey HKLM "Software\${PRODUCT_NAME}"
SectionEnd
