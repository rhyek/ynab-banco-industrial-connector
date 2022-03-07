#!/bin/bash
cd $(dirname "$0")
set -e
pnpm exec tsc --build --verbose
adb push \
  ./build/main.js \
  /storage/self/primary/TaskerScripts/forward-banco-industrial-notifications.js
