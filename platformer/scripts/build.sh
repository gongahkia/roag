#!/usr/bin/env bash
set -euo pipefail
repo_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_dir"
git submodule update --init --recursive
cmake -S desktop_version -B build -DMAKEANDPLAY=ON -DCMAKE_BUILD_TYPE=Release
cmake --build build --parallel "${BUILD_JOBS:-4}"
