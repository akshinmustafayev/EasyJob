#!/bin/sh
# Wraps a macOS publish folder into EasyJob.app so Finder and the Dock show the EasyJob icon.
#
#   dotnet publish EasyJob -c Release -r osx-arm64 --self-contained -o publish/osx-arm64
#   tools/make-macos-app.sh publish/osx-arm64 EasyJob.app [version]
#
# Zip the resulting EasyJob.app (for example: ditto -c -k --keepParent EasyJob.app EasyJob-osx-arm64.zip)
# and attach it to a GitHub release; the in-app updater understands zips that contain a .app bundle.
set -e

PUBLISH_DIR="$1"
APP="${2:-EasyJob.app}"
VERSION="${3:-2.0.0}"
HERE="$(cd "$(dirname "$0")" && pwd)"
ICON="$HERE/../EasyJob/Assets/icon.icns"

[ -d "$PUBLISH_DIR" ] || { echo "usage: $0 <publish-dir> [EasyJob.app] [version]" >&2; exit 1; }
[ -f "$PUBLISH_DIR/EasyJob" ] || { echo "$PUBLISH_DIR/EasyJob not found: publish for an osx runtime first" >&2; exit 1; }

rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp -R "$PUBLISH_DIR"/. "$APP/Contents/MacOS/"
chmod +x "$APP/Contents/MacOS/EasyJob"
cp "$ICON" "$APP/Contents/Resources/icon.icns"

cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>EasyJob</string>
  <key>CFBundleDisplayName</key><string>EasyJob</string>
  <key>CFBundleIdentifier</key><string>com.akshinmustafayev.easyjob</string>
  <key>CFBundleExecutable</key><string>EasyJob</string>
  <key>CFBundleIconFile</key><string>icon</string>
  <key>CFBundleVersion</key><string>$VERSION</string>
  <key>CFBundleShortVersionString</key><string>$VERSION</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>LSMinimumSystemVersion</key><string>11.0</string>
  <key>NSHighResolutionCapable</key><true/>
</dict>
</plist>
PLIST

# Ad-hoc signature, so Apple Silicon accepts the bundle (use your own identity instead for distribution outside your team).
if command -v codesign >/dev/null 2>&1; then
  codesign --force --deep --sign - "$APP" >/dev/null 2>&1 || echo "warning: codesign failed; the app may need to be allowed in System Settings" >&2
fi

echo "Created $APP"
