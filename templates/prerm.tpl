#!/bin/bash
set -e

case "$1" in
    remove|deconfigure)
        if systemctl is-active --quiet {{EXECUTABLE}}.service 2>/dev/null; then
            echo "Stopping service {{EXECUTABLE}}..."
            systemctl stop {{EXECUTABLE}}.service 2>/dev/null || true
        fi
        if systemctl is-enabled --quiet {{EXECUTABLE}}.service 2>/dev/null; then
            echo "Disabling service {{EXECUTABLE}}..."
            systemctl disable {{EXECUTABLE}}.service 2>/dev/null || true
        fi

        # Remove runtime-generated files (e.g. .aspnet data protection keys, logs) so dpkg can cleanly remove the directory
        if [ -d "/usr/local/share/{{SUBFOLDER}}/.aspnet" ]; then
            echo "Cleaning runtime data in /usr/local/share/{{SUBFOLDER}}..."
            rm -rf "/usr/local/share/{{SUBFOLDER}}/.aspnet"
        fi
        if [ -d "/usr/local/share/{{SUBFOLDER}}/Logs" ]; then
            echo "Cleaning leftover logs in /usr/local/share/{{SUBFOLDER}}..."
            rm -rf "/usr/local/share/{{SUBFOLDER}}/Logs"
        fi
        ;;

    upgrade)
        if systemctl is-active --quiet {{EXECUTABLE}}.service 2>/dev/null; then
            echo "Stopping service {{EXECUTABLE}} before upgrade..."
            systemctl stop {{EXECUTABLE}}.service 2>/dev/null || true
        fi
        ;;

    failed-upgrade)
        echo "Handling failed upgrade for {{EXECUTABLE}}..."
        ;;

    *)
        echo "prerm called with unknown argument \`$1'" >&2
        exit 1
        ;;
esac

exit 0
