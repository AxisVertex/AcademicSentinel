# Email Configuration Template for appsettings.Development.json

Copy this into your `AcademicSentinel.Server/appsettings.Development.json` file:

## Template 1: Gmail Configuration

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "From": "your-email@gmail.com",
    "Username": "your-email@gmail.com",
    "Password": "your-app-specific-password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**Gmail Setup Instructions:**
1. Go to https://myaccount.google.com/security
2. Enable 2-Factor Authentication (if not already enabled)
3. Go to "App passwords" section (under Security)
4. Select Mail and Windows Computer
5. Google generates a 16-character password
6. Copy that password and paste into "Password" field above

---

## Template 2: Outlook/Office365 Configuration

```json
{
  "Email": {
    "SmtpHost": "smtp.office365.com",
    "SmtpPort": 587,
    "From": "your-email@outlook.com",
    "Username": "your-email@outlook.com",
    "Password": "your-outlook-password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**Outlook Setup Instructions:**
1. Use your Outlook email address
2. Use your Outlook password (or app-specific password if 2FA is enabled)
3. SMTP Host automatically detected as smtp.office365.com

---

## Template 3: Using User Secrets (Recommended for Development)

Instead of editing appsettings.Development.json, use PowerShell:

```powershell
# Navigate to server project
cd "AcademicSentinel.Server"

# Initialize (first time only)
dotnet user-secrets init

# Set credentials
dotnet user-secrets set "Email:From" "your-email@gmail.com"
dotnet user-secrets set "Email:Username" "your-email@gmail.com"
dotnet user-secrets set "Email:Password" "your-16-char-app-password"

# Verify
dotnet user-secrets list
```

**Advantages:**
- Credentials stored locally (not in version control)
- More secure for team development
- Can't accidentally commit passwords

---

## Template 4: Alternative Email Providers

### Hotmail
```json
{
  "Email": {
    "SmtpHost": "smtp.outlook.com",
    "SmtpPort": 587,
    "From": "your-email@hotmail.com",
    "Username": "your-email@hotmail.com",
    "Password": "your-password"
  }
}
```

### Corporate Exchange Server
```json
{
  "Email": {
    "SmtpHost": "mail.yourdomain.com",
    "SmtpPort": 587,
    "From": "notifications@yourdomain.com",
    "Username": "username@yourdomain.com",
    "Password": "your-password"
  }
}
```

### SendGrid (Recommended for Production)
```json
{
  "Email": {
    "SmtpHost": "smtp.sendgrid.net",
    "SmtpPort": 587,
    "From": "noreply@academicsentinel.com",
    "Username": "apikey",
    "Password": "SG.your-sendgrid-api-key"
  }
}
```

---

## Testing Your Configuration

### Step 1: Add Configuration
Choose one template above and add to your appsettings.Development.json or set user secrets.

### Step 2: Run Server
```powershell
cd AcademicSentinel.Server
dotnet run
```

### Step 3: Test Endpoint
Use Postman or curl:

```bash
POST http://localhost:5000/api/auth/forgot-password
Content-Type: application/json

{
  "email": "test-account@gmail.com"
}
```

### Step 4: Verify Email
Check your email inbox (might be in spam folder).

**Expected Email:**
```
From: your-email@gmail.com
Subject: AcademicSentinel Password Reset Code
Body: Your AcademicSentinel verification code is: 123456
      This code will expire in 10 minutes.
```

---

## Troubleshooting

### Error: "Email settings are not configured"
**Solution:** You haven't set Email:From, Email:Username, or Email:Password
- Try user secrets method (recommended)
- Or add to appsettings.Development.json
- Restart server after making changes

### Error: "Failed to send email"
**Causes:**
- Credentials are incorrect
- Account has 2FA enabled (need App Password instead)
- SMTP host is wrong
- Port is blocked by firewall

**Solutions:**
- Double-check email and password
- For Gmail: Generate App Password (see instructions above)
- For Outlook: Ensure 2FA settings allow SMTP
- Try different port (25, 465, or 587)

### Error: "Authentication failed"
**Causes:**
- Wrong password
- Username doesn't match SMTP requirements
- Account locked or suspended

**Solutions:**
- Verify credentials are correct
- Use 16-character App Password for Gmail
- Check if account is active in email provider
- Try logging in manually to email account

### Email goes to Spam
**Solutions:**
- Check spam/junk folder
- Add sender to contacts
- Set up SPF/DKIM records if using custom domain
- Increase email content quality

---

## Production Configuration

For production, use **environment variables** instead of hardcoding:

```powershell
# Set in your server environment
$env:Email__From = "production-email@domain.com"
$env:Email__Username = "username"
$env:Email__Password = "password"
$env:Email__SmtpHost = "smtp.sendgrid.net"
$env:Email__SmtpPort = "587"
```

Or use **Docker secrets** if containerized.

---

## Security Best Practices

✅ **DO:**
- Use App Passwords, not regular passwords
- Store credentials in user secrets or environment variables
- Use SMTP with TLS (port 587)
- Rotate credentials periodically
- Use SendGrid or similar for production

❌ **DON'T:**
- Commit credentials to git repository
- Use plain password in appsettings.json (for production)
- Use port 25 (insecure)
- Store passwords in comments or documentation
- Reuse same password across services

---

## Quick Reference

| Email Provider | Host | Port | Username | Password | App Specific? |
|---|---|---|---|---|---|
| Gmail | smtp.gmail.com | 587 | Full email | App Password* | YES ⚠️ |
| Outlook/Office365 | smtp.office365.com | 587 | Full email | Regular/App Password | Optional |
| Hotmail | smtp.outlook.com | 587 | Full email | Regular password | NO |
| SendGrid | smtp.sendgrid.net | 587 | apikey | API Key | N/A |

*Gmail requires App Password when 2FA is enabled

---

**Choose one template, add to your project, and you're done!** ✅
