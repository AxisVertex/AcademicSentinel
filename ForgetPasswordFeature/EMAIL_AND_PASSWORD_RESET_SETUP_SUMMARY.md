# Forget Password & Email Verification Setup - Complete Reference

## Overview
Your AcademicSentinel project has a complete **three-step password reset flow** with email verification:
1. **Forgot Password** → Send verification code via email
2. **Verify Code** → Validate code and issue reset token
3. **Reset Password** → Change password using reset token

---

## Files Involved

### 1. **Models/User.cs** (Database Schema)
**Location:** `AcademicSentinel.Server/Models/User.cs`

Contains password reset fields:
```csharp
public string? PasswordResetCodeHash { get; set; }        // Hashed 6-digit code
public DateTime? PasswordResetCodeExpiresAt { get; set; } // Code expires in 10 minutes
public string? PasswordResetToken { get; set; }            // Token for final reset
public DateTime? PasswordResetTokenExpiresAt { get; set; } // Token expires in 15 minutes
```

**Status:** ✅ Configured

---

### 2. **Controllers/AuthController.cs** (API Endpoints)
**Location:** `AcademicSentinel.Server/Controllers/AuthController.cs`

**Three endpoints implemented:**

#### a) `POST /api/auth/forgot-password`
- **Input:** `ForgotPasswordRequestDto` (email)
- **Process:**
  - Generates 6-digit code
  - Hashes code with BCrypt
  - Sets 10-minute expiration
  - Sends code via email
- **Output:** Generic success message (prevents account enumeration)
- **Status:** ✅ Working

#### b) `POST /api/auth/verify-reset-code`
- **Input:** `VerifyResetCodeRequestDto` (email, code)
- **Process:**
  - Validates code against hash
  - Checks expiration
  - Generates 32-byte reset token
  - Sets 15-minute token expiration
- **Output:** `VerifyResetCodeResponseDto` (resetToken)
- **Status:** ✅ Working

#### c) `POST /api/auth/reset-password`
- **Input:** `ResetPasswordRequestDto` (email, newPassword, resetToken)
- **Process:**
  - Validates reset token and expiration
  - Hashes new password with BCrypt
  - Clears all reset fields
- **Output:** Success message
- **Status:** ✅ Working

**Helper Methods:**
- `GenerateSixDigitCode()` - Creates random code from 100000-999999
- `NormalizeEmail()` - Validates and normalizes email addresses
- `HasResolvableDomain()` - Checks if email domain exists (DNS lookup)

---

### 3. **Services/IEmailSender.cs** (Email Interface)
**Location:** `AcademicSentinel.Server/Services/IEmailSender.cs`

```csharp
public interface IEmailSender
{
    Task SendPasswordResetCodeAsync(string toEmail, string code);
}
```

**Status:** ✅ Defined

---

### 4. **Services/OutlookEmailSender.cs** (Email Implementation)
**Location:** `AcademicSentinel.Server/Services/OutlookEmailSender.cs`

**Features:**
- Supports Gmail, Outlook, Hotmail, Live, Office365
- Auto-resolves SMTP host from email domain
- Uses TLS on port 587
- Reads configuration from `appsettings.json`

**Configuration Properties:**
- `Email:SmtpHost` - Optional (auto-resolved if not set)
- `Email:SmtpPort` - Optional (defaults to 587)
- `Email:From` - Sender email address
- `Email:Username` - SMTP authentication username
- `Email:Password` - SMTP authentication password

**Status:** ✅ Implemented

**Email Template:**
```
Subject: AcademicSentinel Password Reset Code
Body: Your AcademicSentinel verification code is: {code}
      This code will expire in 10 minutes.
```

---

### 5. **DTOs/AuthDTOs.cs** (Data Transfer Objects)
**Location:** `AcademicSentinel.Server/DTOs/AuthDTOs.cs`

Four DTOs for password reset flow:

```csharp
public class ForgotPasswordRequestDto
{
    public string Email { get; set; } = string.Empty;
}

public class VerifyResetCodeRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class VerifyResetCodeResponseDto
{
    public bool Success { get; set; }
    public string ResetToken { get; set; } = string.Empty;
}

public class ResetPasswordRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ResetToken { get; set; } = string.Empty;
}
```

**Status:** ✅ Defined

---

### 6. **Migrations/20260412103745_AddForgotPasswordFields.cs** (Database)
**Location:** `AcademicSentinel.Server/Migrations/20260412103745_AddForgotPasswordFields.cs`

Adds 4 columns to Users table:
- `PasswordResetCodeHash` (TEXT, nullable)
- `PasswordResetCodeExpiresAt` (TEXT, nullable)
- `PasswordResetToken` (TEXT, nullable)
- `PasswordResetTokenExpiresAt` (TEXT, nullable)

**Status:** ✅ Applied

---

### 7. **Configuration Files**

#### appsettings.json
**Location:** `AcademicSentinel.Server/appsettings.json`

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "From": "",           // ⚠️ NEEDS CONFIG
    "Username": "",       // ⚠️ NEEDS CONFIG
    "Password": ""        // ⚠️ NEEDS CONFIG
  }
}
```

**Status:** ⚠️ **Email credentials NOT configured**

#### appsettings.Development.json
**Location:** `AcademicSentinel.Server/appsettings.Development.json`

Currently only has logging settings. Can override Email settings here for development.

**Status:** ℹ️ Optional development overrides

---

### 8. **Program.cs** (Dependency Injection)
**Location:** `AcademicSentinel.Server/Program.cs`

Email service registration:
```csharp
builder.Services.AddScoped<IEmailSender, OutlookEmailSender>();
```

**Status:** ✅ Configured

---

## Security Features Implemented

✅ **Password hashing** - BCrypt with salt
✅ **Email validation** - Domain DNS lookup
✅ **Code hashing** - Never stored in plaintext
✅ **Token expiration** - Code (10min), Token (15min)
✅ **Account enumeration prevention** - Generic success message
✅ **Reset token expiration** - Prevents token reuse

---

## What's Missing / To Do

### ⚠️ **CRITICAL: Email Configuration**

To make email sending work, you must configure credentials. Two options:

#### Option A: Visual Studio User Secrets (Recommended for Development)
```powershell
# In PowerShell, navigate to the project directory
cd "E:\Visual Studio\For Thesis Codes\AcademicSentinel - Copy\AcademicSentinel - Copy\AcademicSentinel.Server"

# Initialize user secrets (one-time)
dotnet user-secrets init

# Set email credentials
dotnet user-secrets set "Email:From" "your-email@gmail.com"
dotnet user-secrets set "Email:Username" "your-email@gmail.com"
dotnet user-secrets set "Email:Password" "your-app-password"
```

#### Option B: Environment Variables (Production)
```powershell
$env:Email__From = "your-email@gmail.com"
$env:Email__Username = "your-email@gmail.com"
$env:Email__Password = "your-app-password"
```

#### Option C: appsettings.Development.json
```json
{
  "Email": {
    "From": "your-email@gmail.com",
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
  }
}
```

**Note:** For Gmail, use an App Password, not your regular password.

---

## Testing the Flow

### Step 1: Request Password Reset
```bash
POST /api/auth/forgot-password
Content-Type: application/json

{
  "email": "user@example.com"
}
```

Expected Response:
```json
{
  "message": "If the account exists, a verification code has been sent."
}
```

### Step 2: Verify Code (received via email)
```bash
POST /api/auth/verify-reset-code
Content-Type: application/json

{
  "email": "user@example.com",
  "code": "123456"
}
```

Expected Response:
```json
{
  "success": true,
  "resetToken": "a1b2c3d4e5f6..."
}
```

### Step 3: Reset Password
```bash
POST /api/auth/reset-password
Content-Type: application/json

{
  "email": "user@example.com",
  "newPassword": "NewPassword123",
  "resetToken": "a1b2c3d4e5f6..."
}
```

Expected Response:
```json
{
  "message": "Password reset successful."
}
```

---

## Database Status

✅ **Migration applied** - The `20260412103745_AddForgotPasswordFields` migration has been applied.

Verify with:
```powershell
cd AcademicSentinel.Server
dotnet ef migrations list
```

You should see the migration in the applied list.

---

## Summary

| Component | Status | Notes |
|-----------|--------|-------|
| User Model Fields | ✅ Done | 4 password reset fields added |
| API Endpoints | ✅ Done | 3 endpoints (forgot, verify, reset) |
| Email Service Interface | ✅ Done | IEmailSender defined |
| Email Implementation | ✅ Done | OutlookEmailSender with auto-domain resolution |
| DTOs | ✅ Done | All 4 DTOs defined |
| Database Migration | ✅ Done | Migration applied |
| Dependency Injection | ✅ Done | Service registered in Program.cs |
| **Email Credentials** | ⚠️ **NEEDS CONFIG** | Must set Email:From, Username, Password |
| **Database Updated** | ✅ Done | Columns exist in Users table |

---

## Quick Start to Make It Work

1. **Configure Email Credentials** (choose one method above)
2. **Test in Postman or your client app**
3. **Check email inbox** for the verification code
4. **Complete the three-step flow**

That's it! The system is ready once you add email credentials.
