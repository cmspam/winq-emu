@echo off
REM ============================================================
REM  WINQ-EMU Alpha 5 - VM Launcher
REM
REM  Launches a Linux VM with:
REM    - WHPX hardware acceleration (-cpu host)
REM    - Venus Vulkan GPU forwarding
REM    - Virtio sound
REM
REM  Mouse: Click inside the window to use. Press Ctrl+Alt+G
REM  to release the mouse back to Windows.
REM ============================================================

setlocal

REM --- Configuration (edit these to suit your setup) -----------

REM Path to your Linux VM disk image (qcow2 format)
set DISK_IMAGE=%~dp0vm\disk.qcow2

REM RAM and CPU allocation
set VM_MEMORY=8G
set VM_CPUS=8

REM GPU shared memory for blob resources
set GPU_HOSTMEM=4G

REM SSH port forwarding (connect with: ssh -p 2223 user@localhost)
set SSH_PORT=2223

REM -------------------------------------------------------------

if not exist "%DISK_IMAGE%" (
    echo.
    echo  ERROR: No disk image found at:
    echo    %DISK_IMAGE%
    echo.
    echo  To get started, create a "vm" folder next to this script
    echo  and place your Linux qcow2 disk image there as "disk.qcow2".
    echo.
    pause
    exit /b 1
)

echo.
echo  WINQ-EMU Alpha 5
echo  ================
echo  Disk:   %DISK_IMAGE%
echo  Memory: %VM_MEMORY%  CPUs: %VM_CPUS%
echo  SSH:    localhost:%SSH_PORT%
echo.
echo  Mouse:  Click to capture, Ctrl+Alt+G to release
echo.

"%~dp0bin\qemu-system-x86_64.exe" ^
  -machine q35,accel=whpx ^
  -cpu host ^
  -m %VM_MEMORY% ^
  -smp %VM_CPUS% ^
  -drive file="%DISK_IMAGE%",format=qcow2,if=virtio ^
  -device virtio-vga-gl,blob=on,hostmem=%GPU_HOSTMEM%,venus=on ^
  -display win32-gl ^
  -device virtio-sound-pci ^
  -usb -device usb-tablet ^
  -device virtio-net-pci,netdev=net0 ^
  -netdev user,id=net0,hostfwd=tcp::%SSH_PORT%-:22

if errorlevel 1 (
    echo.
    echo  VM exited with an error. Common fixes:
    echo    - Enable "Windows Hypervisor Platform" in Windows Features
    echo    - Update your GPU drivers for Vulkan support
    echo.
    pause
)
