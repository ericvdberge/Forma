#!/bin/sh
set -e

# There is no way to create a website yet, so a new database gets sample websites and pages.
# Set FORMA_SAMPLE_DATA=false to start empty.
export FORMA_SAMPLE_DATA="${FORMA_SAMPLE_DATA:-true}"

exec supervisord -c /etc/supervisor/forma.conf
