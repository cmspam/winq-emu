WINQ-EMU Alpha 6
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

  - virtio-9p folder sharing (NEW in Alpha 6)
    Share Windows folders with the Linux guest. Pick a host folder
    and a mount tag in the GUI launcher's Folder Sharing tab, then
    mount it inside the guest with:
        sudo mount -t 9p -o trans=virtio,version=9p2000.L <tag> /mnt/<tag>

  - VA-API hardware video decode (NEW in Alpha 6)
    Linux guest apps that use libva-gallium (ffmpeg, mpv, VLC,
    Haruna, GStreamer) can hardware-decode video through the
    Windows host GPU's DXVA pipeline. See "VIDEO DECODE" below
    for codec list and a known issue with Chromium/Brave.


QUICK START
-----------

  1. Place your Linux qcow2 disk image at:
       <install-dir>\vm\disk.qcow2

  2. Double-click WINQ-EMU.exe (GUI) or launch-vm.bat (script)

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


FOLDER SHARING (9P)
-------------------

  The GUI launcher (WINQ-EMU.exe) has a "Folder Sharing" tab.
  Add one or more host folders with a mount tag per folder,
  then save or launch the VM. The shares are forwarded over
  virtio-9p.

  Inside the Linux guest, mount a share on demand with:

      sudo mkdir -p /mnt/<tag>
      sudo mount -t 9p -o trans=virtio,version=9p2000.L <tag> /mnt/<tag>

  Or add this line to /etc/fstab for automatic mount at boot:

      <tag>  /mnt/<tag>  9p  trans=virtio,version=9p2000.L,nofail,_netdev  0 0

  Replace <tag> with whatever mount tag you set in the launcher.
  The share is bidirectional: changes on the Windows side appear
  in the guest and vice versa.


VIDEO DECODE (VA-API)
---------------------

  The Linux guest ships a Mesa gallium VA-API driver ("virgl")
  that forwards video decode requests to the Windows host. The
  host uses D3D11 VideoDecoder (DXVA) to run the actual decode
  on your GPU. Any libva-gallium consumer will automatically
  pick this up:

      ffmpeg -hwaccel vaapi -i <video> ...
      mpv --hwdec=vaapi
      vlc           (when configured for vaapi)
      Haruna / Kaffeine / other mpv-based players
      GStreamer (via va-plugin)

  Supported codecs for decode:

      H.264 / AVC       Constrained Baseline, Main, High, 4:2:0 8-bit
      HEVC / H.265      Main (8-bit), Main10 (10-bit)
      VP9               Profile 0 (8-bit), Profile 2 (10-bit)
      AV1               Main Profile 0 (8-bit)

  Verify inside the guest with:
      vainfo

  Known issue: Chromium and Chromium-derivatives
    Hardware video decode does NOT work correctly in Chromium-
    based browsers (Chrome, Brave, Edge, Opera, Vivaldi, etc.)
    on this platform. Chromium's VaapiVideoDecoder imports the
    exported dma-buf through ANGLE + Skia's Vulkan renderer;
    that code path cannot correctly sample the output and the
    video element renders as garbled stripes or flashes of
    unrelated GPU memory.

    Workaround: disable hardware video acceleration in the
    browser's settings.
        Brave:  chrome://settings/system > uncheck
                "Use hardware acceleration when available"
                OR open chrome://flags, search "hardware-video-
                decoding", set to "Disabled".
        Chrome: same paths.

    VA-API works correctly in every other tested consumer
    (ffmpeg, mpv, Haruna, VLC, GStreamer). Firefox is not
    affected — it uses a different compositor path.


CUSTOMIZATION
-------------

  Edit launch-vm.bat to change:

    VM_MEMORY   - RAM allocation (default: 8G)
    VM_CPUS     - CPU count (default: 8)
    GPU_HOSTMEM - GPU shared memory (default: 4G)
    SSH_PORT    - SSH port forwarding (default: 2223)
    DISK_IMAGE  - Path to your disk image

  Or use the GUI launcher (WINQ-EMU.exe) for interactive
  configuration including folder sharing.


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

  Garbled video in Chromium / Brave / Edge
    Known issue, see "VIDEO DECODE" above. Disable hardware
    video acceleration in the browser. Other players
    (mpv, VLC, Haruna) work correctly.

  9p share does not mount
    Make sure you're using the version=9p2000.L option. The
    mount tag is case-sensitive and must match what you set
    in the launcher.


LICENSE
-------

  QEMU: GPL-2.0 (https://www.qemu.org/)
  virglrenderer: MIT (https://gitlab.freedesktop.org/virgl/virglrenderer)
  Runtime libraries: MSYS2/UCRT64, various open-source licenses
