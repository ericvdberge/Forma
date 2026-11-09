#!/bin/sh
set -e

# Credentials shared by the storage gateway and the API. The gateway only listens inside the
# container, so a random secret per start is enough; set STORAGE_SECRET_KEY to pin one.
export STORAGE_ACCESS_KEY="${STORAGE_ACCESS_KEY:-forma}"
export STORAGE_SECRET_KEY="${STORAGE_SECRET_KEY:-$(head -c 32 /dev/urandom | base64 | tr -d '/+=')}"

exec supervisord -c /etc/supervisor/forma.conf
