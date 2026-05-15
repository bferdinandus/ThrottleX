#!/bin/bash
set -e

# Define variables
PACKAGE_NAME="throttle-x"
VERSION_FILE="version.txt"
TEMPLATES_DIR="templates"
ARCHITECTURE="arm64"  # Updated to arm64 for Raspberry Pi 4B
MAINTAINER="Ben Ferdinandus <2flyfish@gmail.com>"
DESCRIPTION="Run a wi-throttle service."
EXECUTABLE="ThrottleX.Core"
SUBFOLDER="throttle-x"  # Subfolder for organizing files

PKG_DIR="${PACKAGE_NAME}"
DEBIAN_DIR="${PKG_DIR}/DEBIAN"
SHARE_DIR="${PKG_DIR}/usr/local/share/${SUBFOLDER}"
SYSTEMD_DIR="${PKG_DIR}/etc/systemd/system"

# Ensure templates exist
for f in control.tpl preinst.tpl postinst.tpl service.tpl; do
  if [ ! -f "${TEMPLATES_DIR}/${f}" ]; then
    echo "Missing template: ${TEMPLATES_DIR}/${f}"
    exit 1
  fi
done

# Check if the version file exists; if not, create it
if [ ! -f "${VERSION_FILE}" ]; then
    echo "0.1" > "${VERSION_FILE}"
fi

# Read the current version from the file
VERSION=$(cat "${VERSION_FILE}")

# Increment the version (Assuming a simple semantic versioning scenario)
IFS='.' read -r major minor <<< "$VERSION"
minor=$((minor + 1))  # Increment the minor version
VERSION="${major}.${minor}"

# Write the new version back to the version file
echo "${VERSION}" > "${VERSION_FILE}"

echo "Building version: ${VERSION}" 

# Cleanup previous build directories and packages
rm -rf "${PKG_DIR}"  # Remove the existing package structure if it exists

# Create package structure
mkdir -p "${DEBIAN_DIR}"
mkdir -p "${SHARE_DIR}"  # Create subfolder
mkdir -p "${SYSTEMD_DIR}"

# Helper: copy template and replace placeholders (safe, portable)
render_template() {
  local tpl="$1"; local dest="$2"
  sed \
    -e "s|{{PACKAGE_NAME}}|${PACKAGE_NAME}|g" \
    -e "s|{{VERSION}}|${VERSION}|g" \
    -e "s|{{ARCHITECTURE}}|${ARCHITECTURE}|g" \
    -e "s|{{MAINTAINER}}|${MAINTAINER}|g" \
    -e "s|{{DESCRIPTION}}|${DESCRIPTION}|g" \
    -e "s|{{EXECUTABLE}}|${EXECUTABLE}|g" \
    -e "s|{{SUBFOLDER}}|${SUBFOLDER}|g" \
    "${TEMPLATES_DIR}/${tpl}" > "${dest}"
}

# Create control file
render_template "control.tpl" "${DEBIAN_DIR}/control"

# Create pre-installation script
render_template "preinst.tpl" "${DEBIAN_DIR}/preinst"
chmod 755 "${DEBIAN_DIR}/preinst"

# Create post-installation script
render_template "postinst.tpl" "${DEBIAN_DIR}/postinst"
chmod 755 "${DEBIAN_DIR}/postinst"

# Create service file with network dependencies and user directive
render_template "service.tpl" "${SYSTEMD_DIR}/${EXECUTABLE}.service"

# Publish the application targeting the specific project file
dotnet publish ./src/ThrottleX.Core/ThrottleX.Core.csproj -c Release -r linux-arm64 --self-contained -p:PublishSingleFile=true

# Copy the built files to the package structure inside the subfolder
cp -r ./src/ThrottleX.Core/bin/Release/net8.0/linux-arm64/publish/* "${SHARE_DIR}/"

# Build the .deb package with gzip compression
dpkg-deb -Zgzip --build "${PKG_DIR}"

mv ${PKG_DIR}.deb ${PKG_DIR}-${VERSION}.deb

echo "Package ${PKG_DIR}-${VERSION}.deb has been created."
