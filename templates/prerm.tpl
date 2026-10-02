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
        ;;

    upgrade)
        if systemctl is-active --quiet {{EXECUTABLE}}.service 2>/dev/null; then
            echo "Stopping service {{EXECUTABLE}} before upgrade..."
            systemctl stop {{EXECUTABLE}}.service 2>/dev/null || true
        fi
        ;;

    failed-upgrade)
        ;;

    *)
        echo "prerm called with unknown argument \`$1'" >&2
        exit 1
        ;;
esac

exit 0
