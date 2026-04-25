WINQ-EMU Alpha 8
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

  - Zink (GL-over-Vulkan) on Wayland
    Zink now works under native Wayland compositors, not only X11.
    A Windows dma-buf shim in virglrenderer synthesizes the Linux
    dma-buf + modifier Vulkan extensions so guest Wayland compositors
    can import Vulkan-allocated surfaces. No configuration required.

  - virtio-9p folder sharing
    Share Windows folders with the Linux guest. Pick a host folder
    and a mount tag in the GUI launcher's Folder Sharing tab, then
    mount it inside the guest with:
        sudo mount -t 9p -o trans=virtio,version=9p2000.L <tag> /mnt/<tag>

  - VA-API hardware video decode (EXPERIMENTAL)
    Off by default. Enable from the launcher's Experimental
    tab. Works with ffmpeg, mpv (--hwdec=vaapi-copy), VLC, Haruna,
    GStreamer. Does NOT work with Chromium-based browsers — see
    "VIDEO DECODE" below.


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

  - A Linux VM image with Mesa 24.0+ (for the Venus Vulkan driver);
    Mesa 26.0+ if you also want to enable experimental VA-API decode.


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
    Zink translates OpenGL calls to Vulkan (via Venus). This works
    correctly on both native Wayland and X11/XWayland,
    so you can opt an app into Zink for potentially better perf:

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


VIDEO DECODE (VA-API) -- EXPERIMENTAL
-------------------------------------

  Off by default. Enable from the GUI launcher's "Experimental" tab
  (checkbox "Enable VA-API hardware video decode"). If launching
  QEMU directly, set WINQ_VAAPI=1 in the environment.

  With VA-API on, the guest ships a Mesa gallium VA-API driver
  ("virgl") that forwards video decode requests to the Windows host.
  The host uses D3D11 VideoDecoder (DXVA) to run the actual decode
  on your GPU. Any libva-gallium consumer will pick this up:

      ffmpeg -hwaccel vaapi -i <video> ...
      mpv --hwdec=vaapi-copy
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

  Why experimental: Chromium-based browsers do not work
    Chromium (Chrome, Brave, Edge, Opera, Vivaldi) and mpv with
    the non-copy "--hwdec=vaapi" take a zero-copy dma-buf import
    path via vaExportSurfaceHandle. That path is not yet functional
    through virglrenderer on Windows -- you will see black or
    garbled video when those apps try to hardware-decode.

    Workarounds:
      - Use --hwdec=vaapi-copy in mpv (works correctly).
      - Disable hardware video acceleration in Chromium browsers:
          chrome://settings/system  >  uncheck
          "Use hardware acceleration when available"
            OR open chrome://flags, search "hardware-video-
            decoding", set to "Disabled".

    VA-API works correctly in every other tested consumer (ffmpeg,
    mpv copy, Haruna, VLC, GStreamer). Firefox is not affected.


CUSTOMIZATION
-------------

  Edit launch-vm.bat to change:

    VM_MEMORY   - RAM allocation (default: 8G)
    VM_CPUS     - CPU count (default: 8)
    GPU_HOSTMEM - GPU shared memory (default: 4G)
    SSH_PORT    - SSH port forwarding (default: 2223)
    DISK_IMAGE  - Path to your disk image

  Or use the GUI launcher (WINQ-EMU.exe) for interactive
  configuration, including folder sharing and the experimental
  VA-API toggle.


GUEST VM RECOMMENDATIONS
------------------------

  - Use a modern distro with Mesa 24.0+ (Fedora 40+, Ubuntu 24.04+,
    Arch/CachyOS, etc.). Mesa 26.0+ for experimental VA-API.
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

  Black video in Chromium / Brave / Edge / mpv --hwdec=vaapi
    Known issue, see "VIDEO DECODE" above. Disable hardware
    video acceleration in the browser, or use mpv's vaapi-copy
    hwdec mode instead of vaapi.

  9p share does not mount
    Make sure you're using the version=9p2000.L option. The
    mount tag is case-sensitive and must match what you set
    in the launcher.


LICENSE
-------

  QEMU: GPL-2.0 (https://www.qemu.org/)
  virglrenderer: MIT (https://gitlab.freedesktop.org/virgl/virglrenderer)
  Runtime libraries: MSYS2/UCRT64, various open-source licenses
