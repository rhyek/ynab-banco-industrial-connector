https://www.jetbrains.com/help/youtrack/standalone/run-docker-container-as-service.html

/etc/systemd/system/playwright-server.service:

```
[Unit]
Description=Playwright Server
After=docker.service
Requires=docker.service

[Service]
TimeoutStartSec=0
Restart=always
#ExecStartPre=-/usr/bin/docker exec %n stop
#ExecStartPre=-/usr/bin/docker rm %n
ExecStartPre=/usr/bin/docker pull rhyek/playwright-server:latest
ExecStart=/usr/bin/docker run --rm --name %n \
    #-v <path to data directory>:/opt/youtrack/data \
    #-v <path to conf directory>:/opt/youtrack/conf \
    #-v <path to logs directory>:/opt/youtrack/logs \
    #-v <path to backups directory>:/opt/youtrack/backups \
    -e PLAYWRIGHT_SERVER_TOKEN=************ \
    -e NGROK_AUTH_TOKEN=************ \
    -p 3800:3800 \
    rhyek/playwright-server:latest

[Install]
WantedBy=default.target
```
