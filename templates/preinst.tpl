#!/bin/bash
set -e

case "$1" in
    install|upgrade)
        # Check if the service is active, and stop it if it is
        if systemctl is-active --quiet {{EXECUTABLE}}.service 2>/dev/null; then
            echo "Stopping active service {{EXECUTABLE}} before package installation..."
            systemctl stop {{EXECUTABLE}}.service 2>/dev/null || true
        fi
        ;;

    abort-upgrade)
        echo "Aborting upgrade for {{EXECUTABLE}}..."
        ;;

    *)
        echo "preinst called with unknown argument \`$1'" >&2
        exit 1
        ;;
esac

exit 0
