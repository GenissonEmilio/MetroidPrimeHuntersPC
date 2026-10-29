#!/usr/bin/env bash
set -euo pipefail
project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
output="${1:-$project_root/dist/HuntersPC-linux-x64}"
dotnet_bin="${DOTNET_BIN:-dotnet}"
"$dotnet_bin" publish "$project_root/src/MphRead/MphRead.csproj" \
  -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=false \
  -p:DebugType=None -p:DebugSymbols=false "-p:PathMap=$project_root=/src" \
  -o "$output" --nologo
python3 "$project_root/scripts/linux-support.py" "$output"
printf 'Pronto: %s/HuntersPC.sh\n' "$output"
