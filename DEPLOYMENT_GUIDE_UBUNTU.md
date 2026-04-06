# 🚀 Deploying pearlxcore.dev to Ubuntu Server - Beginner's Guide

**Last Updated:** January 2026 | **Target:** Ubuntu 22.04 LTS or newer

---

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Step 0: Configure Cloudflare DNS](#step-0-configure-cloudflare-dns) ⭐ **START HERE**
3. [Step 1: Prepare Your Ubuntu Server](#step-1-prepare-your-ubuntu-server)
4. [Step 2: Install .NET Runtime](#step-2-install-net-runtime)
5. [Step 3: Install SQL Server](#step-3-install-sql-server)
6. [Step 4: Publish Your Application](#step-4-publish-your-application)
7. [Step 5: Transfer Files to Server](#step-5-transfer-files-to-server)
8. [Step 6: Create SystemD Service](#step-6-create-systemd-service)
9. [Step 7: Install Nginx (Reverse Proxy)](#step-7-install-nginx-reverse-proxy)
10. [Step 8: Configure SSL/HTTPS](#step-8-configure-ssluhttps)
11. [Step 9: Set Environment Variables](#step-9-set-environment-variables)
12. [Cloudflare SSL/TLS Settings](#cloudflare-ssltls-settings)
13. [Troubleshooting](#troubleshooting)

---

## Prerequisites

**What You Need:**
- Ubuntu 22.04 LTS server (or newer) with sudo access
- Domain name pointing to your server (for SSL) ✅ pearlxcore.dev via Cloudflare
- SSH access to your server
- SSH key or password for authentication
- Basic command-line knowledge

**Your Infrastructure:**
```
Internet (100.73.111.84)
    ↓ (Port Forward)
Proxmox Host
    ↓
Ubuntu VM (192.168.50.146)
    - .NET 10 Runtime
    - SQL Server 2022
    - Nginx (reverse proxy)
    - pearlxcore.dev Application
```

**On Your Local Machine:**
- Visual Studio Code or Visual Studio
- Git (optional but recommended)
- SSH client (Windows 10+ has built-in, or use PuTTY/Git Bash)

---

## Step 0: Configure Cloudflare DNS

**⭐ DO THIS FIRST** before deploying to your Ubuntu server!

### 0.0 Proxmox Port Forwarding Setup (Prerequisites)

Before adding DNS records, ensure port forwarding is configured properly.

**Your Setup:**
- **External IP (WAN):** 100.73.111.84
- **Ubuntu VM IP (LAN):** 192.168.50.146
- **Ports to forward:** 80 (HTTP), 443 (HTTPS)

### Port Forwarding Location - Two Scenarios:

#### Scenario A: Router → Proxmox → Ubuntu (Most Common)

```
Internet Traffic (100.73.111.84:80)
    ↓
Router (Port Forward)
    ↓
Proxmox Host (192.168.50.x)
    ↓
Ubuntu VM (192.168.50.146:80)
```

**What to configure:**
- In your **Router/Gateway:** Forward ports 80 and 443 to Proxmox host's IP
- In **Proxmox:** Forward traffic to Ubuntu VM IP (192.168.50.146)
- In **Ubuntu:** Firewall allows ports 80/443

#### Scenario B: Proxmox IS the Gateway (100.73.111.84 is Proxmox's IP)

```
Internet Traffic (100.73.111.84:80)
    ↓
Proxmox Host (Port Forward to VM)
    ↓
Ubuntu VM (192.168.50.146:80)
```

**What to configure:**
- In **Proxmox:** Forward ports 80 and 443 to Ubuntu VM (192.168.50.146)
- In **Ubuntu:** Firewall allows ports 80/443

### Identify Your Setup:

```bash
# On your Proxmox host, check which IP is the gateway/external
ip route show default
# If Proxmox has 100.73.111.84, it's Scenario B
# If Proxmox has a different IP, it's Scenario A (router is your gateway)
```

### Option 1: If You Have a Router/Firewall (Scenario A)

**In your Router/Firewall:**
1. Find Port Forwarding settings
2. Add forwarding rules:
   - External: `100.73.111.84:80` → Internal: `<Proxmox-IP>:80`
   - External: `100.73.111.84:443` → Internal: `<Proxmox-IP>:443`

**In Proxmox Host:**
Create iptables rules to forward traffic to Ubuntu VM:
```bash
# SSH to Proxmox host
sudo iptables -t nat -A PREROUTING -p tcp --dport 80 -j DNAT --to-destination 192.168.50.146:80
sudo iptables -t nat -A PREROUTING -p tcp --dport 443 -j DNAT --to-destination 192.168.50.146:443

# Enable IP forwarding
sudo sysctl -w net.ipv4.ip_forward=1

# Make persistent (add to /etc/sysctl.conf)
echo "net.ipv4.ip_forward=1" | sudo tee -a /etc/sysctl.conf
```

### Option 2: If Proxmox IS the Gateway (Scenario B)

**In Proxmox host directly:**
```bash
# SSH to Proxmox host
sudo iptables -t nat -A PREROUTING -p tcp --dport 80 -j DNAT --to-destination 192.168.50.146:80
sudo iptables -t nat -A PREROUTING -p tcp --dport 443 -j DNAT --to-destination 192.168.50.146:443

# Enable IP forwarding
sudo sysctl -w net.ipv4.ip_forward=1

# Make persistent
echo "net.ipv4.ip_forward=1" | sudo tee -a /etc/sysctl.conf
```

### Option 3: Using Proxmox Web Interface (Preferred)

If using Proxmox with vmbr0 bridge:
1. SSH to Proxmox host
2. Edit `/etc/pve/qemu-server/<vm-id>.conf`
3. Add network configuration to forward ports

Or use Proxmox firewall feature:
1. In Proxmox UI → Datacenter → Firewall → Rules
2. Add incoming rule for port 80 and 443
3. Set destination to Ubuntu VM IP

### Option 4: Using HAProxy or Nginx on Proxmox (Advanced)

Install reverse proxy on Proxmox host to forward traffic:
```bash
# Install Nginx on Proxmox host
apt install nginx

# Configure Nginx to forward to Ubuntu VM
# /etc/nginx/sites-available/pearlxcore-proxy

upstream ubuntu_backend {
    server 192.168.50.146;
}

server {
    listen 80;
    listen 443 ssl;
    server_name pearlxcore.dev www.pearlxcore.dev;
    
    location / {
        proxy_pass http://ubuntu_backend;
        proxy_set_header Host $host;
    }
}
```

### Verify Port Forwarding is Working:

```bash
# From Ubuntu VM, verify Nginx is listening
sudo netstat -tulpn | grep -E ':(80|443)'
# Should show nginx listening on 80 and 443

# Test locally from Ubuntu
curl http://localhost
# Should get response

# Test from another machine on your network (using internal IP)
curl http://192.168.50.146
# Should get response

# Test from external/internet (using external IP)
# Use an online port checker: https://www.yougetsignal.com/tools/open-ports/
# Check if 100.73.111.84:80 and :443 are open
```

### Troubleshoot Port Forwarding:

```bash
# Check if Proxmox is forwarding
sudo iptables -t nat -L -n
# Should show your PREROUTING rules

# Test connection from Ubuntu to external world
ping 8.8.8.8
# Should work

# If not working, check:
# 1. Is IP forwarding enabled? (net.ipv4.ip_forward=1)
# 2. Are iptables rules in place?
# 3. Is Ubuntu VM's firewall blocking?
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
```

### In Proxmox Host/Router:

### 0.1 Add A Records in Cloudflare

1. Log in to [Cloudflare Dashboard](https://dash.cloudflare.com)
2. Select your domain: **pearlxcore.dev**
3. Go to **DNS** → **Records**
4. Click **Add Record** and add two A records:

**Record 1:**
- Type: `A`
- Name: `@` (or `pearlxcore.dev`)
- IPv4 address: `100.73.111.84` (your external WAN IP)
- TTL: Auto
- Proxy status: **Gray cloud** ⚠️ (DNS only, see note below)

**Record 2:**
- Type: `A`
- Name: `www`
- IPv4 address: `100.73.111.84` (your external WAN IP)
- TTL: Auto
- Proxy status: **Gray cloud** ⚠️ (DNS only)

**⚠️ IMPORTANT: Use "Gray Cloud" (DNS Only) NOT "Orange Cloud"**
- **Gray Cloud (DNS only):** Traffic goes directly to your server (what we want)
- **Orange Cloud (Proxied):** Traffic goes through Cloudflare's proxy, which can cause issues with SSL certificate validation

### 0.2 Verify DNS Propagation

```bash
# On your local machine, check if DNS is propagated
nslookup pearlxcore.dev
# or
dig pearlxcore.dev

# Should return your external WAN IP address
# Example output: pearlxcore.dev has address 100.73.111.84
```

**DNS can take 24 hours to propagate globally, but usually 5-10 minutes.**

**Verify it's pointing to external IP, not internal:**
```bash
# You should see 100.73.111.84, NOT 192.168.50.146
nslookup pearlxcore.dev
# Expected: pearlxcore.dev has address 100.73.111.84 ✅
# NOT:      pearlxcore.dev has address 192.168.50.146 ❌
```

### 0.3 Disable Cloudflare's HTTP Rewrite

In Cloudflare Dashboard:
1. Go to **Rules** → **Normalization**
2. Make sure **URL Normalization** is OFF (to avoid conflicts with our Nginx redirects)

### 0.4 Set SSL/TLS Mode

In Cloudflare Dashboard:
1. Go to **SSL/TLS** → **Overview**
2. Set encryption mode to: **Full (strict)** ⭐ (or **Full** if you're using Let's Encrypt with a valid certificate)

**Why Full (strict)?**
- Encrypts traffic from Cloudflare to your server
- Requires valid SSL on your server (which we'll setup with Let's Encrypt)
- Prevents man-in-the-middle attacks

### 0.5 Wait for DNS to Propagate

Before moving to Step 1, verify:
```bash
# Keep testing until this returns your server IP
nslookup pearlxcore.dev

# Also test with curl (will fail, but shows DNS is resolving)
curl -v https://pearlxcore.dev
# Should try to connect to your server IP
```

---

## Step 1: Prepare Your Ubuntu Server

### 1.1 Connect to Your Server

```bash
# Connect to Ubuntu VM inside Proxmox (internal LAN)
ssh username@192.168.50.146
# Example: ssh ubuntu@192.168.50.146

# Or if you have SSH key
ssh -i /path/to/key ubuntu@192.168.50.146
```

**Note:** You're connecting to the internal IP `192.168.50.146` since you're on the same network. The external IP `100.73.111.84` is used only by Cloudflare/internet users.

### 1.2 Update System Packages
```bash
# Update package lists
sudo apt update

# Upgrade installed packages
sudo apt upgrade -y

# Install basic utilities
sudo apt install -y curl wget nano git
```

### 1.3 Create Application Directory
```bash
# Create directory for your app
sudo mkdir -p /var/www/pearlxcore-dev
sudo chown $USER:$USER /var/www/pearlxcore-dev

# Create directory for logs
sudo mkdir -p /var/log/pearlxcore-dev
sudo chown $USER:$USER /var/log/pearlxcore-dev
```

---

## Step 2: Install .NET Runtime

### 2.1 Add Microsoft Package Repository
```bash
# Download and run Microsoft setup script
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh

# Install .NET 10 Runtime
./dotnet-install.sh --version latest --runtime aspnetcore --install-dir /usr/share/dotnet

# Create symlink so 'dotnet' command works globally
sudo ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet
```

### 2.2 Verify Installation
```bash
# Check .NET version
dotnet --version

# Should output something like: 10.0.x
```

### 2.3 Alternative: Using APT Repository (Easier)
```bash
# Add Microsoft repository
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Update package lists
sudo apt update

# Install .NET 10 Runtime
sudo apt install -y dotnet-runtime-10.0

# Verify
dotnet --version
```

---

## Step 3: Install SQL Server

### Option A: SQL Server on Linux (Recommended for Production)

```bash
# Add Microsoft SQL Server repository
sudo add-apt-repository "$(wget -qO- https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list)"
sudo apt update

# Install SQL Server
sudo apt install -y mssql-server

# Run setup script
sudo /opt/mssql/bin/mssql-conf setup

# Follow prompts:
# - Accept license: Yes
# - Choose Edition: Developer (free for development)
# - Enter SA password (strong password like: P@ssw0rd123!ABC)

# Start SQL Server
sudo systemctl enable mssql-server
sudo systemctl start mssql-server

# Verify it's running
sudo systemctl status mssql-server
```

### Option B: Azure SQL Database (Cloud-based, Easier Maintenance)

If you prefer managed database:
1. Create free Azure account (200 USD credit)
2. Create Azure SQL Database
3. Copy connection string
4. Use in appsettings.json

**Pros:** Auto backups, auto updates, no server maintenance
**Cons:** Slight monthly cost after free tier

### 3.1 Install SQL Server Command-line Tools
```bash
# Add tools repository
sudo add-apt-repository "$(wget -qO- https://packages.microsoft.com/config/ubuntu/22.04/mssql-tools.list)"
sudo apt update

# Install tools
sudo apt install -y mssql-tools18

# Add to PATH
echo 'export PATH="$PATH:/opt/mssql-tools18/bin"' >> ~/.bashrc
source ~/.bashrc
```

### 3.2 Create Database
```bash
# Login to SQL Server
sqlcmd -S localhost -U sa -P "Your_SA_Password"

# At the command prompt (1>), run:
CREATE DATABASE pearlxcore_dev;
GO

# Verify database created
SELECT name FROM sys.databases;
GO

# Exit
EXIT
```

---

## Step 4: Publish Your Application

### 4.1 On Your Local Machine - Publish Release Build

```bash
# Open PowerShell/Terminal in your project directory
cd "c:\Users\User\source\repos\pearlxcore-dev\pearlxcore-dev"

# Clean previous builds
dotnet clean

# Restore dependencies
dotnet restore

# Publish as Release into an ignored artifact folder
dotnet publish -c Release -o "./.artifacts/publish"

# This creates an ignored '.artifacts/publish' folder with all files needed to run on server
```

### 4.2 Update Production Configuration

Open `.artifacts/publish/appsettings.Production.json` and keep it free of secrets. Put the SQL connection string in `/etc/pearlxcore/pearlxcore.env` as `ConnectionStrings__DefaultConnection=...`.

```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "/var/log/pearlxcore-dev/pearlxcore-.txt",
          "rollingInterval": "Day",
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] {Level:u3} {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

---

## Step 5: Transfer Files to Server

### 5.1 Using SCP (Secure Copy)

**From Windows PowerShell:**
```powershell
# Navigate to publish folder
cd "c:\Users\User\source\repos\pearlxcore-dev\pearlxcore-dev\.artifacts\publish"

# Copy entire publish folder to Ubuntu VM (internal IP)
scp -r . username@192.168.50.146:/var/www/pearlxcore-dev/

# Example:
# scp -r . ubuntu@192.168.50.146:/var/www/pearlxcore-dev/
```

**From Mac/Linux Terminal:**
```bash
# Same command works - use internal IP
scp -r ~/path/to/.artifacts/publish ubuntu@192.168.50.146:/var/www/pearlxcore-dev/
```

### 5.2 Alternative: Using Git

```bash
# SSH into server
ssh username@your-server-ip

# Clone repository
cd /var/www
git clone https://github.com/yourusername/pearlxcore-dev.git

# Navigate to project
cd pearlxcore-dev

# Publish on server
dotnet publish -c Release -o "./.artifacts/publish"
```

### 5.3 Verify Files Transferred
```bash
# SSH into server
ssh username@your-server-ip

# Check files
ls -la /var/www/pearlxcore-dev/

# Should see: releases/, shared/, and `publish` as a symlink to the active release.
```

---

## Step 6: Create SystemD Service

### 6.1 Create Service File

```bash
# SSH into server
ssh username@your-server-ip

# Create service file
sudo nano /etc/systemd/system/pearlxcore-dev.service
```

### 6.2 Paste This Content

```ini
[Unit]
Description=pearlxcore.dev ASP.NET Core Web Application
After=network.target

[Service]
Type=notify
User=www-data
WorkingDirectory=/var/www/pearlxcore-dev/publish
ExecStart=/usr/bin/dotnet /var/www/pearlxcore-dev/publish/pearlxcore-dev.dll
Restart=always
RestartSec=10
StandardOutput=journal
StandardError=journal
Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="ASPNETCORE_URLS=http://localhost:5000"

[Install]
WantedBy=multi-user.target
```

### 6.3 Save and Enable Service

```bash
# Save file: Ctrl+O, Enter, Ctrl+X

# Set permissions
sudo chmod 644 /etc/systemd/system/pearlxcore-dev.service

# Reload systemd
sudo systemctl daemon-reload

# Enable service (start on boot)
sudo systemctl enable pearlxcore-dev

# Start the service
sudo systemctl start pearlxcore-dev

# Check status
sudo systemctl status pearlxcore-dev

# Should show: Active (running)
```

### 6.4 Useful Service Commands

```bash
# View service logs
sudo journalctl -u pearlxcore-dev -f

# Restart service
sudo systemctl restart pearlxcore-dev

# Stop service
sudo systemctl stop pearlxcore-dev

# View last 50 lines of logs
sudo journalctl -u pearlxcore-dev -n 50
```

---

## Step 7: Install Nginx (Reverse Proxy)

Nginx acts as a reverse proxy, forwarding web requests to your .NET application.

### 7.1 Install Nginx

```bash
# Install Nginx
sudo apt install -y nginx

# Enable and start
sudo systemctl enable nginx
sudo systemctl start nginx

# Verify
sudo systemctl status nginx
```

### 7.2 Configure Nginx

```bash
# Edit nginx config
sudo nano /etc/nginx/sites-available/pearlxcore-dev
```

### 7.3 Paste This Configuration

```nginx
upstream pearlxcore_backend {
    server localhost:5000;
}

server {
    listen 80;
    server_name pearlxcore.dev www.pearlxcore.dev;

    # Redirect HTTP to HTTPS (we'll set this up next)
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name pearlxcore.dev www.pearlxcore.dev;

    # SSL certificates (we'll create these with Certbot)
    ssl_certificate /etc/letsencrypt/live/pearlxcore.dev/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/pearlxcore.dev/privkey.pem;

    # SSL configuration
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Frame-Options "DENY" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;

    # Logging
    access_log /var/log/nginx/pearlxcore-dev-access.log;
    error_log /var/log/nginx/pearlxcore-dev-error.log;

    # Proxy settings
    location / {
        proxy_pass http://pearlxcore_backend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        proxy_redirect off;
    }

    # Static files (cache longer)
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

### 7.4 Enable Configuration

```bash
# Create symlink to enable site
sudo ln -s /etc/nginx/sites-available/pearlxcore-dev /etc/nginx/sites-enabled/

# Test nginx config
sudo nginx -t

# Should show: successful

# Reload nginx
sudo systemctl reload nginx
```

---

## Step 8: Configure SSL/HTTPS

### 8.1 Install Certbot (Let's Encrypt)

```bash
# Install Certbot
sudo apt install -y certbot python3-certbot-nginx

# Get SSL certificate
sudo certbot certonly --nginx -d pearlxcore.dev -d www.pearlxcore.dev

# Follow prompts:
# - Enter email address
# - Accept terms
# - Share email with EFF (optional)

# Certbot will place certificates in /etc/letsencrypt/live/pearlxcore.dev/
```

### 8.2 Auto-Renewal

```bash
# Enable certbot auto-renewal service
sudo systemctl enable certbot.timer

# Test renewal (dry-run)
sudo certbot renew --dry-run

# Certbot will automatically renew 30 days before expiry
```

### 8.3 Update Nginx Config

The certificates are now ready. Test Nginx:

```bash
# Test configuration
sudo nginx -t

# Reload if successful
sudo systemctl reload nginx
```

---

## Step 9: Set Environment Variables

The app reads production secrets from `/etc/pearlxcore/pearlxcore.env` through the systemd `EnvironmentFile=` setting.

That file is not part of the Git repo. It lives only on the server.

### 9.1 Create the Environment File

```bash
# Create the directory if it does not exist
sudo mkdir -p /etc/pearlxcore

# Create or edit the environment file
sudo nano /etc/pearlxcore/pearlxcore.env
```

Paste values like this into the file:

```bash
ConnectionStrings__DefaultConnection=Server=127.0.0.1,1433;Database=pearlxcoreDevDb;User Id=sa;Password=CHANGE_ME;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30
AdminUser__Email=admin@pearlxcore.dev
AdminUser__Password=CHANGE_ME
```

You can use [the example file in the repo](./pearlxcore.env.example) as a template.

### 9.2 Reload and Verify

```bash
# Reload systemd so it reads the env file
sudo systemctl daemon-reload

# Restart the application
sudo systemctl restart pearlxcore

# Check if application is listening on port 5000
sudo netstat -tulpn | grep 5000

# Should show: dotnet process listening on :5000

# Or check logs
sudo journalctl -u pearlxcore -f
```

---

## Monitoring & Maintenance

### Check Application Health
```bash
# View real-time logs
sudo journalctl -u pearlxcore-dev -f

# View application logs
tail -f /var/log/pearlxcore-dev/pearlxcore-*.txt

# View Nginx logs
tail -f /var/log/nginx/pearlxcore-dev-error.log
```

### Backup Database
```bash
# Daily backup script
#!/bin/bash
BACKUP_DIR="/var/backups/sql"
mkdir -p $BACKUP_DIR
DATE=$(date +%Y-%m-%d_%H-%M-%S)

sqlcmd -S localhost -U sa -Q "BACKUP DATABASE pearlxcore_dev TO DISK = '/var/backups/sql/pearlxcore_dev_$DATE.bak'"

# Schedule with cron (daily at 2 AM)
sudo crontab -e
# Add: 0 2 * * * /path/to/backup-script.sh
```

### Monitor Disk Space
```bash
# Check disk usage
df -h

# Check specific directory
du -sh /var/www/pearlxcore-dev
du -sh /var/log/pearlxcore-dev
du -sh /var/backups
```

---

## Cloudflare SSL/TLS Settings

Once your Let's Encrypt certificate is installed on your Ubuntu server, configure these Cloudflare settings:

### ✅ SSL/TLS Configuration in Cloudflare

**Recommended Settings:**
- **Encryption Mode:** Full (strict)
- **Certificate:** Use your Let's Encrypt certificate (not Cloudflare's)
- **HTTP Rewrite:** OFF
- **Always Use HTTPS:** ON
- **Minimum TLS Version:** 1.2

### In Cloudflare Dashboard:

1. **SSL/TLS → Overview**
   - Set to: **Full (strict)**
   
2. **SSL/TLS → Edge Certificates**
   - Leave "Universal SSL" enabled (Cloudflare's free SSL for their edge)
   - This doesn't conflict with your server's Let's Encrypt cert

3. **SSL/TLS → Origin Server**
   - Optional: Upload your server's certificate here for extra security
   - But not required since we're using "Full (strict)"

4. **Rules → Page Rules** (if you have)
   - Disable any page rules that might interfere with SSL
   - Remove any "Always Use HTTPS" rules that might cause loops

### How It Works

```
User Request
     ↓
Cloudflare Edge (Orange Cloud) - Uses Cloudflare's SSL
     ↓
Your Ubuntu Server - Uses Let's Encrypt SSL
     ↓
Nginx → .NET Application
```

Both layers have SSL encryption for maximum security.

### Common Cloudflare + Let's Encrypt Issues & Fixes

**Issue 1: "Too many redirects" error**
```
Fix: 
1. Cloudflare Dashboard → SSL/TLS → set to "Full (strict)"
2. Remove any "Always Use HTTPS" Page Rules
3. Check Nginx config - it shouldn't redirect to HTTPS if traffic from Cloudflare already has scheme https
```

**Issue 2: Certificate validation fails**
```
Fix:
1. Ensure Cloudflare DNS is "Gray Cloud" (DNS only) while getting certificate
2. Or temporarily use "HTTP Only" SSL/TLS mode in Cloudflare while running certbot
3. Then switch back to "Full (strict)" after certificate is installed
```

**Issue 3: Mixed content warnings (HTTP + HTTPS)**
```
Fix:
1. Set Cloudflare to "Always Use HTTPS" enabled
2. Set Nginx redirect: return 301 https://$server_name$request_uri;
3. Test: curl -I https://pearlxcore.dev (should be 200, not redirect)
```

---

## Monitoring Cloudflare Performance

### Check Cache Status
```bash
# Get response headers to see Cloudflare cache status
curl -I https://pearlxcore.dev

# Should see header: CF-Cache-Status: HIT or MISS
# HIT = cached by Cloudflare
# MISS = came from your server
```

### View Cloudflare Analytics
In Cloudflare Dashboard:
1. Go to **Analytics & Logs** → **Analytics**
2. View:
   - Requests per day
   - Cache hit rate
   - SSL/TLS handshake stats
   - Top paths
   - Threats blocked

### Disable Cloudflare Caching for Admin Panel

In Cloudflare Dashboard:
1. Go to **Rules** → **Page Rules**
2. Add new rule: `pearlxcore.dev/admin*`
   - Set Cache Level: **Bypass**
   - This prevents admin panel from being cached

```
Pattern: pearlxcore.dev/admin*
Setting: Cache Level
Value: Bypass Cache
```

---

## Rollback: If Cloudflare DNS Breaks

If you have DNS issues and need to rollback:

```bash
# Temporarily point domain to old nameservers
# In Cloudflare: 
# Change DNS Records to point to old provider
# Or change A records to old server IP
```

This gives you flexibility to fix the server without losing domain access.

---

## Proxmox-Specific Troubleshooting

### Port Forwarding Verification

```bash
# From Ubuntu VM, verify it can reach external network
ping 8.8.8.8
# Should get responses

# Check if ports are listening
sudo netstat -tulpn | grep -E ':(80|443)'
# Should show:
# tcp  0  0 0.0.0.0:80   0.0.0.0:*  LISTEN
# tcp  0  0 0.0.0.0:443  0.0.0.0:*  LISTEN
```

### Ubuntu Network Configuration

If Ubuntu can't access the network:

```bash
# Check network interface status
ifconfig
# Should show eth0 with IP 192.168.50.146

# Restart networking if needed
sudo systemctl restart networking

# Or for netplan:
sudo netplan apply
```

### External Access Doesn't Work

```bash
# Test from Ubuntu if ports are open to external
# (You need another computer/phone on different network)

# First verify using internal IP works
curl http://192.168.50.146
# Should work if Nginx is running

# Test using external IP (from other network)
curl http://100.73.111.84
# If this fails but internal works, port forwarding issue

# Check Proxmox port forwarding configuration
# In Proxmox interface, verify rules:
# 100.73.111.84:80 → 192.168.50.146:80
# 100.73.111.84:443 → 192.168.50.146:443
```

### DNS Works but Site Still Doesn't Load

```bash
# Verify you're actually reaching Nginx
curl -v https://pearlxcore.dev

# If connection times out:
# 1. Check Nginx is running: sudo systemctl status nginx
# 2. Check ports: sudo netstat -tulpn | grep nginx
# 3. Check firewall: sudo ufw status
```

---

## Troubleshooting

### Application Won't Start

```bash
# Check service status
sudo systemctl status pearlxcore-dev

# View error logs
sudo journalctl -u pearlxcore-dev -n 50 -p err

# Check database connection
sqlcmd -S localhost -U sa -P "Your_Password" -Q "SELECT COUNT(*) FROM sys.databases"
```

**Common Issues:**
- **Port 5000 already in use:** Change port in service file (ASPNETCORE_URLS)
- **Database connection failed:** Verify SQL Server is running and password is correct
- **Permission denied:** Check /var/www/pearlxcore-dev ownership

### Database Connection Error

```bash
# Verify SQL Server is running
sudo systemctl status mssql-server

# Test connection
sqlcmd -S localhost -U sa -P "Your_Password"

# If fails, check logs
sudo tail -f /var/opt/mssql/log/errorlog
```

### Nginx Not Forwarding Traffic

```bash
# Test Nginx config
sudo nginx -t

# Check Nginx logs
tail -f /var/log/nginx/pearlxcore-dev-error.log

# Verify backend is running
curl http://localhost:5000

# Should return HTML content
```

### SSL Certificate Issues

```bash
# Check certificate expiry
sudo certbot certificates

# Renew manually
sudo certbot renew

# Check Nginx SSL config
sudo openssl s_client -connect pearlxcore.dev:443 -tls1_2
```

### Application Crashes Repeatedly

```bash
# Check logs for error
sudo journalctl -u pearlxcore-dev -n 100

# Restart in foreground to see errors
sudo -u www-data /usr/bin/dotnet /var/www/pearlxcore-dev/publish/pearlxcore-dev.dll

# Check appsettings.Production.json for valid JSON
cat /var/www/pearlxcore-dev/publish/appsettings.Production.json | json_pp
```

---

## Security Checklist

- [ ] SSH key authentication enabled (disable password auth)
- [ ] Firewall configured (ufw)
- [ ] SQL Server password is strong (16+ chars, mixed case, numbers, symbols)
- [ ] Admin password set via environment variable
- [ ] SSL/HTTPS enabled and auto-renewal working
- [ ] Regular backups scheduled
- [ ] Security headers configured in Nginx
- [ ] Log rotation configured
- [ ] Fail2Ban installed for brute-force protection

```bash
# Quick firewall setup
sudo ufw enable
sudo ufw allow 22/tcp    # SSH
sudo ufw allow 80/tcp    # HTTP
sudo ufw allow 443/tcp   # HTTPS
sudo ufw default deny incoming
sudo ufw default allow outgoing
```

---

## Updating Your Application

When you publish a new version:

```bash
# On local machine
dotnet publish -c Release -o "./.artifacts/publish"

# Transfer files
scp -r ./.artifacts/publish/* username@your-server-ip:/var/www/pearlxcore-dev/releases/<timestamp>/

# SSH to server
ssh username@your-server-ip

# Update the live symlink
ln -sfnT /var/www/pearlxcore-dev/releases/<timestamp> /var/www/pearlxcore-dev/publish

# Restart application
sudo systemctl restart pearlxcore-dev

# Verify
sudo systemctl status pearlxcore-dev
```

---

## Performance Optimization

### Enable Compression
```bash
# Add to Nginx location block
gzip on;
gzip_vary on;
gzip_proxied any;
gzip_comp_level 6;
gzip_types text/plain text/css text/xml text/javascript application/json application/javascript application/xml+rss application/atom+xml image/svg+xml;
```

### Connection Pooling
Set the production connection string in `/etc/pearlxcore/pearlxcore.env` instead of `appsettings.Production.json`:
```bash
ConnectionStrings__DefaultConnection=Server=localhost;Database=pearlxcore_dev;User Id=sa;Password=Your_Password;Min Pool Size=5;Max Pool Size=20;TrustServerCertificate=true;
```

---

## Support & Resources

- **Logs Location:** 
  - Application: `/var/log/pearlxcore-dev/`
  - Nginx: `/var/log/nginx/`
  - System: `sudo journalctl -u pearlxcore-dev -f`

- **Useful Commands:**
  - Start app: `sudo systemctl start pearlxcore-dev`
  - Stop app: `sudo systemctl stop pearlxcore-dev`
  - Restart app: `sudo systemctl restart pearlxcore-dev`
  - Reload Nginx: `sudo systemctl reload nginx`

- **Quick Database Access:**
  ```bash
  sqlcmd -S localhost -U sa -P "Your_Password" -d pearlxcore_dev
  ```

---

## Checklist: Ready for Deployment?

- [ ] Application published in Release mode
- [ ] `/etc/pearlxcore/pearlxcore.env` updated with SQL Server connection
- [ ] Ubuntu server prepared with .NET and SQL Server
- [ ] Application files transferred to `/var/www/pearlxcore-dev/releases/<timestamp>/`
- [ ] SystemD service created and running
- [ ] Nginx configured as reverse proxy
- [ ] SSL certificate obtained and configured
- [ ] Domain DNS pointing to server
- [ ] Environment variables set (admin password)
- [ ] Logs configured and accessible
- [ ] Backups scheduled

Once all checked, your app is live! 🎉

---

**Questions?** Check the Troubleshooting section or review your service/Nginx logs.

**Good luck with your deployment!** 🚀
