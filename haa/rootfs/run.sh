#!/usr/bin/env bash
set -e

CONFIG_PATH=/data/options.json
HA_CONFIG_PATH=/homeassistant/configuration.yaml

# Read configuration
CONNECTION_KEY=$(jq -r '.connection_key' $CONFIG_PATH)
HOME_ASSISTANT_URL=$(jq -r '.home_assistant_url // ""' $CONFIG_PATH)

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

# Auto-detect Home Assistant URL if not configured
if [ -z "$HOME_ASSISTANT_URL" ]; then
    echo "Auto-detecting Home Assistant URL..."

    # Default values
    HA_PORT=8123
    HA_SSL=false

    # Try to read from Home Assistant configuration
    if [ -f "$HA_CONFIG_PATH" ]; then
        # Read port from configuration (default 8123)
        CONFIGURED_PORT=$(yq '.http.server_port // ""' "$HA_CONFIG_PATH" 2>/dev/null)
        if [ -n "$CONFIGURED_PORT" ] && [ "$CONFIGURED_PORT" != "null" ]; then
            HA_PORT=$CONFIGURED_PORT
        fi

        # Check if SSL is configured
        HAS_SSL_CERT=$(yq '.http.ssl_certificate // ""' "$HA_CONFIG_PATH" 2>/dev/null)
        HAS_SSL_KEY=$(yq '.http.ssl_key // ""' "$HA_CONFIG_PATH" 2>/dev/null)
        if [ -n "$HAS_SSL_CERT" ] && [ "$HAS_SSL_CERT" != "null" ] && \
           [ -n "$HAS_SSL_KEY" ] && [ "$HAS_SSL_KEY" != "null" ]; then
            HA_SSL=true
        fi
    else
        echo "Warning: Could not read Home Assistant configuration, using defaults"
    fi

    # Build URL
    if [ "$HA_SSL" = "true" ]; then
        HOME_ASSISTANT_URL="https://homeassistant:${HA_PORT}"
    else
        HOME_ASSISTANT_URL="http://homeassistant:${HA_PORT}"
    fi

    echo "Detected: port=${HA_PORT}, ssl=${HA_SSL}"
fi

echo "Starting Home Assistant Anywhere..."
echo "Connection key: ${CONNECTION_KEY:0:8}..."
echo "Home Assistant URL: $HOME_ASSISTANT_URL"

# Configure environment
export ServerUrl="wss://api.homeassistantanywhere.com"
export ConnectionKey="$CONNECTION_KEY"
export HomeAssistantUrl="$HOME_ASSISTANT_URL"

# Run the addon
cd /app
exec dotnet HAA.Addon.dll
