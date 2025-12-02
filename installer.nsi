; NSIS Script for BYOND 2.0 Installer
!include "MUI2.nsh"
!include "Sections.nsh"
!include "LogicLib.nsh"
!include "WordFunc.nsh"

; --- Add Plugins ---
!addplugindir "nsis_plugins"

; --- Compression ---
SetCompressor /SOLID lzma

; --- Defines ---
!define PRODUCT_NAME "BYOND 2.0"
!define PRODUCT_VERSION "1.0"
!define DOTNET_RUNTIME_URL "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-9.0.11-windows-x64-installer"
!define MUI_PAGE_CUSTOMFUNCTION_PRE fn_CreateDesktopShortcut_pre
!define MUI_PAGE_CUSTOMFUNCTION_SHOW fn_CreateDesktopShortcut_show
!define MUI_PAGE_CUSTOMFUNCTION_LEAVE fn_CreateDesktopShortcut_leave

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
Page custom fn_CreateDesktopShortcut_pre fn_CreateDesktopShortcut_show fn_CreateDesktopShortcut_leave
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; --- Languages ---
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "Russian"

; --- Language Strings ---
LangString DOTNET_INSTALL_PROMPT ${LANG_ENGLISH} "This application requires .NET 9 Desktop Runtime. It was not found on your system. Do you want to download and install it now?"
LangString DOTNET_INSTALL_PROMPT ${LANG_RUSSIAN} "Этому приложению требуется .NET 9 Desktop Runtime. Он не был найден в вашей системе. Вы хотите скачать и установить его сейчас?"
LangString DOTNET_DOWNLOADING ${LANG_ENGLISH} "Downloading .NET 9 Desktop Runtime..."
LangString DOTNET_DOWNLOADING ${LANG_RUSSIAN} "Загрузка .NET 9 Desktop Runtime..."
LangString DOTNET_INSTALLING ${LANG_ENGLISH} "Installing .NET 9 Desktop Runtime..."
LangString DOTNET_INSTALLING ${LANG_RUSSIAN} "Установка .NET 9 Desktop Runtime..."
LangString DOTNET_INSTALL_FAILED ${LANG_ENGLISH} ".NET 9 Desktop Runtime installation failed. Please install it manually and try again."
LangString DOTNET_INSTALL_FAILED ${LANG_RUSSIAN} "Не удалось установить .NET 9 Desktop Runtime. Пожалуйста, установите его вручную и попробуйте снова."
LangString DESKTOP_SHORTCUT_TITLE ${LANG_ENGLISH} "Create Desktop Shortcuts"
LangString DESKTOP_SHORTCUT_TITLE ${LANG_RUSSIAN} "Создать ярлыки на рабочем столе"
LangString DESKTOP_SHORTCUT_TEXT ${LANG_ENGLISH} "Create desktop shortcuts for:"
LangString DESKTOP_SHORTCUT_TEXT ${LANG_RUSSIAN} "Создать ярлыки на рабочем столе для:"
LangString DESKTOP_SHORTCUT_LAUNCHER ${LANG_ENGLISH} "BYOND Launcher"
LangString DESKTOP_SHORTCUT_LAUNCHER ${LANG_RUSSIAN} "BYOND Launcher"
LangString DESKTOP_SHORTCUT_CLIENT ${LANG_ENGLISH} "BYOND Client"
LangString DESKTOP_SHORTCUT_CLIENT ${LANG_RUSSIAN} "Клиент BYOND"

Var hCreateDesktopShortcutDialog
Var hCheckboxLauncher
Var hCheckboxClient
Var bCreateDesktopShortcutLauncher
Var bCreateDesktopShortcutClient

Function fn_CreateDesktopShortcut_pre
  ; Set default values
  StrCpy $bCreateDesktopShortcutLauncher ${BST_CHECKED}
  StrCpy $bCreateDesktopShortcutClient ${BST_CHECKED}
FunctionEnd

Function fn_CreateDesktopShortcut_show
  nsDialogs::Create 1018
  Pop $hCreateDesktopShortcutDialog

  ${If} $hCreateDesktopShortcutDialog == error
    Abort
  ${EndIf}

  !insertmacro MUI_HEADER_TEXT "$(DESKTOP_SHORTCUT_TITLE)" "$(DESKTOP_SHORTCUT_TEXT)"

  ${NSD_CreateCheckbox} 10u 20u 80% 12u "$(DESKTOP_SHORTCUT_LAUNCHER)"
  Pop $hCheckboxLauncher
  ${NSD_SetState} $hCheckboxLauncher $bCreateDesktopShortcutLauncher

  ${NSD_CreateCheckbox} 10u 40u 80% 12u "$(DESKTOP_SHORTCUT_CLIENT)"
  Pop $hCheckboxClient
  ${NSD_SetState} $hCheckboxClient $bCreateDesktopShortcutClient

  nsDialogs::Show
FunctionEnd

Function fn_CreateDesktopShortcut_leave
  ${NSD_GetState} $hCheckboxLauncher $bCreateDesktopShortcutLauncher
  ${NSD_GetState} $hCheckboxClient $bCreateDesktopShortcutClient
FunctionEnd

Function .onInit
  ; Check for .NET 9 Desktop Runtime
  Var /GLOBAL DotNet9RuntimeInstalled
  StrCpy $DotNet9RuntimeInstalled 0

  ; Check HKLM
  Push HKLM
  Call CheckDotNetRuntimeInHive
  Pop $0
  ${If} $0 = 1
    StrCpy $DotNet9RuntimeInstalled 1
    Goto Done
  ${EndIf}

  ; Check HKCU
  Push HKCU
  Call CheckDotNetRuntimeInHive
  Pop $0
  ${If} $0 = 1
    StrCpy $DotNet9RuntimeInstalled 1
    Goto Done
  ${EndIf}

  Done:
  ${If} $DotNet9RuntimeInstalled = 0
    MessageBox MB_YESNO|MB_ICONQUESTION "$(DOTNET_INSTALL_PROMPT)" IDYES InstallDotNet
    Abort

    InstallDotNet:
      InitPluginsDir
      DetailPrint "$(DOTNET_DOWNLOADING)"
      InetC::get /POPUP /CAPTION "$(DOTNET_DOWNLOADING)" /BANNER_TEXT "$(DOTNET_DOWNLOADING)" "${DOTNET_RUNTIME_URL}" "$PLUGINSDIR\dotnet-runtime.exe"
      Pop $0
      ${If} $0 != "OK"
        MessageBox MB_OK|MB_ICONEXCLAMATION "Download failed: $0"
        Abort
      ${EndIf}

      DetailPrint "$(DOTNET_INSTALLING)"
      ExecWait '"$PLUGINSDIR\dotnet-runtime.exe" /quiet /norestart' $0

      ${If} $0 != 0
        MessageBox MB_OK|MB_ICONEXCLAMATION "$(DOTNET_INSTALL_FAILED)"
        Abort
      ${EndIf}
  ${EndIf}
FunctionEnd

Function CheckDotNetRuntimeInHive
  Exch $R3 ; Hive Root
  StrCpy $R1 0
  Loop:
    EnumRegKey $R0 $R3 "SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App" $R1
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

  ; Copy files
  SetOutPath "$INSTDIR\Launcher"
  File /r "dist\Launcher\"
  SetOutPath "$INSTDIR\Client"
  File /r "dist\Client\"

  ; Create Start Menu shortcuts
  CreateDirectory "$SMPROGRAMS\${PRODUCT_NAME}"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\BYOND Launcher.lnk" "$INSTDIR\Launcher\Launcher.exe"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\BYOND Client.lnk" "$INSTDIR\Client\Client.exe"

  ; Create Desktop shortcuts if selected
  ${If} $bCreateDesktopShortcutLauncher == ${BST_CHECKED}
    CreateShortCut "$DESKTOP\BYOND Launcher.lnk" "$INSTDIR\Launcher\Launcher.exe"
  ${EndIf}
  ${If} $bCreateDesktopShortcutClient == ${BST_CHECKED}
    CreateShortCut "$DESKTOP\BYOND Client.lnk" "$INSTDIR\Client\Client.exe"
  ${EndIf}

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

  ; Remove Desktop shortcuts
  Delete "$DESKTOP\BYOND Launcher.lnk"
  Delete "$DESKTOP\BYOND Client.lnk"

  ; Remove main directory
  RMDir /r "$INSTDIR"

  ; Remove registry keys
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
  DeleteRegKey HKLM "Software\${PRODUCT_NAME}"
SectionEnd
