# User Secrets Setup Guide

This guide explains how to configure admin credentials using .NET User Secrets instead of hardcoding them.

## Overview

User Secrets are stored locally on your machine and are **NOT committed to version control**, making them perfect for sensitive data like passwords.

- **Development**: Use `dotnet user-secrets` to store credentials locally
- **Production**: Use environment variables

## Setup Instructions

### 1. Initialize User Secrets (One-Time Setup)

Run this command in the project root directory to create a user-secrets file:

```powershell
dotnet user-secrets init
```

This creates a `secrets.json` file in:
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\<project-guid>\secrets.json`
- **Mac**: `~/.microsoft/usersecrets/<project-guid>/secrets.json`
- **Linux**: `~/.microsoft/usersecrets/<project-guid>/secrets.json`

### 2. Set Admin Email (User Secret)

```powershell
dotnet user-secrets set "AdminUser:Email" "admin@yourdomain.com"
```

Example:
```powershell
dotnet user-secrets set "AdminUser:Email" "admin@pearlxcore.dev"
```

### 3. Set Admin Password (User Secret)

```powershell
dotnet user-secrets set "AdminUser:Password" "YourSecurePassword123!"
```

**Password Requirements:**
- Minimum 8 characters
- At least one digit (0-9)
- At least one lowercase letter (a-z)
- Uppercase and special characters are optional

Example:
```powershell
dotnet user-secrets set "AdminUser:Password" "MySecurePass123"
```

### 4. Set the Local Database Connection String

If you want to run the app locally, store your development connection string as a user secret too:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=LighthouseDb;Trusted_Connection=True;TrustServerCertificate=True"
```

If your local SQL Server uses SQL authentication instead of Windows auth, use that connection string here instead.

### 5. Verify Secrets Are Set

List all stored secrets:

```powershell
dotnet user-secrets list
```

Example output:
```
AdminUser:Email = admin@pearlxcore.dev
AdminUser:Password = MySecurePass123
ConnectionStrings:DefaultConnection = Server=localhost;Database=LighthouseDb;Trusted_Connection=True;TrustServerCertificate=True
```

### 6. Run the Application

```powershell
dotnet run
```

The admin user will be automatically created/updated with the credentials from user-secrets.

---

## Production Deployment

In production, use **environment variables** instead of user-secrets:

### Database Connection String
```powershell
$env:ConnectionStrings__DefaultConnection = "Server=127.0.0.1,1433;Database=pearlxcoreDevDb;User Id=sa;Password=YourSecurePassword123!;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30"
```

### Windows Server / IIS:
```powershell
$env:AdminUser__Email = "admin@yourdomain.com"
$env:AdminUser__Password = "YourSecurePassword123!"
```

### Linux / Docker:
```bash
export AdminUser__Email="admin@yourdomain.com"
export AdminUser__Password="YourSecurePassword123!"
```

### Docker Compose:
```yaml
environment:
  - AdminUser__Email=admin@yourdomain.com
  - AdminUser__Password=YourSecurePassword123!
```

### Azure App Service:
1. Go to **Settings → Configuration**
2. Add new Application Settings:
   - `AdminUser__Email` = `admin@yourdomain.com`
   - `AdminUser__Password` = `YourSecurePassword123!`

3. Click **Save**

---

## Notes

- User secrets are **local-only** and never committed to Git
- Environment variable names use `__` (double underscore) instead of `:` (colon)
- You can have different credentials per environment by using different secret stores
- To clear all secrets: `dotnet user-secrets clear`
- To remove a specific secret: `dotnet user-secrets remove "AdminUser:Email"`
- To remove the local DB string: `dotnet user-secrets remove "ConnectionStrings:DefaultConnection"`

---

## Troubleshooting

### Secrets not being read?

1. Verify secrets are initialized:
   ```powershell
   dotnet user-secrets list
   ```

2. Check project file has UserSecretsId:
   ```xml
   <PropertyGroup>
       <UserSecretsId>your-project-guid</UserSecretsId>
   </PropertyGroup>
   ```

3. Rebuild project:
   ```powershell
   dotnet clean
   dotnet build
   ```

### Error: "Unable to find a project in the current directory"

Run commands from the project root directory where `.csproj` file is located.

### Can I see the secrets.json file?

The `secrets.json` file is stored outside the project directory. On Windows, you can view it at:

```powershell
$env:APPDATA\Microsoft\UserSecrets\<project-guid>\secrets.json
```

But it's easier to use `dotnet user-secrets list` to view them.
