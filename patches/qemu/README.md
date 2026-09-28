# QEMU Patches

Patches to apply on top of a QEMU checkout when building the `qemu-system-x86_64.exe` used by this project.

## 0001-ui-sdl2-add-clipboard-sharing-via-qemu-vdagent.patch

Adds host clipboard support to the SDL display (`ui/sdl2-clipboard.c`), enables `--enable-spice` in `build.sh` to compile the `qemu-vdagent` chardev backend, and adds the corresponding launch flags. See the commit message inside the patch for full detail.

Apply on top of an `alpha10`-based QEMU tree:

```bash
git am patches/qemu/0001-ui-sdl2-add-clipboard-sharing-via-qemu-vdagent.patch
```

Enable the resulting checkbox from the launcher's Devices tab, or add the flags manually:

```bash
-device virtio-serial-pci \
-chardev qemu-vdagent,id=vdagent,name=vdagent,clipboard=on \
-device virtserialport,chardev=vdagent,id=vdagent,name=com.redhat.spice.0
```

Guest side needs `spice-vdagent` installed. Clipboard sync works for X11/XWayland guest apps; native Wayland apps under compositors without an X11-clipboard bridge (e.g. niri without one) are not yet reached — see the patch's commit message for why.
