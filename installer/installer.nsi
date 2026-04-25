; WINQ-EMU Alpha 8 Installer
; Optimized QEMU for Windows with WHPX host CPU, Venus Vulkan GPU,
; 9p folder sharing, and VA-API video decode.

!include "MUI2.nsh"
!include "FileFunc.nsh"
!include "WordFunc.nsh"

; ---- General ----
Name "WINQ-EMU Alpha 8"
OutFile "..\WINQ-EMU-Alpha8-Setup.exe"
InstallDir "C:\WINQ-EMU"
InstallDirRegKey HKCU "Software\WINQ-EMU" "InstallDir"
RequestExecutionLevel admin
SetCompressor /SOLID lzma

; ---- Version Info ----
VIProductVersion "0.7.0.0"
VIAddVersionKey "ProductName" "WINQ-EMU"
VIAddVersionKey "FileDescription" "WINQ-EMU Alpha 8 - Optimized QEMU for Windows"
VIAddVersionKey "FileVersion" "0.7.0"
VIAddVersionKey "LegalCopyright" "GPL-2.0"

; ---- MUI Settings ----
!define MUI_ABORTWARNING
!define MUI_ICON "icons\winq-emu.ico"
!define MUI_UNICON "icons\winq-emu.ico"
!define MUI_WELCOMEPAGE_TITLE "Welcome to WINQ-EMU Alpha 8"
!define MUI_WELCOMEPAGE_TEXT "WINQ-EMU is an optimized build of QEMU for Windows featuring:$\r$\n$\r$\n\
    $\u2022  Enhanced WHPX with -cpu host passthrough$\r$\n\
    $\u2022  Venus Vulkan GPU acceleration$\r$\n\
    $\u2022  virtio-gpu blob resources$\r$\n\
    $\u2022  Enhanced SDL display with USB tablet and DPI awareness$\r$\n\
    $\u2022  Graphical VM launcher with Folder Sharing tab$\r$\n\
    $\u2022  virtio-9p folder sharing (Windows host $\u2194 Linux guest)$\r$\n\
    $\u2022  VA-API hardware video decode: H.264, HEVC, VP9, AV1$\r$\n\
    $\u2022  Virtio sound and networking$\r$\n$\r$\n\
This will install WINQ-EMU Alpha 8 on your computer.$\r$\n$\r$\n\
Click Next to continue."

; ---- Pages ----
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "LICENSE.txt"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

; ---- Installation ----
Section "WINQ-EMU Core (required)" SecCore
    SectionIn RO

    ; Binaries and DLLs
    SetOutPath "$INSTDIR\bin"
    File "bin\qemu-system-x86_64.exe"
    File "bin\qemu-system-x86_64w.exe"
    File "bin\qemu-img.exe"
    File "bin\libvirglrenderer-1.dll"
    File "bin\*.dll"
    File "icons\winq-emu.ico"

    ; Firmware (must be at bin\share\ for QEMU to find it)
    SetOutPath "$INSTDIR\bin\share"
    File "bin\share\*.bin"
    File "bin\share\*.rom"
    File /nonfatal "bin\share\*.fd"

    ; Firmware descriptors
    SetOutPath "$INSTDIR\bin\share\firmware"
    File /nonfatal "bin\share\firmware\*.json"

    ; Keymaps
    SetOutPath "$INSTDIR\bin\share\keymaps"
    File /r "bin\share\keymaps\*"

    ; GUI launcher, launch script, and docs
    SetOutPath "$INSTDIR"
    File "WINQ-EMU.exe"
    File "launch-vm.bat"
    File "README.txt"

    ; Icon
    SetOutPath "$INSTDIR"
    File "icons\winq-emu.ico"

    ; Desktop shortcut for GUI launcher
    CreateShortcut "$DESKTOP\WINQ-EMU.lnk" "$INSTDIR\WINQ-EMU.exe" \
                   "" "$INSTDIR\winq-emu.ico"

    ; Create VM directory
    CreateDirectory "$INSTDIR\vm"

    ; Write registry keys (per-user)
    WriteRegStr HKCU "Software\WINQ-EMU" "InstallDir" "$INSTDIR"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WINQ-EMU" \
                     "DisplayName" "WINQ-EMU Alpha 8"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WINQ-EMU" \
                     "UninstallString" '"$INSTDIR\uninstall.exe"'
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WINQ-EMU" \
                     "DisplayIcon" '"$INSTDIR\winq-emu.ico"'
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WINQ-EMU" \
                     "InstallLocation" "$INSTDIR"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WINQ-EMU" \
                     "Publisher" "WINQ-EMU Project"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WINQ-EMU" \
                     "DisplayVersion" "Alpha 8"
    WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WINQ-EMU" \
                       "NoModify" 1
    WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WINQ-EMU" \
                       "NoRepair" 1

    ; Estimate installed size
    ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
    IntFmt $0 "0x%08X" $0
    WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WINQ-EMU" \
                       "EstimatedSize" $0

    ; Uninstaller
    WriteUninstaller "$INSTDIR\uninstall.exe"
SectionEnd

Section "Add to user PATH" SecPath
    ReadRegStr $0 HKCU "Environment" "Path"
    StrCpy $0 "$0;$INSTDIR\bin"
    WriteRegExpandStr HKCU "Environment" "Path" $0
    SendMessage ${HWND_BROADCAST} ${WM_WININICHANGE} 0 "STR:Environment" /TIMEOUT=5000
SectionEnd

Section "Start Menu Shortcut" SecShortcuts
    CreateDirectory "$SMPROGRAMS\WINQ-EMU"
    CreateShortcut "$SMPROGRAMS\WINQ-EMU\WINQ-EMU.lnk" "$INSTDIR\WINQ-EMU.exe" \
                   "" "$INSTDIR\winq-emu.ico"
    CreateShortcut "$SMPROGRAMS\WINQ-EMU\Launch VM (script).lnk" "$INSTDIR\launch-vm.bat" \
                   "" "$INSTDIR\winq-emu.ico"
    CreateShortcut "$SMPROGRAMS\WINQ-EMU\README.lnk" "$INSTDIR\README.txt"
    CreateShortcut "$SMPROGRAMS\WINQ-EMU\Uninstall.lnk" "$INSTDIR\uninstall.exe"
SectionEnd

; ---- Descriptions ----
LangString DESC_SecCore ${LANG_ENGLISH} "QEMU with enhanced WHPX host CPU passthrough, Venus Vulkan GPU acceleration, VA-API video decode, virtio-9p folder sharing, and all required libraries."
LangString DESC_SecPath ${LANG_ENGLISH} "Add WINQ-EMU to your user PATH so you can run qemu-system-x86_64 from any terminal."
LangString DESC_SecShortcuts ${LANG_ENGLISH} "Create a Start Menu shortcut for launching VMs."

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SecCore} $(DESC_SecCore)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecPath} $(DESC_SecPath)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecShortcuts} $(DESC_SecShortcuts)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

; ---- Uninstaller ----
Section "Uninstall"
    RMDir /r "$INSTDIR\bin"
    Delete "$INSTDIR\WINQ-EMU.exe"
    Delete "$INSTDIR\launch-vm.bat"
    Delete "$INSTDIR\README.txt"
    Delete "$INSTDIR\winq-emu.ico"
    Delete "$INSTDIR\uninstall.exe"
    Delete "$DESKTOP\WINQ-EMU.lnk"

    ; Remove VM dir only if empty (don't delete user's disk images)
    RMDir "$INSTDIR\vm"
    RMDir "$INSTDIR"

    RMDir /r "$SMPROGRAMS\WINQ-EMU"

    ; Remove PATH entry
    ReadRegStr $0 HKCU "Environment" "Path"
    ${WordReplace} $0 ";$INSTDIR\bin" "" "+" $0
    WriteRegExpandStr HKCU "Environment" "Path" $0
    SendMessage ${HWND_BROADCAST} ${WM_WININICHANGE} 0 "STR:Environment" /TIMEOUT=5000

    DeleteRegKey HKCU "Software\WINQ-EMU"
    DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WINQ-EMU"
SectionEnd
