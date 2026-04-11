WINQ-EMU Alpha 5
================

WINQ-EMU is an optimized build of QEMU for Windows with:

  - Enhanced WHPX support with -cpu host passthrough
    Run Linux VMs at native CPU speed. Your full host CPU feature
    set (AVX-512, hybrid cores, etc.) is passed through to the guest.

  - Venus Vulkan GPU acceleration
    Linux guest applications can use your real GPU for Vulkan
    rendering via the Venus protocol. No software rasterization.

  - virtio-gpu with blob resources
    Efficient zero-copy shared memory between host and guest GPU.


QUICK START
-----------

  1. Place your Linux qcow2 disk image at:
       <install-dir>\vm\disk.qcow2

  2. Double-click launch-vm.bat

  3. SSH into the VM:
       ssh -p 2223 user@localhost


REQUIREMENTS
------------

  - Windows 10/11 with "Windows Hypervisor Platform" enabled
    (Settings > Apps > Optional Features > More Windows Features
     > check "Windows Hypervisor Platform")

  - A GPU with up-to-date Vulkan drivers (Intel, AMD, or NVIDIA)

  - A Linux VM image with Mesa 24.0+ (for the Venus Vulkan driver)


GRAPHICS GUIDE
--------------

  Vulkan
    Works system-wide (Wayland and X11). The Venus driver is
    automatically available to all Vulkan applications in the guest.
    Verify with: vulkaninfo --summary

  OpenGL (virgl)
    OpenGL is provided via virgl (GL forwarding to the host).
    This works everywhere including native Wayland compositors.
    Most desktop environments use this by default.

  OpenGL via Zink
    Zink translates OpenGL calls to Vulkan (via Venus). It can
    offer better performance in some cases, but currently only
    works with X11 and XWayland applications. Native Wayland
    clients will not display output when using Zink.

    To try Zink for a specific app:
      MESA_LOADER_DRIVER_OVERRIDE=zink <application>

  Important: Use BIOS boot, not EFI/UEFI
    EFI boot causes a timing issue with Vulkan initialization that
    results in very slow performance (~5 FPS). Always use the
    default BIOS boot for full Vulkan speed. The included
    launch-vm.bat uses BIOS boot by default.


CUSTOMIZATION
-------------

  Edit launch-vm.bat to change:

    VM_MEMORY   - RAM allocation (default: 8G)
    VM_CPUS     - CPU count (default: 8)
    GPU_HOSTMEM - GPU shared memory (default: 4G)
    SSH_PORT    - SSH port forwarding (default: 2223)
    DISK_IMAGE  - Path to your disk image


GUEST VM RECOMMENDATIONS
------------------------

  - Use a modern distro with Mesa 24.0+ (Fedora 40+, Ubuntu 24.04+,
    Arch/CachyOS, etc.)
  - Use a Wayland compositor (KDE Plasma, GNOME) for best results
  - Install virtio guest drivers (usually included by default)


TROUBLESHOOTING
---------------

  "WHPX not available"
    Enable "Windows Hypervisor Platform" in Windows Features.
    Reboot after enabling.

  "Failed to initialize Venus"
    Your GPU may lack Vulkan support. Run "vulkaninfo" on the
    Windows host to check. Update your GPU drivers.

  No sound
    The VM uses virtio-sound. If there's no sound, check that
    audio is not muted in both the host and guest, and that
    the guest kernel has the virtio-snd driver.

  Mouse
    Click inside the QEMU window to capture the mouse.
    Press Ctrl+Alt+G to release it back to Windows.


LICENSE
-------

  QEMU: GPL-2.0 (https://www.qemu.org/)
  virglrenderer: MIT (https://gitlab.freedesktop.org/virgl/virglrenderer)
  Runtime libraries: MSYS2/UCRT64, various open-source licenses
