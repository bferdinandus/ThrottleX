#!/bin/bash
set -e

case "$1" in
    purge)
        echo "Purging {{PACKAGE_NAME}}..."

        # Reload systemd daemon
        echo "Reloading systemd daemon..."
        systemctl daemon-reload 2>/dev/null || true

        # Remove application logs
        if [ -d "/var/log/{{SUBFOLDER}}" ]; then
            echo "Removing application logs (/var/log/{{SUBFOLDER}})..."
            rm -rf /var/log/{{SUBFOLDER}}
        fi

        # Remove application directory if still present
        if [ -d "/usr/local/share/{{SUBFOLDER}}" ]; then
            echo "Removing application directory (/usr/local/share/{{SUBFOLDER}})..."
            rm -rf /usr/local/share/{{SUBFOLDER}}
        fi

        # Remove the system user and group on purge
        if id -u throttlex &>/dev/null; then
            echo "Removing system user 'throttlex'..."
            userdel throttlex 2>/dev/null || true
        fi
        if getent group throttlex &>/dev/null; then
            echo "Removing system group 'throttlex'..."
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
