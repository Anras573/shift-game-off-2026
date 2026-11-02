#!/usr/bin/env bash
# Packages a self-contained desktop publish folder into a player-ready archive.
#
#   scripts/package-desktop.sh <publish-dir> <rid> <version> <out-dir>
#
# Windows builds become a .zip. Linux and macOS builds become a .tar.gz so the
# GravityShift executable keeps its +x bit (GitHub artifact zips drop it).
# Prints the path of the created archive.
set -euo pipefail

publish_dir=$1
rid=$2
version=$3
out_dir=$4

name="gravity-shift-${version}-${rid}"
mkdir -p "$out_dir"
out_dir=$(cd "$out_dir" && pwd)

# Put the files in a named top-level folder so extracting doesn't spill into the player's cwd.
staging=$(mktemp -d)
trap 'rm -rf "$staging"' EXIT
cp -R "$publish_dir" "$staging/$name"
rm -f "$staging/$name"/*.pdb

case "$rid" in
  win-*)
    (cd "$staging" && zip -qr "$out_dir/$name.zip" "$name")
    echo "$out_dir/$name.zip"
    ;;
  linux-* | osx-*)
    chmod +x "$staging/$name/GravityShift"
    tar -C "$staging" -czf "$out_dir/$name.tar.gz" "$name"
    echo "$out_dir/$name.tar.gz"
    ;;
  *)
    echo "Unsupported runtime identifier: $rid" >&2
    exit 1
    ;;
esac
