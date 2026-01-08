# Home Assistant Anywhere

Access your Home Assistant from anywhere without port forwarding or VPN.

## Setup

1. Go to [homeassistantanywhere.com](https://homeassistantanywhere.com) and create an account
2. Register your Home Assistant and choose a subdomain (e.g., `myhome`)
3. Copy your **Connection Key** from the dashboard
4. Paste the Connection Key in this addon's configuration
5. Start the addon

## Usage

After setup, access your Home Assistant at:

```
https://[your-subdomain].homeassistantanywhere.com
```

## How it works

This addon creates a secure outbound WebSocket connection to our server. No port forwarding needed - all connections are initiated from your Home Assistant.

## Support

- Website: https://homeassistantanywhere.com
- Issues: https://github.com/databot/home-assistant-anywhere/issues
