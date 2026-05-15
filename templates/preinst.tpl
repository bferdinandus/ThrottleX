#!/bin/bash
set -e

# Check if the service is active, and stop it if it is
if systemctl is-active --quiet {{EXECUTABLE}}.service; then
    echo "Stopping the existing service {{EXECUTABLE}} before installation."
    systemctl stop {{EXECUTABLE}}.service
fi

exit 0
