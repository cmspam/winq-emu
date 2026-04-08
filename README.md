# WINQ-EMU

WINQ-EMU is an optimized build of QEMU for Windows that gives Linux VMs direct access to your host GPU for hardware-accelerated Vulkan rendering.

## Download

Grab the latest installer from [Releases](https://github.com/cmspam/winq-emu/releases).

## What You Get

- **Venus Vulkan GPU forwarding** - Your Linux VM uses your real GPU for Vulkan. No software rasterization.
- **WHPX with -cpu host** - Full CPU passthrough including AVX-512, hybrid cores, everything your CPU has. Upstream QEMU doesn't support `-cpu host` with WHPX — we fixed that.
- **virtio-gpu blob resources** - Efficient zero-copy shared memory between host and guest.
- **Graphical launcher** - No terminal needed. Point, click, VM.
- **One-click installer** - All DLLs, firmware, and tools bundled.

## Quick Start

1. Install WINQ-EMU
2. Place a Linux qcow2 image in the `vm` folder
3. Double-click `WINQ-EMU.exe` or `launch-vm.bat`
4. SSH in: `ssh -p 2223 user@localhost`

## Performance

SuperTuxKart, Vulkan renderer, default settings, CachyOS on both:

| | FPS |
|---|---|
| **WINQ-EMU (Venus)** | 410 |
| WSL2 (CachyOS) | 226 |

## Graphics

- **Vulkan**: Works everywhere (Wayland + X11) via Venus
- **OpenGL**: Works everywhere via virgl (GL forwarding)
- **Zink (GL-over-Vulkan)**: Works on X11/XWayland only (not native Wayland)

**Important**: Use BIOS boot, not EFI. EFI boot causes a timing issue that tanks Vulkan performance to ~5 FPS.

## Guest Requirements

- Linux distro with Mesa 24.0+ (Fedora 40+, Ubuntu 24.04+, Arch, CachyOS, etc.)
- Wayland compositor recommended (KDE Plasma, GNOME)

## Building from Source

See the individual repos:
- [winq-emu-virglrenderer](https://github.com/cmspam/winq-emu-virglrenderer) - Venus Windows port
- [winq-emu-qemu](https://github.com/cmspam/winq-emu-qemu) - QEMU with WHPX + Venus

Both build in MSYS2 UCRT64 on Windows.

## Status

**Alpha 1** - It works, it's fast, but expect some rough edges. Stability improvements are ongoing.

## Contributing

Anyone is welcome to look at, modify, and/or merge these changes into upstream projects. The patches are intentionally kept minimal and clean for upstreaming.

## License

- QEMU: GPL-2.0
- virglrenderer: MIT
- Launcher/installer: MIT
