# 📝 SECURE EMAIL CONFIG - CHEAT SHEET

Print this out or keep it in your bookmarks!

---

## 🚀 ONE-TIME SETUP (5 minutes)

```powershell
# Step 1: Navigate to project
cd "E:\Visual Studio\For Thesis Codes\FourCUDA Clone Projects\AcademicSentinel.Server"

# Step 2: Store your credentials
dotnet user-secrets set "Email:From" "pajaganasdarryll2004@gmail.com"
dotnet user-secrets set "Email:Username" "pajaganasdarryll2004@gmail.com"
dotnet user-secrets set "Email:Password" "boww pazn riqb rpba"

# Step 3: Verify
dotnet user-secrets list
```

Expected output:
```
Email:From = pajaganasdarryll2004@gmail.com
Email:Password = boww pazn riqb rpba
Email:Username = pajaganasdarryll2004@gmail.com
```

---

## 📊 CONFIGURATION SOURCES

```
┌─────────────────────────────────────────┐
│ At Runtime (IConfiguration lookup)      │
└─────────────────────────────────────────┘

DEVELOPMENT (dotnet run)
═══════════════════════
1. Environment Variables (not set)
2. User Secrets ✅ FOUND → Use this value
3. appsettings.json (never reached)

PRODUCTION (dotnet run --environment Production)
═══════════════════════════════════════
1. Environment Variables ✅ FOUND → Use this value
2. User Secrets (never reached - ignored in prod)
3. appsettings.json (never reached)
```

---

## 📂 WHERE THINGS ARE STORED

```
Your Project:
  AcademicSentinel.Server/
    ├── appsettings.json (EMPTY fields)
    │   "Password": ""  ← Safe for git
    │
    └── Program.cs (No changes)

Your Computer (Hidden):
  C:\Users\YourUsername\AppData\Roaming\Microsoft\UserSecrets\
    └── d2daf8b8-f2d4-4b0d-9608-bdedd21f6970\
        └── secrets.json (NOT in git) ← Your passwords stored here
            {
              "Email:Password": "boww pazn riqb rpba"
            }
```

---

## ✅ QUICK COMMANDS

```powershell
# View all secrets
dotnet user-secrets list

# Add/update a secret
dotnet user-secrets set "Email:Password" "new-value"

# Remove a secret
dotnet user-secrets remove "Email:Password"

# Clear all secrets for this project
dotnet user-secrets clear

# Initialize secrets (if not already done)
dotnet user-secrets init

# Build and test
dotnet build
dotnet run
```

---

## 🔒 SECURITY CHECKLIST

- [ ] ✅ Gmail credentials in user secrets (not appsettings.json)
- [ ] ✅ appsettings.json has empty Email fields
- [ ] ✅ appsettings.json is safe to commit to GitHub
- [ ] ✅ User secrets are local-only (never in git)
- [ ] ✅ Build succeeds with no errors
- [ ] ✅ Project runs without config warnings

---

## 🎯 CONFIGURATION PRIORITY

| Environment | Source 1 | Source 2 | Source 3 |
|-------------|----------|----------|----------|
| **Dev** | Env Vars (empty) | **User Secrets ✅** | appsettings.json |
| **Prod** | **Env Vars ✅** | (skipped) | appsettings.json |

✅ = Where value is found and used

---

## 💡 MENTAL MODEL

```
Your Code:
    var password = _configuration["Email:Password"];

Behind the scenes:
    "Is there an environment variable?"
        No
    "Is there a user secret?"
        Yes! → Return it

Result: password = "boww pazn riqb rpba" ✅

Everything automatic - no code changes needed!
```

---

## ⚙️ appsettings.json (Should Look Like This)

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "From": "",
    "Username": "",
    "Password": ""
  }
}
```

✅ All three Email fields are EMPTY  
✅ Safe to push to GitHub

---

## 🐛 TROUBLESHOOTING

| Problem | Solution |
|---------|----------|
| Command not found | Make sure you're in AcademicSentinel.Server directory |
| Secrets not listed | Rerun `dotnet user-secrets set` commands |
| "Password is empty" | Verify with `dotnet user-secrets list` |
| Build fails | Check appsettings.json for syntax errors |
| Email doesn't send | Check server logs for SMTP errors |

---

## 👥 FOR YOUR TEAM

Each developer runs (once):
```powershell
cd AcademicSentinel.Server
dotnet user-secrets set "Email:From" "their-email@gmail.com"
dotnet user-secrets set "Email:Username" "their-email@gmail.com"
dotnet user-secrets set "Email:Password" "their-app-password"
```

Result: Each developer has their own local secrets ✅

---

## 🚀 FOR PRODUCTION

Set environment variables (don't use user secrets):
```bash
# On production server
export Email:From=email@gmail.com
export Email:Username=email@gmail.com
export Email:Password=production-password

dotnet run --environment Production
```

Same code works! ✅

---

## 📋 FILE CHECKLIST

| File | Status | Notes |
|------|--------|-------|
| appsettings.json | ✅ Keep empty fields | Safe for git |
| Program.cs | ✅ No changes | Auto-loads secrets |
| OutlookEmailSender.cs | ✅ No changes | Reads IConfiguration |
| .csproj | ✅ Already has ID | UserSecretsId present |
| User Secrets | ✅ Create locally | `dotnet user-secrets set` |

---

## 🎓 KEY CONCEPTS (1 Minute Read)

**User Secrets:**
- Store sensitive values locally (not in git)
- Development-only (production uses env vars instead)
- Per-developer (each person has their own)
- Automatically loaded by ASP.NET Core

**Configuration Override:**
- Later sources override earlier sources
- User Secrets > appsettings.json
- Env Vars > User Secrets (in production)

**Same Code, Different Config:**
- Your code doesn't change
- ASP.NET Core finds config from right source
- Works in dev, staging, production

---

## ⏱️ TIME BREAKDOWN

| Task | Time |
|------|------|
| Navigate to folder | 10 sec |
| Store 3 secrets | 30 sec |
| Verify with list | 10 sec |
| Build project | 20 sec |
| Read quick reference | 2 min |
| **TOTAL** | **~5 min** |

---

## ✨ BENEFITS

```
BEFORE (Insecure):
  appsettings.json → Contains password → Committed to git ❌

AFTER (Secure):
  appsettings.json → Empty fields → Safe in git ✅
  User Secrets → Contains password → Local only ✅
```

Security gain: **⭐⭐⭐⭐⭐**

---

## 🔗 REFERENCE

- **Quick Setup:** ACTION_PLAN_5_MIN_SETUP.md
- **Full Guide:** SECURE_EMAIL_CONFIG_GUIDE.md
- **Visual Guide:** EMAIL_CONFIG_VISUAL_ARCHITECTURE.md
- **FAQ:** COMPLETE_SECURE_EMAIL_SETUP.md
- **Summary:** SECURE_EMAIL_CONFIG_FINAL_SUMMARY.md

---

## 📌 BOOKMARK THIS

**If you forget everything, just remember:**

```powershell
# 3 commands to run (once):
cd "E:\Visual Studio\For Thesis Codes\FourCUDA Clone Projects\AcademicSentinel.Server"
dotnet user-secrets set "Email:From" "pajaganasdarryll2004@gmail.com"
dotnet user-secrets set "Email:Username" "pajaganasdarryll2004@gmail.com"
dotnet user-secrets set "Email:Password" "boww pazn riqb rpba"
dotnet user-secrets list

# Verify:
dotnet build
dotnet run
```

That's it! ✅

---

**Status:** ✅ Ready to Use  
**Difficulty:** ⭐ (Very Easy)  
**Time Required:** ~5 minutes  
**Security Impact:** ⭐⭐⭐⭐⭐ Critical  

