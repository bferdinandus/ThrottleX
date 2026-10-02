#!/bin/bash
set -e

case "$1" in
    configure)
        echo "Configuring {{PACKAGE_NAME}}..."

        # Create the system group 'throttlex', forcing it if it already exists
        if ! getent group throttlex &>/dev/null; then
            echo "Creating system group 'throttlex'..."
        fi
        groupadd --force --system throttlex

        # Check if the user 'throttlex' exists, and create it if it does not
        if ! id -u throttlex &>/dev/null; then
            echo "Creating system user 'throttlex'..."
            useradd --system --comment "User for Throttlex service" \
                --shell "/bin/false" --gid throttlex throttlex
        fi

        echo "Setting file permissions and ownership for /usr/local/share/{{SUBFOLDER}}..."
        # Change ownership of the files to throttlex
        chown -R throttlex:throttlex /usr/local/share/{{SUBFOLDER}}

        # Set appropriate file permissions
        chmod +x /usr/local/share/{{SUBFOLDER}}/{{EXECUTABLE}}

        echo "Creating log directory /var/log/{{SUBFOLDER}}..."
        mkdir -p /var/log/{{SUBFOLDER}}
        chown -R throttlex:throttlex /var/log/{{SUBFOLDER}}

        # Reload systemd manager configuration
        echo "Reloading systemd daemon..."
        systemctl daemon-reload 2>/dev/null || true

        # Enable the service
        echo "Enabling service {{EXECUTABLE}}..."
        systemctl enable {{EXECUTABLE}}.service 2>/dev/null || true

        # Start or restart the service
        echo "Starting service {{EXECUTABLE}}..."
        systemctl restart {{EXECUTABLE}}.service 2>/dev/null || systemctl start {{EXECUTABLE}}.service 2>/dev/null || true
        ;;

    abort-upgrade|abort-remove|abort-deconfigure)
        echo "Aborting action ($1) for {{PACKAGE_NAME}}..."
        ;;

    *)
        echo "postinst called with unknown argument \`$1'" >&2
        exit 1
        ;;
esac

exit 0
