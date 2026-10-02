#!/bin/bash
set -e

case "$1" in
    configure)
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

        # Reload systemd manager configuration
        systemctl daemon-reload 2>/dev/null || true

        echo "Enabling service..." | logger
        # Enable the service
        systemctl enable {{EXECUTABLE}}.service 2>/dev/null || true

        # Start or restart the service
        echo "Starting service..." | logger
        systemctl restart {{EXECUTABLE}}.service 2>/dev/null || systemctl start {{EXECUTABLE}}.service 2>/dev/null || true
        ;;

    abort-upgrade|abort-remove|abort-deconfigure)
        ;;

    *)
        echo "postinst called with unknown argument \`$1'" >&2
        exit 1
        ;;
esac

exit 0
