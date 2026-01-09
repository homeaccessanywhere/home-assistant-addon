#!/usr/bin/env bash
set -e

CONFIG_PATH=/data/options.json

# Read configuration
CONNECTION_KEY=$(jq -r '.connection_key' $CONFIG_PATH)
HOME_ASSISTANT_URL=$(jq -r '.home_assistant_url // "http://supervisor/core"' $CONFIG_PATH)

if [ -z "$CONNECTION_KEY" ] || [ "$CONNECTION_KEY" == "null" ]; then
    echo "============================================"
    echo "ERROR: Connection key not configured!"
    echo ""
    echo "1. Go to https://homeassistantanywhere.com"
    echo "2. Register and get your Connection Key"
    echo "3. Enter it in the addon configuration"
    echo "============================================"
    exit 1
fi

echo "Starting Home Assistant Anywhere..."
echo "Connection key: ${CONNECTION_KEY:0:8}..."

# Configure environment
export ServerUrl="wss://api.homeassistantanywhere.com"
export ConnectionKey="$CONNECTION_KEY"
export HomeAssistantUrl="$HOME_ASSISTANT_URL"

echo "Home Assistant URL: $HOME_ASSISTANT_URL"

# Run the addon
cd /app
exec dotnet HAA.Addon.dll
