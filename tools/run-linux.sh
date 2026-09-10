#!/usr/bin/env bash
set -euo pipefail

project_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
binary="$project_dir/build/linux/Petalfell.x86_64"

if [ ! -x "$binary" ]; then
  echo "No Linux build found. Build it first with:" >&2
  echo "  $project_dir/tools/build-linux.sh" >&2
  exit 1
fi

exec "$binary" "$@"
