[Unit]
Description={{EXECUTABLE}} Daemon
Wants=network-online.target
After=network-online.target

[Service]
User=throttlex
ExecStart=/usr/local/share/{{SUBFOLDER}}/{{EXECUTABLE}}
WorkingDirectory=/usr/local/share/{{SUBFOLDER}}
Restart=no

[Install]
WantedBy=multi-user.target
