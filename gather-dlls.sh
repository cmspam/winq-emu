#!/bin/bash
# Walks ldd graph for the qemu binaries + virglrenderer, copies every DLL
# found under /ucrt64/bin into installer/bin/.
set -euo pipefail

UCRT_BIN=/c/msys64/ucrt64/bin
INSTALLER_BIN=installer/bin

declare -A SEEN

walk() {
    local target="$1"
    if [[ -n "${SEEN[$target]:-}" ]]; then
        return
    fi
    SEEN[$target]=1

    # ldd will list both system DLLs (KERNEL32.dll, etc.) and ucrt64 ones.
    # We only copy ucrt64 ones; system DLLs ship with Windows.
    while read -r line; do
        # Format: "name => path (offset)"
        local name="${line%% *}"
        local path
        path=$(echo "$line" | sed -E 's/.* => ([^ ]+) .*/\1/')
        if [[ "$path" == /ucrt64/bin/* ]] || [[ "$path" == "$UCRT_BIN"/* ]]; then
            local fname="$(basename "$path")"
            if [[ ! -f "$INSTALLER_BIN/$fname" ]]; then
                cp "$path" "$INSTALLER_BIN/"
                echo "  + $fname"
            fi
            walk "$path"
        fi
    done < <(/c/msys64/usr/bin/bash.exe -lc "ldd '$target' 2>/dev/null" || true)
}

echo "Walking dependencies..."
for bin in "$INSTALLER_BIN"/qemu-system-x86_64.exe "$INSTALLER_BIN"/qemu-img.exe "$INSTALLER_BIN"/libvirglrenderer-1.dll; do
    echo "  [$bin]"
    walk "$bin"
done

echo "Done. Total DLLs in installer/bin/:"
ls -1 "$INSTALLER_BIN"/*.dll | wc -l
