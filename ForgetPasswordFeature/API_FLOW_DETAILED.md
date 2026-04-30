# Password Reset & Email Verification - Complete API Flow

## 3-Step Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│  USER INITIATES PASSWORD RESET                              │
└─────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 1: FORGOT PASSWORD                                    │
│  POST /api/auth/forgot-password                             │
│                                                              │
│  Request Body:                                               │
│  {                                                           │
│    "email": "user@example.com"                              │
│  }                                                           │
└─────────────────────────────────────────────────────────────┘
                         │
                         ▼
             Server-side Processing:
    ┌─────────────────────────────────────┐
    │ 1. Normalize email (validate domain)│
    │ 2. Find user in database            │
    │ 3. Generate 6-digit code (123456)   │
    │ 4. Hash code with BCrypt            │
    │ 5. Set expiration = now + 10 mins   │
    │ 6. Save to database                 │
    │ 7. Send code via email              │
    └─────────────────────────────────────┘
                         │
                         ▼
             Response (same for all cases):
┌─────────────────────────────────────────────────────────────┐
│  HTTP 200 OK                                                │
│  {                                                           │
│    "message": "If the account exists, a verification code   │
│               has been sent."                               │
│  }                                                           │
│                                                              │
│  ⚠️  Generic message to prevent account enumeration          │
└─────────────────────────────────────────────────────────────┘
                         │
         ┌───────────────┴───────────────┐
         │                               │
    EMAIL SENT ✉️           USER CHECKS EMAIL ✓
         │                               │
         └───────────────┬───────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 2: VERIFY RESET CODE                                  │
│  POST /api/auth/verify-reset-code                           │
│                                                              │
│  Request Body:                                               │
│  {                                                           │
│    "email": "user@example.com",                             │
│    "code": "123456"  ← Code from email                      │
│  }                                                           │
└─────────────────────────────────────────────────────────────┘
                         │
                         ▼
             Server-side Processing:
    ┌─────────────────────────────────────┐
    │ 1. Normalize email                  │
    │ 2. Find user in database            │
    │ 3. Check if code hash exists        │
    │ 4. Check if code expired            │
    │ 5. Verify code against hash         │
    │ 6. Generate 32-byte reset token     │
    │ 7. Set token expiration = now + 15 min │
    │ 8. Clear code hash (one-time use)   │
    │ 9. Save to database                 │
    └─────────────────────────────────────┘
                         │
         ┌───────────────┴───────────────┐
         │                               │
    Code Valid?                      Code Invalid?
         │                               │
         ▼                               ▼
    Continue               HTTP 401 Unauthorized
         │                 {
         │                   "error": "Invalid or
         │                   expired verification code"
         │                 }
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│  HTTP 200 OK                                                │
│  {                                                           │
│    "success": true,                                          │
│    "resetToken": "a1b2c3d4e5f6..."  ← Use in next step    │
│  }                                                           │
└─────────────────────────────────────────────────────────────┘
                         │
                         ▼
         USER ENTERS NEW PASSWORD ✓
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 3: RESET PASSWORD                                     │
│  POST /api/auth/reset-password                              │
│                                                              │
│  Request Body:                                               │
│  {                                                           │
│    "email": "user@example.com",                             │
│    "newPassword": "NewPassword123",                         │
│    "resetToken": "a1b2c3d4e5f6..."                         │
│  }                                                           │
└─────────────────────────────────────────────────────────────┘
                         │
                         ▼
             Server-side Processing:
    ┌─────────────────────────────────────┐
    │ 1. Normalize email                  │
    │ 2. Validate password length (6+)    │
    │ 3. Find user in database            │
    │ 4. Check if reset token exists      │
    │ 5. Check if token expired           │
    │ 6. Verify token matches             │
    │ 7. Hash new password with BCrypt    │
    │ 8. Update password hash             │
    │ 9. Clear all reset fields           │
    │ 10. Save to database                │
    └─────────────────────────────────────┘
                         │
         ┌───────────────┴───────────────┐
         │                               │
    Token Valid?                    Token Invalid?
         │                               │
         ▼                               ▼
    Continue              HTTP 401 Unauthorized
         │                {
         │                  "error": "Invalid or
         │                  expired reset session"
         │                }
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│  HTTP 200 OK                                                │
│  {                                                           │
│    "message": "Password reset successful."                  │
│  }                                                           │
│                                                              │
│  ✅ User can now login with new password                    │
└─────────────────────────────────────────────────────────────┘
```

---

## Database Schema

### Users Table - Password Reset Fields

```sql
Users
├── Id (int) [Primary Key]
├── Email (text)
├── FullName (text)
├── PasswordHash (text) ← Current password (BCrypt hashed)
├── Role (text)
├── CreatedAt (datetime)
├── ProfileImageUrl (text, nullable)
├── ProfileImagePath (text, nullable)
├── ProfileImageContentType (text, nullable)
├── ProfileImageSize (bigint, nullable)
├── ProfileImageUploadedAt (datetime, nullable)
│
├── PasswordResetCodeHash (text, nullable) ← 6-digit code (BCrypt hashed)
├── PasswordResetCodeExpiresAt (datetime, nullable) ← Code expiration time
├── PasswordResetToken (text, nullable) ← 32-byte hex token
└── PasswordResetTokenExpiresAt (datetime, nullable) ← Token expiration time
```

---

## Data Flow - Step by Step

### Step 1: Generate and Store Code

```csharp
// Generate random 6-digit code (100000-999999)
var code = GenerateSixDigitCode(); // e.g., "654321"

// Hash with BCrypt (never store plaintext!)
user.PasswordResetCodeHash = BCrypt.Net.BCrypt.HashPassword(code);

// Set expiration
user.PasswordResetCodeExpiresAt = DateTime.UtcNow.AddMinutes(10);

// Send code via email
await _emailSender.SendPasswordResetCodeAsync(user.Email, code);
```

**Email Sent to User:**
```
Subject: AcademicSentinel Password Reset Code
Body: Your AcademicSentinel verification code is: 654321
      This code will expire in 10 minutes.
```

---

### Step 2: Verify Code and Issue Token

```csharp
// User provides code from email
var userProvidedCode = "654321"; // From email

// Check if code still valid
if (user.PasswordResetCodeExpiresAt < DateTime.UtcNow)
{
    // Code expired, reject
    return Unauthorized("Invalid or expired verification code.");
}

// Verify against hash
if (!BCrypt.Net.BCrypt.Verify(userProvidedCode, user.PasswordResetCodeHash))
{
    // Code doesn't match, reject
    return Unauthorized("Invalid or expired verification code.");
}

// Generate secure reset token (32 random bytes as hex)
var resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
// e.g., "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6"

// Store token and set expiration
user.PasswordResetToken = resetToken;
user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);

// Clear the code (one-time use)
user.PasswordResetCodeHash = null;
user.PasswordResetCodeExpiresAt = null;

await _context.SaveChangesAsync();
```

---

### Step 3: Reset Password

```csharp
// User provides email, new password, and reset token
var userProvidedToken = "a1b2c3d4e5f6..."; // From step 2 response

// Check if token still valid
if (user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
{
    return Unauthorized("Invalid or expired reset session.");
}

// Verify token matches
if (!string.Equals(user.PasswordResetToken, userProvidedToken, StringComparison.Ordinal))
{
    return Unauthorized("Invalid or expired reset session.");
}

// Hash new password with BCrypt
user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

// Clear all reset fields
user.PasswordResetToken = null;
user.PasswordResetTokenExpiresAt = null;

await _context.SaveChangesAsync();
```

---

## Security Features Explained

### 1. ✅ Email Normalization
```csharp
// Trim and lowercase email for consistency
var parsed = new MailAddress(email.Trim());
return parsed.Address.ToLowerInvariant();
```
**Why:** Prevents case-sensitivity issues; ensures "User@Example.COM" = "user@example.com"

---

### 2. ✅ Domain Validation
```csharp
// DNS lookup to verify domain exists
_ = Dns.GetHostEntry(domain);
```
**Why:** Rejects fake domains like "user@madeup123.com"; catches typos

---

### 3. ✅ Code Hashing (BCrypt)
```csharp
// Never store code in plaintext!
user.PasswordResetCodeHash = BCrypt.Net.BCrypt.HashPassword(code);

// Later, verify code against hash
if (!BCrypt.Net.BCrypt.Verify(userProvidedCode, user.PasswordResetCodeHash))
{
    return Unauthorized(...);
}
```
**Why:** If database is breached, attacker can't read the codes

---

### 4. ✅ Time-Based Expiration
```csharp
// Code: 10 minutes
user.PasswordResetCodeExpiresAt = DateTime.UtcNow.AddMinutes(10);

// Token: 15 minutes
user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);

// Check expiration
if (user.PasswordResetCodeExpiresAt < DateTime.UtcNow)
{
    return Unauthorized("Code expired");
}
```
**Why:** Limits the window for brute-force attacks

---

### 5. ✅ Account Enumeration Prevention
```csharp
// Always return same message, whether user exists or not
if (user == null)
{
    return Ok(new { message = "If the account exists, a verification code has been sent." });
}
```
**Why:** Attacker can't discover which emails are registered

---

### 6. ✅ One-Time Use Code
```csharp
// After successful verification, clear the code
user.PasswordResetCodeHash = null;
user.PasswordResetCodeExpiresAt = null;
```
**Why:** Code can't be reused even if someone intercepts it

---

### 7. ✅ Password Hashing (BCrypt with Salt)
```csharp
// BCrypt automatically handles salt generation and verification
user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

// Later, verify password
if (!BCrypt.Net.BCrypt.Verify(loginPassword, user.PasswordHash))
{
    return Unauthorized(...);
}
```
**Why:** Even if database is breached, attacker can't recover original passwords

---

## API Endpoints Summary

| Endpoint | Method | Purpose | Body |
|----------|--------|---------|------|
| `/api/auth/forgot-password` | POST | Request password reset code | `{ email }` |
| `/api/auth/verify-reset-code` | POST | Validate code and get reset token | `{ email, code }` |
| `/api/auth/reset-password` | POST | Change password with reset token | `{ email, newPassword, resetToken }` |

---

## Error Responses

### Invalid Code or Expired
```
HTTP 401 Unauthorized
{
  "error": "Invalid or expired verification code."
}
```

### Invalid Token or Expired
```
HTTP 401 Unauthorized
{
  "error": "Invalid or expired reset session."
}
```

### Missing Required Fields
```
HTTP 400 Bad Request
{
  "error": "Email and code are required."
}
```

### Password Too Short
```
HTTP 400 Bad Request
{
  "error": "Password must be at least 6 characters long."
}
```

---

## Testing Checklist

- [ ] Configure email credentials (see QUICK_EMAIL_SETUP.md)
- [ ] Run server locally
- [ ] Call `/forgot-password` with valid email
- [ ] Check email inbox for code
- [ ] Call `/verify-reset-code` with code
- [ ] Receive reset token in response
- [ ] Call `/reset-password` with new password and token
- [ ] Login with new password ✅

---

**Status:** ✅ All endpoints implemented and ready to use (awaiting email configuration)
