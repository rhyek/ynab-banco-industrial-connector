#!/bin/bash
docker build --platform=linux/amd64 -t rhyek/playwright-server:latest .
docker push rhyek/playwright-server:latest
# run:
# docker run --rm -e PLAYWRIGHT_SERVER_TOKEN=asd -p 3777:3800 rhyek/playwright-server:latest
