#!/bin/bash

# Define variables
PACKAGE_NAME="throttle-x"
VERSION="0.1"
ARCHITECTURE="arm64"  # Updated to arm64 for Raspberry Pi 4B
MAINTAINER="Ben Ferdinandus <2flyfish@gmail.com>"
DESCRIPTION="Run a wi-throttle service."
EXECUTABLE="ThrottleX.Core"
SUBFOLDER="throttle-x"  # Subfolder for organizing files

# Cleanup previous build directories and packages
rm -rf "${PACKAGE_NAME}"  # Remove the existing package structure if it exists
rm -f "${PACKAGE_NAME}.deb"  # Remove the existing .deb package if it exists

# Create package structure
mkdir -p "${PACKAGE_NAME}/DEBIAN"
mkdir -p "${PACKAGE_NAME}/usr/local/share/${SUBFOLDER}"  # Create subfolder
mkdir -p "${PACKAGE_NAME}/etc/systemd/system"

# Create control file
cat <<EOF > "${PACKAGE_NAME}/DEBIAN/control"
Package: ${PACKAGE_NAME}
Version: ${VERSION}
Architecture: ${ARCHITECTURE}
Maintainer: ${MAINTAINER}
Depends: systemd
Section: utils
Priority: optional
Description: ${DESCRIPTION}
EOF

# Create pre-installation script
cat <<EOF > "${PACKAGE_NAME}/DEBIAN/preinst"
#!/bin/bash
set -e

# Check if the service is active, and stop it if it is
if systemctl is-active --quiet ${EXECUTABLE}.service; then
    echo "Stopping the existing service ${EXECUTABLE} before installation."
    systemctl stop ${EXECUTABLE}.service
fi

exit 0
EOF

chmod 755 "${PACKAGE_NAME}/DEBIAN/preinst"

# Create post-installation script
cat <<EOF > "${PACKAGE_NAME}/DEBIAN/postinst"
#!/bin/bash
set -e

# Create the system group 'throttlex', forcing it if it already exists
groupadd --force --system throttlex

# Check if the user 'throttlex' exists, and create it if it does not
if ! id -u throttlex &>/dev/null; then
    useradd --system --comment "User for Throttlex service" \
        --shell "/bin/false" --gid throttlex throttlex
fi
        
# Change ownership of the files to throttlex
chown -R throttlex:throttlex /usr/local/share/${SUBFOLDER}

# Set appropriate file permissions
chmod +x /usr/local/share/${SUBFOLDER}/${EXECUTABLE}

echo "Enabling service..." | logger
# Enable the service
systemctl enable ${EXECUTABLE}.service

# Start the service
echo "Starting service..." | logger
systemctl start ${EXECUTABLE}.service

exit 0
EOF

chmod 755 "${PACKAGE_NAME}/DEBIAN/postinst"

# Create service file with network dependencies and user directive
cat <<EOF > "${PACKAGE_NAME}/etc/systemd/system/${EXECUTABLE}.service"
[Unit]
Description=${EXECUTABLE} Daemon
Wants=network-online.target
After=network-online.target

[Service]
User=throttlex
ExecStart=/usr/local/share/${SUBFOLDER}/${EXECUTABLE}
WorkingDirectory=/usr/local/share/${SUBFOLDER}
Restart=no

[Install]
WantedBy=multi-user.target
EOF

# Publish the application targeting the specific project file
dotnet publish ./src/ThrottleX.Core/ThrottleX.Core.csproj -c Release -r linux-arm64 --self-contained -p:PublishSingleFile=true

# Copy the built files to the package structure inside the subfolder
cp -r ./src/ThrottleX.Core/bin/Release/net8.0/linux-arm64/publish/* "${PACKAGE_NAME}/usr/local/share/${SUBFOLDER}/"

# Build the .deb package with gzip compression
dpkg-deb -Zgzip --build "${PACKAGE_NAME}"

echo "Package ${PACKAGE_NAME}.deb has been created."
