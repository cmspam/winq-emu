# WINQ-EMU

**[Project Page](https://cmspam.github.io/winq-emu/)** | **[Download](https://github.com/cmspam/winq-emu/releases)**

The best way to run a full graphical Linux desktop on Windows — with real Vulkan GPU acceleration, Wayland compositor support, and host folder sharing.

## Three Things That Make It Different

- **Better WHPX Support** — Full `-cpu host` passthrough (not available in upstream QEMU), hybrid CPU P-core/E-core topology detection, sub-millisecond timer resolution
- **Venus Vulkan GPU Forwarding** — Your Linux VM uses your real GPU for Vulkan. No software rasterization.
- **Enhanced SDL Display** — DPI-aware SDL display with USB tablet support (no freezing), automatic host refresh rate matching, and Venus Vulkan GPU acceleration.

## Download

Grab the latest installer from [Releases](https://github.com/cmspam/winq-emu/releases).

## Quick Start

1. Install WINQ-EMU
2. Place a Linux qcow2 image in the `vm` folder
3. Double-click `WINQ-EMU.exe` (GUI) or `launch-vm.bat` (script)
4. SSH in: `ssh -p 2223 user@localhost`

## Performance

SuperTuxKart, Vulkan renderer, default settings, CachyOS on both:

| | FPS |
|---|---|
| **WINQ-EMU (Venus)** | 410 |
| WSL2 (CachyOS) | 226 |

## Features

- **Vulkan**: Works everywhere (Wayland + X11) via Venus
- **OpenGL**: Works everywhere via virgl (GL forwarding)
- **Zink (GL-over-Vulkan)**: Works on Wayland and X11/XWayland
- **Folder sharing**: virtio-9p host ↔ guest folders via the GUI launcher's Folder Sharing tab
- **Hardware video decode (experimental)**: VA-API for H.264, HEVC (Main / Main10), VP9 (Profile 0 / Profile 2), AV1 — routed through the host GPU's DXVA pipeline. Opt-in from the Experimental tab.

**Important**: Use BIOS boot, not EFI. EFI boot causes a timing issue that tanks Vulkan performance to ~5 FPS.

## Folder Sharing (9p)

Open the GUI launcher, go to the **Folder Sharing** tab, add a host folder with a mount tag, launch the VM, then inside the guest:

```
sudo mkdir -p /mnt/<tag>
sudo mount -t 9p -o trans=virtio,version=9p2000.L <tag> /mnt/<tag>
```

For auto-mount at boot, add to `/etc/fstab`:

```
<tag>  /mnt/<tag>  9p  trans=virtio,version=9p2000.L,nofail,_netdev  0 0
```

Changes propagate both ways.

## Hardware Video Decode (VA-API) — Experimental

VA-API is off by default. Enable it from the **Experimental** tab in the launcher (or set `WINQ_VAAPI=1` in the environment before launching `qemu-system-x86_64.exe` directly).

With it enabled, Mesa's `virgl` Gallium VA driver in the guest forwards decode commands to virglrenderer on the host, which runs the actual decode on `ID3D11VideoDecoder` (DXVA). Decoded frames flow back to the guest as virtio-gpu resources that libva consumers read through `vaGetImage` / `vaMapBuffer`.

Supported codecs:

| Codec | Profiles |
|---|---|
| H.264 | Constrained Baseline, Main, High (8-bit 4:2:0) |
| HEVC  | Main (8-bit), Main10 (10-bit) |
| VP9   | Profile 0 (8-bit), Profile 2 (10-bit) |
| AV1   | Main Profile 0 (8-bit) |

Confirm inside the guest with `vainfo`.

**Known-good consumers** (anything that uses the `vaGetImage` / vaapi-copy path): **ffmpeg, mpv (`--hwdec=vaapi-copy`), VLC, Haruna, GStreamer**.

### Why it's experimental

Chromium-based browsers (Chrome, Brave, Edge, Opera, Vivaldi), and mpv with `--hwdec=vaapi` (non-copy) take a **zero-copy** path via `vaExportSurfaceHandle` + `EGL_LINUX_DMA_BUF_EXT`. That path is not yet functional through virglrenderer on Windows — you'll see black or garbled video. For now the only reliable workarounds for Chromium users are:

- Disable hardware video acceleration in the browser:
  - `chrome://settings/system` → uncheck "Use hardware acceleration when available", or
  - `chrome://flags` → search for "hardware-video-decoding" → set to **Disabled**.
- Or prefer mpv / VLC / Haruna for video playback, where the copy path works.

Firefox and every non-Chromium consumer tested above work correctly.

## Guest Requirements

- Linux distro with Mesa 26.0+ (Fedora 41+, Ubuntu 25.04+, Arch, CachyOS, etc.) for the VA-API video driver
- Mesa 24.0+ is enough for Vulkan-only use
- Wayland compositor recommended (KDE Plasma, GNOME)

## Building from Source

### Prerequisites

- [MSYS2](https://www.msys2.org/) with the UCRT64 environment
- .NET Framework 4 (included with Windows — provides `csc.exe` at `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\`)

### Build Order

1. **Build virglrenderer** — see [winq-emu-virglrenderer](https://github.com/cmspam/winq-emu-virglrenderer). Run `ninja -C builddir install` to install `libvirglrenderer-1.dll` to `/ucrt64/bin`.
2. **Build QEMU** — see [winq-emu-qemu](https://github.com/cmspam/winq-emu-qemu). Produces `qemu-system-x86_64.exe`, `qemu-system-x86_64w.exe` (no console window), and `qemu-img.exe`.
3. **Build the frontend** (from this repo):
   ```
   csc.exe /target:winexe /out:installer\WINQ-EMU.exe /win32icon:launcher\winq-emu.ico /r:System.Windows.Forms.dll /r:System.Drawing.dll launcher\WINQ-EMU.cs
   ```
4. **Collect DLLs** — copy all UCRT64 DLL dependencies into `installer\bin\`. Use `ldd qemu-system-x86_64.exe` from the UCRT64 shell to find them. Also copy `winq-emu.ico` into `installer\icons\` for the window icon.
5. **Build the installer** (requires `mingw-w64-ucrt-x86_64-nsis`):
   ```bash
   cd installer && makensis installer.nsi
   ```

## Status

**Alpha 8** - It works, it's fast, but expect some rough edges. Stability improvements are ongoing.

### What's New in Alpha 8
- **Rebased on QEMU 11.0.0** (final release, up from 11.0.0-rc3 in Alpha 7).
- **Rebased on virglrenderer 1.3.0** (stable release, replacing the live-`main` snapshot used in Alpha 7).
- No functional changes vs Alpha 7 — this release exists to track stable upstream releases instead of release candidates / development tips.

### What Was New in Alpha 7
- **Zink on Wayland** — Zink (GL-over-Vulkan) now works under native Wayland compositors, not only X11/XWayland. Enabled by a Windows dma-buf shim that synthesizes `VK_EXT_external_memory_dma_buf` and `VK_EXT_image_drm_format_modifier` on top of the host Vulkan ICD so guest Wayland compositors can import Vulkan-allocated surfaces.
- **VA-API moved to experimental** — now off by default; enable from the launcher's **Experimental** tab. The copy path (mpv/VLC/Haruna) remains reliable; the zero-copy path used by Chromium is still broken and is being tracked as a separate project.
- **Rebased** on latest upstream QEMU + virglrenderer.

### What Was New in Alpha 6
- **virtio-9p folder sharing** — share Windows folders with the Linux guest via the Folder Sharing tab in the GUI launcher. Uses the Windows port of QEMU's 9pfs backend.
- **VA-API hardware video decode** — H.264, HEVC (Main / Main10), VP9 (Profile 0 / Profile 2), and AV1 (Main Profile 0) decode through the host GPU's DXVA pipeline.
- **Rebased** on latest upstream QEMU + virglrenderer.

### What Was New in Alpha 5
- **USB mouse fix**: Configurations generated by the GUI now correctly include `-usb -device usb-tablet` for proper mouse input
- **Disk image tools**: `qemu-img` is now bundled with the installer, so the GUI can create qcow2 disk images out of the box
- **Rebased on QEMU v11.0.0-rc3** and latest upstream virglrenderer

### What Was New in Alpha 4
- **Keyboard fix**: Dynamic keyboard layout detection for OEM keys (backslash, brackets, etc.) — works correctly on US, UK, ISO, JIS, and all other layouts
- **Install location**: Default install to C:\WINQ-EMU instead of AppData

## Contributing

Anyone is welcome to look at, modify, and/or merge these changes into upstream projects. The patches are intentionally kept minimal and clean for upstreaming.

## License

- QEMU: GPL-2.0
- virglrenderer: MIT
- Launcher/installer: MIT
