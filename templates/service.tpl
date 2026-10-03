[Unit]
Description={{EXECUTABLE}} Daemon
Wants=network-online.target
After=network-online.target

[Service]
User=throttlex
AmbientCapabilities=CAP_SYS_TIME
CapabilityBoundingSet=CAP_SYS_TIME
ExecStart=/usr/local/share/{{SUBFOLDER}}/{{EXECUTABLE}}
WorkingDirectory=/usr/local/share/{{SUBFOLDER}}
Environment=DOTNET_ENVIRONMENT=Production
Restart=no

[Install]
WantedBy=multi-user.target
