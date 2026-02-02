
;--------------------------------
; General Attributes

Name "Inetc Translate Test"
OutFile "Translate.exe"
RequestExecutionLevel user


;--------------------------------
;Interface Settings

  !include "MUI2.nsh"
  !define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install-colorful.ico"
  !insertmacro MUI_PAGE_WELCOME
  !insertmacro MUI_PAGE_INSTFILES
  !insertmacro MUI_PAGE_FINISH
  !insertmacro MUI_LANGUAGE "Russian"


;--------------------------------
;Installer Sections

Section "Dummy Section" SecDummy

; This is russian variant. See Readme.txt for a list of parameters.
; Use LangStrings as TRANSLATE parameters for multilang options

    inetc::load /POPUP "" /CAPTION "\xd4\xe0\xe9\xeb \xe3\xe0\xeb\xe5\xf0\xe5\xe8" /TRANSLATE "URL" "\xc7\xe0\xe3\xf0\xf3\xe7\xea\xe0" "\xcf\xee\xef\xfb\xf2\xea\xe0 \xf1\xee\xe5\xe4\xe8\xed\xe5\xed\xe8\xff" "\xc8\xec\xff \xf4\xe0\xe9\xeb\xe0" \xcf\xee\xeb\xf3\xf7\xe5\xed\xee "\xd0\xe0\xe7\xec\xe5\xf0" "\xce\xf1\xf2\xe0\xeb\xee\xf1\xfc" "\xcf\xf0\xee\xf8\xeb\xee" "http://ineum.narod.ru/g06s.htm" "$EXEDIR\g06s.htm"
    Pop $0 # return value = exit code, "OK" if OK
    MessageBox MB_OK "Download Status: $0"

SectionEnd
