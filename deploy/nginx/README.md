# Nginx Deployment Guide

## Prerequisites
- Ubuntu/Debian server
- Nginx installed
- Certbot (Let's Encrypt) installed
- .NET 10 runtime installed
- PostgreSQL 17 running

## Installation Steps

### 1. Install Nginx and Certbot
```bash
sudo apt update
sudo apt install nginx certbot python3-certbot-nginx
```

### 2. Copy Nginx Configuration
```bash
sudo cp deploy/nginx/tabflow.conf /etc/nginx/sites-available/tabflow
sudo ln -s /etc/nginx/sites-available/tabflow /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### 3. Obtain SSL Certificates
```bash
sudo certbot certonly --nginx -d platform.cafetech.uk -d tenant.cafetech.uk
```

### 4. Configure Application Ports
- Platform Host: `localhost:5000`
- Tenant Host: `localhost:5001`

### 5. Start Applications
```bash
# Platform Host
cd /opt/onlynet/src/apps/platform
dotnet run --urls "http://localhost:5000"

# Tenant Host
cd /opt/onlynet/src/apps/tenant
dotnet run --urls "http://localhost:5001"
```

### 6. Setup Systemd Services (Production)
Create `/etc/systemd/system/tabflow-platform.service`:
```ini
[Unit]
Description=TabFlow Platform Host
After=network.target

[Service]
WorkingDirectory=/opt/onlynet/src/apps/platform
ExecStart=/usr/bin/dotnet /opt/onlynet/src/apps/platform/bin/Release/net10.0/TabFlow.Platform.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

Create `/etc/systemd/system/tabflow-tenant.service`:
```ini
[Unit]
Description=TabFlow Tenant Host
After=network.target

[Service]
WorkingDirectory=/opt/onlynet/src/apps/tenant
ExecStart=/usr/bin/dotnet /opt/onlynet/src/apps/tenant/bin/Release/net10.0/TabFlow.Tenant.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

Enable and start:
```bash
sudo systemctl enable tabflow-platform
sudo systemctl enable tabflow-tenant
sudo systemctl start tabflow-platform
sudo systemctl start tabflow-tenant
```

## Firewall
```bash
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
```

## SSL Renewal
Certbot auto-renews certificates. Test renewal:
```bash
sudo certbot renew --dry-run
```
