; Скрипт установщика BYOND 2.0
!include "MUI2.nsh"

!define PRODUCT_NAME "BYOND 2.0"
!define PRODUCT_VERSION "1.0"
!define LAUNCHER_EXECUTABLE_NAME "Launcher"
!define BYOND2_EXECUTABLE_NAME "Client"

; --- Настройки MUI ---
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

; --- Основные настройки ---
Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "BYOND2_Installer.exe"
InstallDir "$PROGRAMFILES64\${PRODUCT_NAME}"
InstallDirRegKey HKLM "Software\${PRODUCT_NAME}" "Install_Dir"
RequestExecutionLevel admin

; --- Секции ---
Section "Launcher" SEC_LAUNCHER
  SectionIn RO
  SetOutPath "$INSTDIR\Launcher"
  File /r "installer_files\Launcher\"

  CreateDirectory "$SMPROGRAMS\${PRODUCT_NAME}"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\${PRODUCT_NAME} Launcher.lnk" "$INSTDIR\Launcher\${LAUNCHER_EXECUTABLE_NAME}"
SectionEnd

Section "BYOND 2" SEC_BYOND2
  SetOutPath "$INSTDIR\BYOND2"
  File /r "installer_files\BYOND2\"

  CreateDirectory "$SMPROGRAMS\${PRODUCT_NAME}"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\${PRODUCT_NAME}.lnk" "$INSTDIR\BYOND2\Client\${BYOND2_EXECUTABLE_NAME}"
SectionEnd

Section -Post
  WriteUninstaller "$INSTDIR\uninstall.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "DisplayName" "${PRODUCT_NAME}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegStr HKLM "Software\${PRODUCT_NAME}" "Install_Dir" "$INSTDIR"
SectionEnd

; --- Деинсталлятор ---
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

Function .onInit
  !insertmacro MUI_LANGDLL_DISPLAY
FunctionEnd

Function un.onInit
  !insertmacro MUI_UNGETLANGUAGE
FunctionEnd
