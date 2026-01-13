# Home Access Anywhere

Access your Home Assistant from anywhere without port forwarding or VPN.

## About

This add-on creates a secure outbound WebSocket connection to our server, allowing you to access your Home Assistant remotely via your personal subdomain (e.g., `myhome.homeaccessanywhere.com`).

**No port forwarding needed** - all connections are initiated from your Home Assistant.

## Installation

1. Go to [homeaccessanywhere.com](https://homeaccessanywhere.com) and create an account
2. Register your Home Assistant and choose a subdomain
3. Copy your **Connection Key** from the dashboard
4. Add this repository to Home Assistant:
   - Go to **Settings** > **Add-ons** > **Add-on Store**
   - Click the menu (top right) > **Repositories**
   - Add: `https://github.com/homeaccessanywhere/home-assistant-addon`
5. Install the **Home Access Anywhere** add-on
6. Enter your Connection Key in the add-on configuration
7. Start the add-on

## Usage

After setup, access your Home Assistant at:

```
https://[your-subdomain].homeaccessanywhere.com
```

## Requirements

- Home Assistant OS (64-bit: amd64 or aarch64)
- An account on homeaccessanywhere.com

## Support

- Website: [homeaccessanywhere.com](https://homeaccessanywhere.com)
- Issues: [GitHub Issues](https://github.com/homeaccessanywhere/home-assistant-addon/issues)

## License

MIT License
