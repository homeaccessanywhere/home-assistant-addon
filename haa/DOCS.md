# Home Access Anywhere

Access your Home Assistant from anywhere without port forwarding or VPN.

## Setup

1. Go to [homeaccessanywhere.com](https://homeaccessanywhere.com) and create an account
2. **Verify your email address** by clicking the link in the confirmation email
3. Copy your **Connection Key** from the registration page or dashboard
4. Paste the Connection Key in this addon's configuration
5. Start the addon

**Note:** Your tunnel will only become active after you verify your email address.

## Recommended Settings

- **Start on boot:** Enable this so the addon starts automatically after a restart
- **Watchdog:** Enabled by default - automatically restarts the addon if it becomes unresponsive

## Usage

After setup, access your Home Assistant at:

```
https://[your-subdomain].homeaccessanywhere.com
```

## How it works

This addon creates a secure outbound WebSocket connection to our server. No port forwarding needed - all connections are initiated from your Home Assistant.

## Support

- Website: https://homeaccessanywhere.com
- Issues: https://github.com/homeaccessanywhere/home-assistant-addon/issues
