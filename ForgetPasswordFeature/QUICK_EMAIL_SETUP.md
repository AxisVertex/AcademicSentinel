# Email Configuration Quick Setup Guide

## The Issue
Your password reset and email verification system is **fully implemented**, but it's currently **not sending emails** because credentials are missing in `appsettings.json`.

## Solution: Set Email Credentials

### Method 1: User Secrets (RECOMMENDED for Development)

**Step 1:** Open PowerShell and navigate to the server project
```powershell
cd "E:\Visual Studio\For Thesis Codes\AcademicSentinel - Copy\AcademicSentinel - Copy\AcademicSentinel.Server"
```

**Step 2:** Initialize user secrets (first time only)
```powershell
dotnet user-secrets init
```

**Step 3:** Set your email credentials
```powershell
dotnet user-secrets set "Email:From" "your-email@gmail.com"
dotnet user-secrets set "Email:Username" "your-email@gmail.com"
dotnet user-secrets set "Email:Password" "your-app-specific-password"
```

**For Gmail:**
- Use your Gmail address for `From` and `Username`
- Generate an **App Password** (not your regular password):
  1. Go to https://myaccount.google.com/security
  2. Enable 2-Factor Authentication (if not already)
  3. Go to "App passwords" under Security
  4. Select "Mail" and "Windows Computer"
  5. Copy the generated password and use it above

**For Outlook/Office365:**
- Use your Outlook email for both `From` and `Username`
- Use your Outlook password for `Password`

### Method 2: appsettings.Development.json

Edit `AcademicSentinel.Server/appsettings.Development.json` and add:

```json
{
  "Email": {
    "From": "your-email@gmail.com",
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Method 3: Environment Variables

In PowerShell:
```powershell
$env:Email__From = "your-email@gmail.com"
$env:Email__Username = "your-email@gmail.com"
$env:Email__Password = "your-app-password"
```

---

## Verify It's Working

1. **Build the project** to ensure no errors
2. **Run the server** (F5 or dotnet run)
3. **Test the endpoint** using Postman or your client:
   ```
   POST http://localhost:5000/api/auth/forgot-password
   Content-Type: application/json

   {
     "email": "test@example.com"
   }
   ```
4. **Check the email inbox** for the verification code
5. If you receive the code, everything is working! ✅

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Email settings are not configured" error | You haven't set Email:From, Username, or Password. Use one of the methods above. |
| Email fails to send | Check credentials are correct. For Gmail, ensure you're using an App Password, not your regular password. |
| Server won't start | Verify the configuration syntax is correct. Check `appsettings.json` for JSON formatting errors. |

---

## What's Configured

✅ **Password Reset Flow** - Three endpoints ready to use
✅ **Database Fields** - Migration applied with password reset columns
✅ **Email Template** - Ready to send codes
✅ **Security** - BCrypt hashing, token expiration, account enumeration prevention
✅ **SMTP Support** - Gmail, Outlook, Hotmail, Office365 (auto-detected)

**Only missing:** Your email credentials
