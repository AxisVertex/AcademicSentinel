# ⚡ Email SMTP Configuration - Quick Reference Card

## 🎯 GOAL
Store Gmail credentials securely using **dotnet user-secrets** (development best practice)

---

## ✅ 3-STEP SETUP (Copy-Paste)

```powershell
# 1. Navigate to project
cd "E:\Visual Studio\For Thesis Codes\FourCUDA Clone Projects\AcademicSentinel.Server"

# 2. Initialize user secrets (one-time only)
dotnet user-secrets init

# 3. Store your Gmail credentials
dotnet user-secrets set "Email:From" "pajaganasdarryll2004@gmail.com"
dotnet user-secrets set "Email:Username" "pajaganasdarryll2004@gmail.com"
dotnet user-secrets set "Email:Password" "boww pazn riqb rpba"
```

**Done!** Your code automatically reads from user secrets.

---

## 📊 Configuration Priority (Runtime Resolution)

```
User Secrets (highest)
    ↓ IConfiguration["Email:Password"] returns from HERE
appsettings.json (lowest)
```

**What This Means:**
- ✅ User secrets override appsettings.json
- ✅ Your `OutlookEmailSender.cs` code needs NO changes
- ✅ `IConfiguration["Email:Password"]` automatically gets the secret value

---

## 🔒 appsettings.json (Safe to Commit)

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "From": "",              ← Leave empty (secrets fill this)
    "Username": "",          ← Leave empty (secrets fill this)
    "Password": ""           ← Leave empty (secrets fill this)
  }
}
```

✅ **Why:** Non-sensitive values stay here, passwords in secrets only.

---

## 🗂️ File Locations

| Location | Content | Version-Controlled | Sensitive |
|----------|---------|-------------------|-----------|
| `appsettings.json` | SMTP host, port | ✅ YES | ❌ NO |
| User Secrets | Email passwords | ❌ NO | ✅ YES |
| `.csproj` | UserSecretsId (auto-set) | ✅ YES | ❌ NO |
| Code | Reads IConfiguration | ✅ YES | ❌ NO |

---

## ✔ VERIFY IT WORKS

```powershell
# See all stored secrets
dotnet user-secrets list

# Expected output:
# Email:From = pajaganasdarryll2004@gmail.com
# Email:Password = boww pazn riqb rpba
# Email:Username = pajaganasdarryll2004@gmail.com

# Start server to test
dotnet run
```

---

## 🛠️ Manage Secrets

```powershell
# List all secrets
dotnet user-secrets list

# Update a secret
dotnet user-secrets set "Email:Password" "new-password"

# Remove a secret
dotnet user-secrets remove "Email:Password"

# Clear all secrets for this project
dotnet user-secrets clear

# Open secrets file in editor
dotnet user-secrets set --help
```

---

## 🚀 Configuration in Different Environments

| Environment | Configuration Source | Command |
|-------------|----------------------|---------|
| **Development** | User Secrets | `dotnet run` |
| **Staging** | appsettings.Staging.json | `dotnet run --environment Staging` |
| **Production** | Environment Variables | `SET Email:Password=xxx` then `dotnet run --environment Production` |

**Same code** - Different configuration source based on environment.

---

## ✅ SECURITY CHECKLIST

- [ ] User secrets initialized: `dotnet user-secrets init`
- [ ] Email credentials in secrets: `dotnet user-secrets set Email:Password "..."`
- [ ] appsettings.json has empty Email fields
- [ ] appsettings.json committed to git (no secrets!)
- [ ] `.gitignore` excludes user secrets (auto-excluded)
- [ ] Verified with: `dotnet user-secrets list`
- [ ] Server starts without configuration errors
- [ ] Email sending works in practice

---

## ❌ What NOT to Do

| ❌ Don't | ✅ Do Instead |
|----------|--------------|
| Store passwords in appsettings.json | Use user-secrets in development |
| Commit secrets to git | Keep secrets local only |
| Hardcode passwords in code | Use IConfiguration to read them |
| Use regular Gmail password | Use Gmail App Password |
| Commit UserSecretsId changes | .csproj already has it |

---

## 🎓 How It Works Under the Hood

```csharp
// Your code (UNCHANGED):
var password = _configuration["Email:Password"];

// At runtime, ASP.NET Core:
// 1. Loads appsettings.json → Email:Password = ""
// 2. Loads user secrets → Email:Password = "boww pazn riqb rpba"
// 3. User secrets OVERRIDE appsettings.json
// 4. Result: password = "boww pazn riqb rpba" ✅

// No code changes needed because CreateBuilder() handles this!
```

---

## 📍 User Secrets Location

Windows (Your System):
```
C:\Users\YourUsername\AppData\Roaming\Microsoft\UserSecrets\
    └── d2daf8b8-f2d4-4b0d-9608-bdedd21f6970\
        └── secrets.json
```

Find the ID in your `.csproj`:
```xml
<UserSecretsId>d2daf8b8-f2d4-4b0d-9608-bdedd21f6970</UserSecretsId>
```

---

## 🆘 Troubleshooting

| Problem | Solution |
|---------|----------|
| "Email:Password is empty" at runtime | Run `dotnet user-secrets set "Email:Password" "..."` |
| Secrets not loading | Verify with `dotnet user-secrets list` |
| Server won't start | Check secrets: `dotnet user-secrets list` |
| Old password still used | Restart Visual Studio / `dotnet run` |
| "User secrets not initialized" | Run `dotnet user-secrets init` first |

---

## 📚 Reference

**Files Involved:**
- ✅ `.csproj` - Has UserSecretsId (auto-set)
- ✅ `appsettings.json` - Empty Email fields
- ✅ `Program.cs` - No changes needed
- ✅ `OutlookEmailSender.cs` - No changes needed

**No code changes required!** Configuration merging is automatic.

---

**Status:** ✅ Ready to Use  
**Setup Time:** 2 minutes  
**Security Level:** ⭐⭐⭐⭐⭐ (Best Practice)

