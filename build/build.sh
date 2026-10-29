#!/bin/sh
# Builds EasyJob for several operating systems / CPUs and zips every build for distribution.
# Works from macOS, Linux and Windows (Git Bash / WSL); .NET can cross-compile all targets from any of them.
#
#   build/build.sh                                   everything: 6 platforms x 2 variants
#   build/build.sh --rids "win-x64 osx-arm64"        only these platforms
#   build/build.sh --variants portable               only the self-contained build
#   build/build.sh --version 2.1.0                   override the version (default: <Version> in EasyJob.csproj)
#   build/build.sh --clean                           delete build/out and build/dist first
#
# Variants (the zip name says which one):
#   EasyJob-<version>-<rid>-portable.zip   runs on its own, the .NET runtime is included (bigger)
#   EasyJob-<version>-<rid>.zip            small, needs the .NET 10 runtime installed on the machine
#
# Results: build/dist/*.zip (and the unzipped publish folders in build/out/).
set -e

HERE="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$HERE/.." && pwd)"
PROJECT="$ROOT/EasyJob/EasyJob.csproj"
OUT="$HERE/out"
DIST="$HERE/dist"

RIDS="win-x64 win-arm64 linux-x64 linux-arm64 osx-x64 osx-arm64"
VARIANTS="portable framework"
VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$PROJECT" | head -n 1)"
CLEAN=0

while [ $# -gt 0 ]; do
  case "$1" in
    --rids) RIDS="$2"; shift 2 ;;
    --variants) VARIANTS="$2"; shift 2 ;;
    --version) VERSION="$2"; shift 2 ;;
    --clean) CLEAN=1; shift ;;
    -h|--help) sed -n '2,19p' "$0"; exit 0 ;;
    *) echo "unknown option: $1 (try --help)" >&2; exit 1 ;;
  esac
done

command -v dotnet >/dev/null 2>&1 || { echo "dotnet SDK 10 was not found on PATH" >&2; exit 1; }
command -v zip >/dev/null 2>&1 || command -v ditto >/dev/null 2>&1 || { echo "'zip' was not found on PATH" >&2; exit 1; }

[ "$CLEAN" = 1 ] && rm -rf "$OUT" "$DIST"
mkdir -p "$OUT" "$DIST"

make_zip() { # make_zip <parent-dir> <entry-name> <zip-path> [keep-xattrs]
  rm -f "$3"
  if [ "$4" = keep-xattrs ] && command -v ditto >/dev/null 2>&1; then
    # A signed .app keeps the signatures of its .dll files in extended attributes; only ditto preserves them.
    (cd "$1" && ditto -c -k --keepParent "$2" "$3")
  elif command -v zip >/dev/null 2>&1; then
    (cd "$1" && zip -q -r -y -X "$3" "$2")             # Unix permissions are stored; no macOS "._" files
  else
    (cd "$1" && ditto -c -k --norsrc --noextattr --keepParent "$2" "$3")
  fi
}

echo "EasyJob $VERSION  |  platforms: $RIDS  |  variants: $VARIANTS"

for rid in $RIDS; do
  for variant in $VARIANTS; do
    case "$variant" in
      portable)  sc=true;  suffix="-portable" ;;
      framework) sc=false; suffix="" ;;
      *) echo "unknown variant '$variant' (portable | framework)" >&2; exit 1 ;;
    esac

    echo ""
    echo "== $rid ($variant)"
    publish="$OUT/$rid-$variant"
    rm -rf "$publish"
    dotnet publish "$PROJECT" -c Release -r "$rid" --self-contained "$sc" \
      -p:Version="$VERSION" -p:DebugType=None -p:DebugSymbols=false \
      -o "$publish" --nologo -v quiet

    stage="$OUT/stage-$rid-$variant"
    rm -rf "$stage"; mkdir -p "$stage"
    zipfile="$DIST/EasyJob-$VERSION-$rid$suffix.zip"

    case "$rid" in
      osx-*)
        # A real EasyJob.app (icon, Info.plist, ad-hoc signature when built on a Mac).
        "$ROOT/tools/make-macos-app.sh" "$publish" "$stage/EasyJob.app" "$VERSION" >/dev/null
        make_zip "$stage" "EasyJob.app" "$zipfile" keep-xattrs
        ;;
      *)
        mkdir -p "$stage/EasyJob"
        cp -R "$publish"/. "$stage/EasyJob/"
        cp "$ROOT/LICENSE.txt" "$ROOT/README.md" "$ROOT/policy.example.json" "$stage/EasyJob/"
        case "$rid" in linux-*) chmod +x "$stage/EasyJob/EasyJob" ;; esac
        make_zip "$stage" "EasyJob" "$zipfile"
        ;;
    esac
    rm -rf "$stage"
    echo "   -> $zipfile ($(du -h "$zipfile" | cut -f1))"
  done
done

echo ""
echo "Done. Zips are in $DIST"
