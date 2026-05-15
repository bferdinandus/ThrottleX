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
chown -R throttlex:throttlex /usr/local/share/{{SUBFOLDER}}

# Set appropriate file permissions
chmod +x /usr/local/share/{{SUBFOLDER}}/{{EXECUTABLE}}

echo "Enabling service..." | logger
# Enable the service
systemctl enable {{EXECUTABLE}}.service

# Start the service
echo "Starting service..." | logger
systemctl start {{EXECUTABLE}}.service

exit 0
