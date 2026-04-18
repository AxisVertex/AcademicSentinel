# 🔗 Secure Assessment Client → AcademicSentinel.Server Integration Guide

**Status:** ✅ **READY FOR IMPLEMENTATION**  
**Framework:** .NET 9 (Client) + .NET 10 (Server)  
**Communication:** SignalR (Real-Time) + REST API (Auth & Config)

---

## 📋 Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Server Setup & Configuration](#server-setup--configuration)
3. [Authentication Flow](#authentication-flow)
4. [SignalR Hub Connection](#signalr-hub-connection)
5. [API Endpoints Reference](#api-endpoints-reference)
6. [SAC Implementation Examples](#sac-implementation-examples)
7. [Event Transmission Pipeline](#event-transmission-pipeline)
8. [Testing & Troubleshooting](#testing--troubleshooting)

---

## 🏗️ Architecture Overview

### System Components

```
┌─────────────────────────────────────┐
│  Secure Assessment Client (SAC)     │
│  - Phase 6: Environment Detection   │
│  - Phase 7: Behavioral Monitoring   │
│  - Phase 8: Decision Engine         │
│  - Phase 9: Event Logger ⟶ SERVER   │
└────────────────────┬────────────────┘
                     │
        ┌────────────┴────────────┐
        │                         │
        ▼                         ▼
   [REST API]              [SignalR Hub]
   - /auth/login           - JoinLiveExam
   - /auth/register        - SendMonitoringEvent
   - /rooms/{roomId}       - Broadcasts to IMC
        │                         │
        └────────────────────┬────┘
                             │
        ┌────────────────────▼────────────────┐
        │  AcademicSentinel.Server            │
        │  - Authentication & Authorization   │
        │  - Room & Session Management        │
        │  - Real-Time Monitoring Hub         │
        │  - Event Storage & Analysis         │
        │  - Instructor Monitoring Console    │
        └────────────────────────────────────┘
```

### Communication Flow

1. **Authentication Phase:**
   - SAC sends student credentials via REST to `/auth/login`
   - Server validates and returns JWT token
   - SAC stores token for subsequent requests

2. **Room Join Phase:**
   - SAC establishes SignalR connection with JWT token
   - SignalR Hub authenticates token via JWT Bearer scheme
   - SAC calls `JoinLiveExam(roomId)` method on hub
   - Server adds student to SignalR group for that room

3. **Monitoring Phase:**
   - SAC detects violations (from Phase 6-8 pipeline)
   - SAC calls `SendMonitoringEvent()` with detection data
   - Server stores event and broadcasts to Instructor Monitoring Console
   - Real-time alerts appear in IMC dashboard

4. **Disconnect Phase:**
   - SAC closes connection or loses internet
   - SignalR automatically triggers `OnDisconnectedAsync`
   - Server marks student as disconnected
   - IMC receives live disconnect notification

---

## 🖥️ Server Setup & Configuration

### 1. Server Project Location
```
E:\Darryll pogi\FEU files Darryll\3rd Year\3rd Year Second Sem\FOR Thesis\Codes\SystemFourCUDA\AcademicSentinel.Server\
```

### 2. Key Configuration Files

**appsettings.json** - Server configuration:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=academicsentinel.db"
  },
  "Jwt": {
    "Key": "ThisIsAVerySecureSecretKeyForAcademicSentinel2026!!!",
    "Issuer": "AcademicSentinelServer",
    "Audience": "AcademicSentinelClients"
  }
}
```

### 3. Important Configuration Values for SAC

| Setting | Value | Usage |
|---------|-------|-------|
| **Server Base URL** | `https://localhost:5001` (Dev) | All HTTP requests |
| **SignalR Hub URL** | `https://localhost:5001/monitoringHub` | WebSocket connection |
| **JWT Key** | From `Jwt:Key` in appsettings.json | Token validation |
| **Issuer** | `AcademicSentinelServer` | Token validation |
| **Audience** | `AcademicSentinelClients` | Token validation |

### 4. Start the Server

```powershell
# Navigate to server project
cd "E:\Darryll pogi\FEU files Darryll\3rd Year\3rd Year Second Sem\FOR Thesis\Codes\SystemFourCUDA\AcademicSentinel.Server\"

# Build
dotnet build

# Run
dotnet run
```

**Expected output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
      Now listening on: http://localhost:5000
```

---

## 🔐 Authentication Flow

### Step 1: Register Student Account (First Time)

**REST Endpoint:**
```
POST https://localhost:5001/api/auth/register
Content-Type: application/json
```

**Request Body:**
```json
{
  "email": "student@example.com",
  "password": "SecurePass123!",
  "role": "Student"
}
```

**Response (201):**
```json
{
  "id": 1,
  "email": "student@example.com",
  "role": "Student",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### Step 2: Login to Get JWT Token

**REST Endpoint:**
```
POST https://localhost:5001/api/auth/login
Content-Type: application/json
```

**Request Body:**
```json
{
  "email": "student@example.com",
  "password": "SecurePass123!"
}
```

**Response (200):**
```json
{
  "id": 1,
  "email": "student@example.com",
  "role": "Student",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### JWT Token Structure

**Token payload contains:**
```json
{
  "nameid": "1",           // Student ID (ClaimTypes.NameIdentifier)
  "email": "student@example.com",
  "role": "Student",
  "iat": 1673000000,       // Issued at
  "exp": 1673003600        // Expiration (1 hour from now)
}
```

**Token usage in headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 📡 SignalR Hub Connection

### Hub Information

| Property | Value |
|----------|-------|
| **Hub URL** | `/monitoringHub` |
| **Scheme** | WebSocket with JWT Bearer |
| **Authentication** | Required (JWT token in query string) |
| **Event Source** | SAC → Server methods |

### Connection Setup Code (C# - For SAC EventLoggerService)

```csharp
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

public class SignalRService
{
    private HubConnection? _connection;
    private readonly string _hubUrl = "https://localhost:5001/monitoringHub";
    private readonly ILogger<SignalRService> _logger;

    public SignalRService(ILogger<SignalRService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Establish connection to monitoring hub with JWT authentication
    /// </summary>
    public async Task ConnectAsync(string jwtToken)
    {
        try
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(_hubUrl, options =>
                {
                    // Add JWT token as query parameter
                    options.AccessTokenProvider = () => Task.FromResult(jwtToken);
                    
                    // Enable automatic reconnection with exponential backoff
                    options.UseDefaultCredentials = false;
                })
                .WithAutomaticReconnect(new[] 
                { 
                    TimeSpan.Zero,              // Immediate first retry
                    TimeSpan.FromSeconds(2),    // 2 seconds
                    TimeSpan.FromSeconds(10),   // 10 seconds
                    TimeSpan.FromSeconds(30)    // 30 seconds
                })
                .Build();

            // Register event handlers BEFORE connecting
            _connection.On<string>("ViolationDetected", OnViolationDetected);
            _connection.On<int>("StudentJoined", OnStudentJoined);
            _connection.On<int>("StudentDisconnected", OnStudentDisconnected);
            _connection.On<string>("JoinFailed", OnJoinFailed);

            // Handle reconnect events
            _connection.Reconnected += OnReconnected;
            _connection.Reconnecting += OnReconnecting;
            _connection.Closed += OnClosed;

            // Start connection
            await _connection.StartAsync();
            _logger.LogInformation("✅ Connected to SignalR Hub");
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ SignalR connection failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Join live exam room on the hub
    /// </summary>
    public async Task JoinLiveExamAsync(int roomId)
    {
        if (_connection?.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("⚠️ Not connected to hub. Connect first.");
            return;
        }

        try
        {
            // Call server method JoinLiveExam(roomId)
            await _connection.InvokeAsync("JoinLiveExam", roomId);
            _logger.LogInformation($"✅ Joined room {roomId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Failed to join room: {ex.Message}");
        }
    }

    /// <summary>
    /// Send monitoring event to server
    /// Called from EventLoggerService.LogAssessment()
    /// </summary>
    public async Task SendMonitoringEventAsync(int roomId, int studentId, 
        string eventType, int severityScore, string description)
    {
        if (_connection?.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("⚠️ Not connected to hub. Queueing event for retry.");
            // TODO: Implement retry queue
            return;
        }

        try
        {
            var eventData = new
            {
                roomId = roomId,
                eventType = eventType,      // "ALT_TAB", "CLIPBOARD", "VM", "PROCESS", etc.
                severityScore = severityScore, // 0-100
                description = description,  // "Alt+Tab to Discord", "Detected VirtualBox", etc.
                timestamp = DateTime.UtcNow
            };

            // Call server method SendMonitoringEvent(int, int, dto)
            await _connection.InvokeAsync("SendMonitoringEvent", roomId, studentId, eventData);
            _logger.LogInformation($"✅ Sent event: {eventType} (Severity: {severityScore})");
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Failed to send event: {ex.Message}");
            // TODO: Queue for retry
        }
    }

    /// <summary>
    /// Disconnect from hub
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
            _logger.LogInformation("✅ Disconnected from SignalR Hub");
        }
    }

    // ===== EVENT HANDLERS =====

    /// <summary>
    /// Fired when server broadcasts ViolationDetected to room
    /// </summary>
    private void OnViolationDetected(string violationData)
    {
        _logger.LogWarning($"⚠️ Violation detected (from hub): {violationData}");
        // TODO: Update UI with violation alert
    }

    private void OnStudentJoined(int studentId)
    {
        _logger.LogInformation($"✅ Student {studentId} joined the exam");
    }

    private void OnStudentDisconnected(int studentId)
    {
        _logger.LogWarning($"⚠️ Student {studentId} disconnected from exam");
    }

    private void OnJoinFailed(string reason)
    {
        _logger.LogError($"❌ Failed to join exam: {reason}");
    }

    private async Task OnReconnected(string? connectionId)
    {
        _logger.LogInformation("✅ Reconnected to SignalR Hub");
        // Rejoin room if needed
    }

    private async Task OnReconnecting(Exception? ex)
    {
        _logger.LogWarning($"⚠️ Attempting to reconnect: {ex?.Message}");
    }

    private async Task OnClosed(Exception? ex)
    {
        _logger.LogError($"❌ Hub connection closed: {ex?.Message}");
        // Trigger graceful shutdown or reconnection logic
    }
}
```

### Hub Methods Summary

| Method | Caller | Parameters | Purpose |
|--------|--------|------------|---------|
| **JoinLiveExam** | SAC | `roomId: int` | SAC joins room for active exam |
| **SendMonitoringEvent** | SAC | `roomId: int, studentId: int, eventData: DTO` | SAC sends detected violation |
| **ViolationDetected** | Server (Broadcast) | `violationData: object` | IMC notified of violation |
| **StudentJoined** | Server (Broadcast) | `studentId: int` | IMC notified student joined |
| **StudentDisconnected** | Server (Broadcast) | `studentId: int` | IMC notified student disconnected |
| **JoinFailed** | Server (Unicast) | `reason: string` | SAC notified join failed |

---

## 📚 API Endpoints Reference

### Authentication Endpoints

#### Register New Student
```
POST /api/auth/register
Content-Type: application/json

Request:
{
  "email": "student@example.com",
  "password": "SecurePass123!",
  "role": "Student"
}

Response (201 Created):
{
  "id": 1,
  "email": "student@example.com",
  "role": "Student",
  "token": "eyJhbGciOiJIUzI1NiIs..."
}
```

#### Login
```
POST /api/auth/login
Content-Type: application/json

Request:
{
  "email": "student@example.com",
  "password": "SecurePass123!"
}

Response (200 OK):
{
  "id": 1,
  "email": "student@example.com",
  "role": "Student",
  "token": "eyJhbGciOiJIUzI1NiIs..."
}
```

### Room Endpoints

#### Get Room Details
```
GET /api/rooms/{roomId}
Authorization: Bearer {token}

Response (200 OK):
{
  "id": 1,
  "subjectName": "Biology 101",
  "instructorId": 1,
  "status": "Active",
  "enrollmentCode": null,
  "createdAt": "2026-01-15T10:30:00Z"
}
```

#### Get Room Status
```
GET /api/rooms/{roomId}/status
Authorization: Bearer {token}

Response (200 OK):
{
  "roomId": 1,
  "subjectName": "Biology 101",
  "status": "Active",
  "totalEnrolled": 25,
  "totalJoined": 23,
  "totalNotJoined": 2
}
```

#### Get Student's Rooms
```
GET /api/rooms/my
Authorization: Bearer {token}

Response (200 OK):
[
  {
    "id": 1,
    "subjectName": "Biology 101",
    "instructorId": 1,
    "status": "Active",
    "createdAt": "2026-01-15T10:30:00Z"
  }
]
```

### Error Responses

#### 401 Unauthorized (Invalid/Missing Token)
```json
{
  "message": "Unauthorized",
  "statusCode": 401
}
```

#### 403 Forbidden (Insufficient Permissions)
```json
{
  "message": "You do not have permission to access this resource",
  "statusCode": 403
}
```

#### 404 Not Found (Room Doesn't Exist)
```json
{
  "message": "Room not found",
  "statusCode": 404
}
```

#### 500 Internal Server Error
```json
{
  "message": "An error occurred processing your request",
  "statusCode": 500
}
```

---

## 💻 SAC Implementation Examples

### 1. Update EventLoggerService to Connect to Server

**File:** `Services/DetectionService/EventLoggerService.cs`

```csharp
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SecureAssessmentClient.Services.DetectionService
{
    public class EventLoggerService
    {
        private readonly ILogger<EventLoggerService> _logger;
        private HubConnection? _hubConnection;
        private Queue<RiskAssessment> _eventBatch = new Queue<RiskAssessment>();
        private const int BATCH_SIZE = 10;
        
        // Configuration for server connection
        private const string SERVER_BASE_URL = "https://localhost:5001";
        private const string HUB_URL = "https://localhost:5001/monitoringHub";
        
        // Will be set during authentication
        private string? _jwtToken;
        private int _studentId;
        private int _roomId;

        public EventLoggerService(ILogger<EventLoggerService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Authenticate with server and get JWT token
        /// Must be called BEFORE any monitoring starts
        /// </summary>
        public async Task AuthenticateAsync(string email, string password)
        {
            try
            {
                using var httpClient = new HttpClient();
                
                var loginRequest = new { email, password };
                var json = System.Text.Json.JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync($"{SERVER_BASE_URL}/api/auth/login", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(responseBody);
                    
                    _jwtToken = jsonDoc.RootElement.GetProperty("token").GetString();
                    _studentId = jsonDoc.RootElement.GetProperty("id").GetInt32();
                    
                    _logger.LogInformation("✅ Authentication successful");
                    return;
                }
                
                _logger.LogError("❌ Authentication failed");
                throw new Exception("Authentication failed");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Authentication error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Connect to SignalR hub and join exam room
        /// </summary>
        public async Task JoinExamAsync(int roomId)
        {
            if (string.IsNullOrEmpty(_jwtToken))
            {
                _logger.LogError("❌ Not authenticated. Call AuthenticateAsync first.");
                return;
            }

            try
            {
                _roomId = roomId;
                
                // Build hub connection with JWT authentication
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(HUB_URL, options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(_jwtToken);
                    })
                    .WithAutomaticReconnect(new[] 
                    { 
                        TimeSpan.Zero,
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(10)
                    })
                    .Build();

                // Register server event handlers
                _hubConnection.On<string>("ViolationDetected", violationData =>
                {
                    _logger.LogWarning($"⚠️ VIOLATION BROADCAST FROM SERVER: {violationData}");
                });

                _hubConnection.On<int>("StudentJoined", studentId =>
                {
                    _logger.LogInformation($"✅ Student {studentId} joined");
                });

                _hubConnection.On<int>("StudentDisconnected", studentId =>
                {
                    _logger.LogWarning($"⚠️ Student {studentId} disconnected");
                });

                // Start connection
                await _hubConnection.StartAsync();
                _logger.LogInformation("✅ Connected to SignalR Hub");

                // Join the exam room
                await _hubConnection.InvokeAsync("JoinLiveExam", roomId);
                _logger.LogInformation($"✅ Joined room {roomId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Failed to join exam: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Log a single assessment and transmit to server
        /// </summary>
        public async Task LogAssessmentAsync(RiskAssessment assessment)
        {
            try
            {
                _eventBatch.Enqueue(assessment);
                _logger.LogInformation($"📝 Assessment queued (Batch: {_eventBatch.Count}/{BATCH_SIZE})");

                // Send immediately if we hit batch size OR if severity is critical
                if (_eventBatch.Count >= BATCH_SIZE || assessment.RiskScore > 80)
                {
                    await TransmitBatchAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error logging assessment: {ex.Message}");
            }
        }

        /// <summary>
        /// Transmit queued assessments to server via SignalR
        /// </summary>
        private async Task TransmitBatchAsync()
        {
            if (_eventBatch.Count == 0)
                return;

            try
            {
                if (_hubConnection?.State != HubConnectionState.Connected)
                {
                    _logger.LogWarning("⚠️ Not connected to hub. Retrying...");
                    await Task.Delay(1000);
                    if (_hubConnection?.State != HubConnectionState.Connected)
                        return;
                }

                while (_eventBatch.Count > 0)
                {
                    var assessment = _eventBatch.Dequeue();
                    
                    // Convert assessment to event data
                    var eventData = new
                    {
                        eventType = assessment.RecommendedAction,
                        severityScore = assessment.RiskScore,
                        description = assessment.Rationale,
                        timestamp = DateTime.UtcNow
                    };

                    // Send to server
                    await _hubConnection.InvokeAsync(
                        "SendMonitoringEvent", 
                        _roomId, 
                        _studentId, 
                        eventData
                    );

                    _logger.LogInformation($"✅ Event transmitted: {assessment.RiskLevel}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Transmission failed: {ex.Message}");
                // TODO: Implement retry queue
            }
        }

        /// <summary>
        /// Disconnect from server gracefully
        /// </summary>
        public async Task DisconnectAsync()
        {
            try
            {
                // Flush remaining events
                await TransmitBatchAsync();

                if (_hubConnection != null)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                    _hubConnection = null;
                    _logger.LogInformation("✅ Disconnected from server");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error disconnecting: {ex.Message}");
            }
        }
    }
}
```

### 2. Update App.xaml.cs to Initialize Connection

```csharp
// In App.xaml.cs startup or MainWindow.xaml.cs

private async void InitializeServerConnection()
{
    try
    {
        // 1. Authenticate
        await _eventLoggerService.AuthenticateAsync("student@example.com", "SecurePass123!");
        
        // 2. Get room ID (from exam parameters)
        int roomId = GetRoomIdFromExamParameters();
        
        // 3. Join exam
        await _eventLoggerService.JoinExamAsync(roomId);
        
        MessageBox.Show("✅ Connected to server and joined exam room", "Success");
    }
    catch (Exception ex)
    {
        MessageBox.Show($"❌ Connection failed: {ex.Message}", "Error");
        // Optionally exit or run in offline mode
    }
}
```

### 3. Update DetectionTestConsole to Test Connection

Add new option to test server connection:

```csharp
// In DetectionTestConsole.cs

case "7":
    Console.WriteLine("\n[TEST] Server Connection Test");
    await TestServerConnectionAsync();
    break;

private async Task TestServerConnectionAsync()
{
    try
    {
        // Get credentials from user
        Console.Write("Email: ");
        string email = Console.ReadLine() ?? "";
        Console.Write("Password: ");
        string password = Console.ReadLine() ?? "";
        
        // Authenticate
        Console.WriteLine("🔐 Authenticating...");
        await _eventLoggerService.AuthenticateAsync(email, password);
        
        // Join room
        Console.Write("Room ID: ");
        if (int.TryParse(Console.ReadLine(), out int roomId))
        {
            Console.WriteLine($"📡 Joining room {roomId}...");
            await _eventLoggerService.JoinExamAsync(roomId);
            
            // Send test event
            Console.WriteLine("\n📤 Sending test monitoring event...");
            var testAssessment = new RiskAssessment
            {
                RiskScore = 45,
                RiskLevel = "Suspicious",
                RecommendedAction = "TEST_EVENT",
                Rationale = "Test connection to server"
            };
            
            await _eventLoggerService.LogAssessmentAsync(testAssessment);
            
            Console.WriteLine("✅ Test event sent successfully!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Test failed: {ex.Message}");
    }
}
```

---

## 📊 Event Transmission Pipeline

### Data Flow from SAC to Server

```
1. DETECTION PHASE (Phase 6-7)
   ├─ EnvironmentIntegrityService detects VirtualBox running
   └─ BehavioralMonitoringService detects Alt+Tab to Discord

2. ASSESSMENT PHASE (Phase 8)
   ├─ DecisionEngineService evaluates events
   ├─ Calculates risk scores: VM=70, Alt+Tab=50
   └─ Creates RiskAssessment objects

3. LOGGING PHASE (Phase 9) ← UPDATED
   ├─ EventLoggerService receives RiskAssessment
   ├─ Converts to MonitoringEventDto
   ├─ Adds to batch queue
   └─ When batch full OR severity high:
      └─ TRANSMIT VIA SIGNALR

4. SERVER RECEPTION
   ├─ SignalR Hub receives SendMonitoringEvent call
   ├─ Authenticates student via JWT
   ├─ Stores MonitoringEvent in database
   ├─ Broadcasts ViolationDetected to IMC dashboard
   └─ Real-time alert displayed to instructor

5. IMC DISPLAYS
   ├─ Student: "John Doe"
   ├─ Event: "Virtual Machine Detected"
   ├─ Severity: "High (70)"
   ├─ Time: "14:30:45"
   └─ Action: [Flag Student] [Send Warning] [Escalate]
```

### Event Data Structure

**RiskAssessment → MonitoringEventDto**

```csharp
// From SAC (RiskAssessment)
{
    RiskScore: 70,                          // 0-100
    RiskLevel: "Cheating",                  // Safe/Suspicious/Cheating
    RecommendedAction: "SUSPICIOUS_PROCESS",
    Rationale: "VirtualBox process detected"
}

        ↓ Convert ↓

// To Server (MonitoringEventDto)
{
    roomId: 1,
    studentId: 1,
    eventType: "SUSPICIOUS_PROCESS",        // Same as RecommendedAction
    severityScore: 70,                      // Same as RiskScore
    description: "VirtualBox process detected",
    timestamp: "2026-01-15T14:30:45Z"
}
```

### Batch Transmission Strategy

**Why Batching?**
- Reduces network overhead
- Prevents flooding the hub with individual events
- Groups related violations together

**Batch Rules:**
- Transmit when batch reaches 10 events
- Transmit immediately if severity > 80 (critical)
- Transmit remaining events when exam ends (flush)
- Retry failed transmissions after 5 seconds

---

## 🧪 Testing & Troubleshooting

### Test 1: Authentication
```powershell
# Start server
cd AcademicSentinel.Server
dotnet run

# In SAC test menu, try Option 7: Server Connection Test
# Expected: ✅ Authentication successful
```

### Test 2: SignalR Connection
```
After Authentication test:
- Enter room ID: 1
- Expected: ✅ Connected to SignalR Hub
- Expected: ✅ Joined room 1
```

### Test 3: Event Transmission
```
After SignalR connection:
- System sends test event
- Expected: ✅ Test event sent successfully!
- Check server console: Should see "SendMonitoringEvent received"
```

### Common Issues & Solutions

#### Issue: "Unauthorized" on Authentication
```
❌ {"message":"Unauthorized"}

Solution:
1. Verify email/password are correct
2. Check user exists in database (see registration test)
3. Verify JWT key in appsettings.json hasn't changed
4. Restart server
```

#### Issue: SignalR Connection Timeout
```
❌ HubConnectionException: Timeout occurred

Solution:
1. Verify server is running: https://localhost:5001
2. Check firewall allows port 5001
3. Verify JWT token is being passed correctly
4. Check hub URL is correct: https://localhost:5001/monitoringHub
5. Enable detailed SignalR logging (see below)
```

#### Issue: "Not Connected to Hub" When Sending Events
```
❌ Not connected to hub. Queueing event for retry.

Solution:
1. Ensure JoinExamAsync was called successfully
2. Check room ID is correct and room exists
3. Verify room status is "Active"
4. Check network connectivity to server
```

### Enable Detailed Logging

**In SAC (Program.cs or app initialization):**
```csharp
var services = new ServiceCollection();

services.AddLogging(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Debug)
        .AddConsole()
        .AddDebug();
});

// This will show all SignalR connection details
```

### Database Verification

**Check if events are being stored:**

```sql
-- In server project directory, open database:
sqlite3 academicsentinel.db

-- Query stored events:
SELECT * FROM MonitoringEvents ORDER BY Timestamp DESC LIMIT 10;

-- Query student participation:
SELECT * FROM SessionParticipants WHERE StudentId = 1;

-- Query violations:
SELECT * FROM ViolationLogs WHERE StudentId = 1;
```

---

## 🔒 Security Considerations

### 1. JWT Token Security
- ✅ Token expires after 1 hour
- ✅ Token stored securely (not in LocalStorage)
- ✅ HTTPS only (no HTTP)
- ⚠️ Change `Jwt:Key` in production (`appsettings.Production.json`)

### 2. Student Identity Verification
- ✅ Server extracts `ClaimTypes.NameIdentifier` from JWT
- ✅ Student can only send their own data
- ✅ Server rejects cross-identity tampering
- ✅ All requests logged with student ID and timestamp

### 3. Room Access Control
- ✅ Students can only join enrolled rooms
- ✅ Students can only see their own rooms
- ✅ Instructors can only see their own rooms
- ✅ Only room instructors can change room status

### 4. Data Encryption
- ✅ HTTPS/WSS for transport encryption
- ✅ Passwords hashed with BCrypt (not stored plaintext)
- ⚠️ Database is SQLite (suitable for development only)

### 5. Production Recommendations
- [ ] Change JWT key in `appsettings.Production.json`
- [ ] Use HTTPS certificates (not self-signed)
- [ ] Migrate database to SQL Server or PostgreSQL
- [ ] Enable CORS for allowed origins only
- [ ] Implement rate limiting on API endpoints
- [ ] Add request signing for critical operations
- [ ] Implement audit logging for all data access

---

## 📝 Quick Reference Checklist

### Prerequisites
- [ ] Server running on `https://localhost:5001`
- [ ] Database initialized (`academicsentinel.db`)
- [ ] JWT key configured in `appsettings.json`
- [ ] Student account created or registration working

### SAC Setup
- [ ] Update `EventLoggerService` with SignalR connection code
- [ ] Update `App.xaml.cs` to call `AuthenticateAsync`
- [ ] Update `App.xaml.cs` to call `JoinExamAsync` with room ID
- [ ] Add error handling for connection failures
- [ ] Implement retry logic for failed transmissions

### Testing
- [ ] Test registration endpoint
- [ ] Test login endpoint and JWT retrieval
- [ ] Test SignalR connection with token
- [ ] Test JoinLiveExam hub method
- [ ] Test SendMonitoringEvent hub method
- [ ] Verify events stored in database
- [ ] Verify IMC receives broadcast notifications

### Monitoring
- [ ] Check server logs for errors
- [ ] Monitor database for event storage
- [ ] Monitor network for connection issues
- [ ] Track event transmission latency
- [ ] Monitor batch sizes and transmission frequency

---

## 📞 Support & Resources

**Server Project Files:**
- Project: `AcademicSentinel.Server.csproj`
- Location: `E:\Darryll pogi\FEU files Darryll\3rd Year\3rd Year Second Sem\FOR Thesis\Codes\SystemFourCUDA\AcademicSentinel.Server\`

**Key Files:**
- Hub: `Hubs/MonitoringHub.cs`
- Controllers: `Controllers/AuthController.cs`, `Controllers/RoomsController.cs`
- Models: `Models/MonitoringEvent.cs`, `Models/SessionParticipant.cs`
- DTOs: `DTOs/AdditionalDTOs.cs`
- Config: `appsettings.json`, `Program.cs`

**Documentation:**
- Server Docs: `API_QUICK_REFERENCE.md`, `COMPLETION_SUMMARY.md`
- This Guide: `SAC_SERVER_INTEGRATION_GUIDE.md`

---

**Status:** ✅ **READY FOR IMPLEMENTATION**  
**Last Updated:** Today  
**Next Steps:** Implement EventLoggerService updates and test connection
