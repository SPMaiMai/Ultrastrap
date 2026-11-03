#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
cd "$ROOT"

APP_NAME="Ultrastrap"
DIST="$ROOT/dist"
APP_DIR="$DIST/${APP_NAME}.app"
CONTENTS="$APP_DIR/Contents"
MACOS_DIR="$CONTENTS/MacOS"
RESOURCES_DIR="$CONTENTS/Resources"
ICONSET="$DIST/${APP_NAME}.iconset"
PNG="$ROOT/Sources/Ultrastrap/Resources/Ultrastrap.png"

if ! command -v swift >/dev/null 2>&1; then
  echo "Swift is not installed. Install Xcode from the App Store, then run: xcode-select --install"
  exit 1
fi

if [[ ! -f "$PNG" ]]; then
  echo "Missing icon: $PNG"
  exit 1
fi

echo "==> Building Ultrastrap (release)"
swift build -c release --product Ultrastrap

BIN="$(swift build -c release --show-bin-path)/Ultrastrap"
if [[ ! -x "$BIN" ]]; then
  echo "Build succeeded but the Ultrastrap binary was not found at $BIN"
  exit 1
fi

echo "==> Creating ${APP_NAME}.app"
rm -rf "$APP_DIR"
mkdir -p "$MACOS_DIR" "$RESOURCES_DIR"
cp "$BIN" "$MACOS_DIR/$APP_NAME"
chmod +x "$MACOS_DIR/$APP_NAME"
cp "$ROOT/Info.plist" "$CONTENTS/Info.plist"
cp "$PNG" "$RESOURCES_DIR/Ultrastrap.png"

if command -v sips >/dev/null 2>&1 && command -v iconutil >/dev/null 2>&1; then
  echo "==> Building Ultrastrap.icns"
  rm -rf "$ICONSET"
  mkdir -p "$ICONSET"
  for size in 16 32 64 128 256 512 1024; do
    sips -z "$size" "$size" "$PNG" --out "$ICONSET/icon_${size}x${size}.png" >/dev/null
  done
  sips -z 32 32 "$PNG" --out "$ICONSET/icon_16x16@2x.png" >/dev/null
  sips -z 64 64 "$PNG" --out "$ICONSET/icon_32x32@2x.png" >/dev/null
  sips -z 256 256 "$PNG" --out "$ICONSET/icon_128x128@2x.png" >/dev/null
  sips -z 512 512 "$PNG" --out "$ICONSET/icon_256x256@2x.png" >/dev/null
  sips -z 1024 1024 "$PNG" --out "$ICONSET/icon_512x512@2x.png" >/dev/null
  iconutil -c icns "$ICONSET" -o "$RESOURCES_DIR/Ultrastrap.icns"
  rm -rf "$ICONSET"
else
  echo "sips/iconutil not found; the app will use the PNG icon at runtime."
fi

cat > "$CONTENTS/PkgInfo" <<'EOF'
APPL????
EOF

echo
echo "Built: $APP_DIR"
echo "Open it with:"
echo "  open \"$APP_DIR\""
echo
echo "If macOS says the app is from an unidentified developer:"
echo "  Right-click Ultrastrap.app → Open → Open"
echo "or:"
echo "  xattr -dr com.apple.quarantine \"$APP_DIR\""
