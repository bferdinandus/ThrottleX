#!/bin/bash
set -e

case "$1" in
    purge)
        # Reload systemd daemon
        systemctl daemon-reload 2>/dev/null || true

        # Remove runtime generated data, logs, and directory
        rm -rf /usr/local/share/{{SUBFOLDER}}

        # Remove the system user and group on purge
        if id -u throttlex &>/dev/null; then
            userdel throttlex 2>/dev/null || true
        fi
        if getent group throttlex &>/dev/null; then
            groupdel throttlex 2>/dev/null || true
        fi
        ;;

    remove|upgrade|failed-upgrade|abort-install|abort-upgrade|disappear)
        # Reload systemd daemon
        systemctl daemon-reload 2>/dev/null || true
        ;;

    *)
        echo "postrm called with unknown argument \`$1'" >&2
        exit 1
        ;;
esac

exit 0
