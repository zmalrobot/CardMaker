#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-1.0.0}"
PUBLISH_DIR="${2:-publish/linux-x64}"
OUTPUT_DIR="${3:-dist}"

# Strip leading 'v' if present
VERSION="${VERSION#v}"

ARCH="amd64"
PKG_DIR="$(mktemp -d /tmp/cardmaker-deb.XXXXXX)"
trap 'rm -rf "${PKG_DIR}"' EXIT

echo "==> Building Debian package for CardMaker v${VERSION} (${ARCH})..."

mkdir -p "${PKG_DIR}/DEBIAN"
mkdir -p "${PKG_DIR}/usr/lib/cardmaker"
mkdir -p "${PKG_DIR}/usr/bin"
mkdir -p "${PKG_DIR}/usr/share/applications"
mkdir -p "${PKG_DIR}/usr/share/icons/hicolor/512x512/apps"
mkdir -p "${OUTPUT_DIR}"

# 1. Copy published binaries
cp -a "${PUBLISH_DIR}/." "${PKG_DIR}/usr/lib/cardmaker/"
chmod 755 "${PKG_DIR}/usr/lib/cardmaker/CardMaker"

# 2. Launcher wrapper in /usr/bin
cat << 'EOF' > "${PKG_DIR}/usr/bin/cardmaker"
#!/usr/bin/env sh
exec /usr/lib/cardmaker/CardMaker "$@"
EOF
chmod 755 "${PKG_DIR}/usr/bin/cardmaker"

# 3. Desktop Entry
if [ -f "src/CardMaker.Desktop/Resources/cardmaker.desktop" ]; then
    cp "src/CardMaker.Desktop/Resources/cardmaker.desktop" "${PKG_DIR}/usr/share/applications/cardmaker.desktop"
else
    cat << 'EOF' > "${PKG_DIR}/usr/share/applications/cardmaker.desktop"
[Desktop Entry]
Name=CardMaker
GenericName=TCG Card Maker
Comment=Professional TCG Card Generator and Renderer
Exec=cardmaker
Icon=cardmaker
Terminal=false
Type=Application
Categories=Graphics;2DGraphics;
StartupWMClass=CardMaker
StartupNotify=true
EOF
fi
chmod 644 "${PKG_DIR}/usr/share/applications/cardmaker.desktop"

# 4. Icon
if [ -f "src/CardMaker.Desktop/icon.png" ]; then
    cp "src/CardMaker.Desktop/icon.png" "${PKG_DIR}/usr/share/icons/hicolor/512x512/apps/cardmaker.png"
fi

# 5. Control File
cat << EOF > "${PKG_DIR}/DEBIAN/control"
Package: cardmaker
Version: ${VERSION}
Section: graphics
Priority: optional
Architecture: ${ARCH}
Depends: libwebkit2gtk-4.1-0, libfontconfig1, libfreetype6
Maintainer: CardMaker Team <admin@cardmaker.local>
Description: High-fidelity TCG card creation and rendering platform
 CardMaker is a professional data-driven platform for designing,
 composing and exporting high-resolution collectible card games.
EOF
chmod 644 "${PKG_DIR}/DEBIAN/control"

# 6. Build package
DEB_NAME="cardmaker_${VERSION}_amd64.deb"
dpkg-deb --build --root-owner-group "${PKG_DIR}" "${OUTPUT_DIR}/${DEB_NAME}"

echo "==> Successfully created ${OUTPUT_DIR}/${DEB_NAME}"

