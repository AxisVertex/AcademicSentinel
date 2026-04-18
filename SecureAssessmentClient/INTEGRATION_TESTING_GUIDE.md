# 🚀 INTEGRATION TESTING GUIDE

## Step-by-Step Testing of SAC-Server Connection

### Prerequisites ✅
- Both projects build successfully (0 errors)
- Unified package management in place
- Database file: `academicsentinel.db` exists
- Server listening on `http://localhost:5264`

---

## Phase 1: Initialize Test Data

### Step 1.1: Start the Server
```powershell
# Terminal 1
cd AcademicSentinel.Server
dotnet run
```

**Expected Output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5264
```

The server should show: **"Now listening on: http://localhost:5264"**

### Step 1.2: Create Test User (Development Endpoint)

In another terminal or PowerShell, call the seed endpoint:

```powershell
# Terminal 2
$response = Invoke-RestMethod -Uri "http://localhost:5264/api/auth/seed-test-user" -Method Post
Write-Host $response
```

**Expected Output:**
```
✅ Test user created successfully - ID: 1, Email: student@example.com
```

Or if user already exists:
```
Test user already exists with ID: 1
```

### Step 1.3: Create Test Room (Development Endpoint)

```powershell
$response = Invoke-RestMethod -Uri "http://localhost:5264/api/auth/seed-test-room" -Method Post
Write-Host $response
```

**Expected Output:**
```
✅ Test room created successfully - ID: 1, Name: Test Exam Room, Status: Active
```

---

## Phase 2: Test Authentication Flow

### Step 2.1: Test Manual Login Endpoint

Verify the authentication endpoint works:

```powershell
$loginData = @{
    email = "student@example.com"
    password = "SecurePass123!"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5264/api/auth/login" `
    -Method Post `
    -ContentType "application/json" `
    -Body $loginData

Write-Host "Token received: $($response.token.Substring(0, 50))..."
Write-Host "User ID: $($response.id)"
```

**Expected Output:**
```
Token received: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9Ey...
User ID: 1
```

---

## Phase 3: Test Client-Server Integration

### Step 3.1: Start the Client

```powershell
# Terminal 3
cd ..  # Go back to workspace root
dotnet run
```

The WPF client should start.

### Step 3.2: Navigate to Test Console

In the client application, navigate to the **Testing/DetectionTestConsole**.

### Step 3.3: Run Option 9 - Server Connection Test

When the console menu appears, select **Option 9** (or option for "Server Connection Test").

**Input the following credentials:**
```
Email: student@example.com
Password: SecurePass123!
Room ID: 1
```

### Step 3.4: Observe the Output

The test will execute the following steps:

1. **Authenticate with server** (POST to `/api/auth/login`)
   - ✅ SUCCESS: JWT token received
   - ❌ FAILED: Check credentials and database

2. **Establish SignalR connection**
   - ✅ SUCCESS: WebSocket connected
   - ❌ FAILED: Check hub URL and network

3. **Join exam room** (SignalR hub method)
   - ✅ SUCCESS: Client joined room group
   - ❌ FAILED: Check room status is "Active"

4. **Send test event** (SignalR hub method)
   - ✅ SUCCESS: Event transmitted to server
   - ❌ FAILED: Check SignalR connection state

**Expected Final Output:**
```
? Initializing server connection...

✅ Authentication successful - Token: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
✅ SignalR connection established successfully
✅ Successfully joined room 1
✅ Exam event transmitted: TestViolation
✅ Server connection test passed!

SignalR Connection Status:
  Connected: YES
  Hub URL: http://localhost:5264/monitoringHub
```

---

## Phase 4: Troubleshooting

### Issue: "Server connection test failed"

#### Symptom 1: Authentication Error
```
❌ Authentication failed: 401 Unauthorized
```
**Cause:** Wrong credentials or user doesn't exist
**Solution:**
1. Verify seed endpoint was called: `Invoke-RestMethod -Uri "http://localhost:5264/api/auth/seed-test-user" -Method Post`
2. Check credentials match exactly: `student@example.com` / `SecurePass123!`
3. Verify server is running on port 5264

#### Symptom 2: SignalR Connection Failed
```
❌ Network error establishing SignalR connection
```
**Cause:** Server not running or wrong URL
**Solution:**
1. Verify server is running: Check for "Now listening on: http://localhost:5264"
2. Check firewall isn't blocking port 5264
3. Verify hub URL in client test console matches: `http://localhost:5264/monitoringHub`

#### Symptom 3: "Cannot join room"
```
❌ Cannot join room: the instructor has not started the session or has ended it
```
**Cause:** Room status is not "Active"
**Solution:**
1. Recreate room with seed endpoint: `Invoke-RestMethod -Uri "http://localhost:5264/api/auth/seed-test-room" -Method Post`
2. Or manually update room status to "Active" in database

#### Symptom 4: "Possible null reference" or JWT errors
**Cause:** JWT configuration mismatch
**Solution:**
1. Verify `appsettings.json` has correct JWT keys:
   ```json
   "Jwt": {
     "Key": "ThisIsAVerySecureSecretKeyForAcademicSentinel2026!!!",
     "Issuer": "AcademicSentinelServer",
     "Audience": "AcademicSentinelClients"
   }
   ```
2. Restart server to apply configuration changes

---

## Phase 5: Full Integration Test Script

Run this complete PowerShell script to test everything:

```powershell
# Colors for output
function Write-Success { Write-Host "✅ $args" -ForegroundColor Green }
function Write-Error { Write-Host "❌ $args" -ForegroundColor Red }
function Write-Info { Write-Host "ℹ️  $args" -ForegroundColor Cyan }

Write-Info "Starting SAC-Server Integration Test..."

# 1. Test server is running
Write-Info "Testing server connectivity..."
try {
    $health = Invoke-RestMethod -Uri "http://localhost:5264/api/auth/seed-test-user" -Method Post
    Write-Success "Server is running"
} catch {
    Write-Error "Server not responding. Make sure server is running on port 5264"
    exit 1
}

# 2. Initialize test data
Write-Info "Initializing test data..."
try {
    $userResult = Invoke-RestMethod -Uri "http://localhost:5264/api/auth/seed-test-user" -Method Post
    Write-Success $userResult
    
    $roomResult = Invoke-RestMethod -Uri "http://localhost:5264/api/auth/seed-test-room" -Method Post
    Write-Success $roomResult
} catch {
    Write-Error "Failed to initialize test data: $_"
    exit 1
}

# 3. Test authentication
Write-Info "Testing authentication endpoint..."
try {
    $loginData = @{
        email = "student@example.com"
        password = "SecurePass123!"
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "http://localhost:5264/api/auth/login" `
        -Method Post `
        -ContentType "application/json" `
        -Body $loginData

    Write-Success "Authentication successful"
    Write-Success "Token: $($response.token.Substring(0, 50))..."
    Write-Success "User ID: $($response.id)"
} catch {
    Write-Error "Authentication failed: $_"
    exit 1
}

Write-Info ""
Write-Success "All tests passed! Ready for client integration testing."
Write-Info "Next: Run client with Option 9 (Server Connection Test)"
```

Save as `test-integration.ps1` and run:
```powershell
.\test-integration.ps1
```

---

## Expected Architecture After Integration

```
┌─────────────────────────────────────────────────────────┐
│                    SECURE ASSESSMENT CLIENT (.NET 9)     │
│                                                          │
│  ┌────────────────────────────────────────────────────┐  │
│  │  Detection Pipeline (Phases 1-9)                  │  │
│  │  - Gaze Tracking                                  │  │
│  │  - Face Detection                                 │  │
│  │  - Keyboard Patterns                              │  │
│  │  - Risk Assessment → EventLoggerService           │  │
│  └────────────────────────────────────────────────────┘  │
│                          ↓                               │
│  ┌────────────────────────────────────────────────────┐  │
│  │  EventLoggerService                               │  │
│  │  - Batches events (10 items or 5sec timeout)      │  │
│  │  - Local persistence fallback                     │  │
│  └────────────────────────────────────────────────────┘  │
│                          ↓                               │
│  ┌────────────────────────────────────────────────────┐  │
│  │  SignalRService                                   │  │
│  │  - AuthenticateAsync() → Gets JWT token            │  │
│  │  - ConnectAsync() → Establishes WebSocket          │  │
│  │  - JoinExamAsync() → Enters room group             │  │
│  │  - SendBatchMonitoringEventsAsync() → Real-time    │  │
│  └────────────────────────────────────────────────────┘  │
│                          ↓                               │
│                   HTTPS + WebSocket                      │
└─────────────────────────────────────────────────────────┘
                          ↓
                          ↓
┌─────────────────────────────────────────────────────────┐
│           ACADEMICSENTINEL.SERVER (.NET 10)             │
│                                                          │
│  ┌────────────────────────────────────────────────────┐  │
│  │  AuthController (/api/auth/login)                 │  │
│  │  - Validate credentials                           │  │
│  │  - Generate JWT token                             │  │
│  │  - Return token to client                         │  │
│  └────────────────────────────────────────────────────┘  │
│                          ↓                               │
│  ┌────────────────────────────────────────────────────┐  │
│  │  MonitoringHub (SignalR Hub)                       │  │
│  │  - JoinLiveExam() → Add to room group              │  │
│  │  - SendMonitoringEvent() → Store in DB             │  │
│  │  - SendBatchMonitoringEvents() → Batch process     │  │
│  └────────────────────────────────────────────────────┘  │
│                          ↓                               │
│  ┌────────────────────────────────────────────────────┐  │
│  │  AppDbContext (Entity Framework)                  │  │
│  │  - Users (authentication)                         │  │
│  │  - Rooms (exam sessions)                          │  │
│  │  - MonitoringEvents (violations)                  │  │
│  │  - SessionParticipants (connection tracking)      │  │
│  └────────────────────────────────────────────────────┘  │
│                          ↓                               │
│  ┌────────────────────────────────────────────────────┐  │
│  │  SQLite Database (academicsentinel.db)            │  │
│  └────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

---

## Success Criteria ✅

After running Option 9 test, you should see:

- ✅ **Authentication Step**: JWT token received
- ✅ **Connection Step**: WebSocket connected to hub
- ✅ **Room Join Step**: Successfully joined room 1
- ✅ **Event Transmission**: Violation event sent to server
- ✅ **Overall Status**: "Server connection test passed!"
- ✅ **SignalR Status**: Connected: YES

---

## Next Steps After Successful Integration

1. **Test with real detection events** - Use Option 8 in console to run behavioral monitoring
2. **Monitor server logs** - Watch for event receipts and acknowledgments
3. **Verify database** - Check MonitoringEvents table has event records
4. **Test persistence** - Simulate network disconnect and verify local fallback
5. **Performance profiling** - Measure batch transmission times and latency

---

**Last Updated:** Session 3 - Integration Testing Setup  
**Status:** ✅ READY FOR TESTING  
**Next Action:** Run server seed endpoint, then test client Option 9
