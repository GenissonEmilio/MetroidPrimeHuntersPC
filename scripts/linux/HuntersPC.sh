#!/usr/bin/env bash
set -euo pipefail
app_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# Prefer the GLFW shipped by OpenTK, including on systems with an older GLFW.
export LD_LIBRARY_PATH="$app_dir${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
if [ "$#" -eq 1 ] && [ -f "$1" ]; then
  "$app_dir/HuntersPC" --pc-import "$1"
  set --
fi
exec "$app_dir/HuntersPC" "$@"
