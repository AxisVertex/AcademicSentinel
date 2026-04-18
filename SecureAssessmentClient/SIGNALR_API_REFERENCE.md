# 🚀 SignalR & API Endpoints Quick Reference

**For:** Secure Assessment Client (SAC) Integration  
**Server:** AcademicSentinel.Server  
**Framework:** .NET 10 + SignalR

---

## 🌐 SignalR Hub Endpoints

### Hub Location
```
URL: https://localhost:5001/monitoringHub
Protocol: WebSocket with JWT Bearer Authentication
Auth: Token passed via query parameter: ?access_token={token}
```

### SAC → Server Methods (Invocations)

#### 1. JoinLiveExam
**Called when:** Student enters exam room (Phase 9 start)  
**Purpose:** Register student for real-time monitoring in a room

```csharp
// Signature on Hub:
public async Task JoinLiveExam(int roomId)

// Call from SAC:
await _hubConnection.InvokeAsync("JoinLiveExam", roomId);

// Parameters:
- roomId (int): ID of exam room student is joining

// Server Returns:
- (void) or throws exception

// Server Side Effects:
1. Verifies room exists and status is "Active"
2. Adds connection to SignalR group: "{roomId}"
3. Creates/updates SessionParticipant record
4. Broadcasts "StudentJoined" to all in room
5. If room not active: Broadcasts "JoinFailed" to caller

// Success Response from Server Broadcast:
- Clients.Group(roomId.ToString()).SendAsync("StudentJoined", studentId)
```

**Example Usage:**
```csharp
try
{
    await _hubConnection.InvokeAsync("JoinLiveExam", 1);
    Console.WriteLine("✅ Successfully joined room 1");
}
catch (HubException ex)
{
    Console.WriteLine($"❌ Failed to join: {ex.Message}");
}
```

---

#### 2. SendMonitoringEvent
**Called when:** SAC detects violation (Phase 9 main loop)  
**Purpose:** Transmit real-time monitoring events to server

```csharp
// Signature on Hub:
public async Task SendMonitoringEvent(int roomId, int studentId, MonitoringEventDto eventData)

// Call from SAC:
await _hubConnection.InvokeAsync("SendMonitoringEvent", roomId, studentId, eventData);

// Parameters:
- roomId (int): Room ID where violation occurred
- studentId (int): Student ID (from JWT claim)
- eventData (MonitoringEventDto): Event details

// MonitoringEventDto Structure:
{
    "eventType": "ALT_TAB",           // String: Event category
    "severityScore": 50,              // Int 0-100: Risk level
    "description": "Switched to Discord",  // String: Details
    "timestamp": "2026-01-15T14:30:45Z"    // DateTime: UTC
}

// Server Returns:
- (void) or throws exception

// Server Side Effects:
1. Validates studentId matches JWT claim
2. Creates MonitoringEvent record in database
3. Broadcasts "ViolationDetected" to all in room
4. IMC receives real-time alert

// Success Response from Server Broadcast:
- Clients.Group(roomId.ToString()).SendAsync("ViolationDetected", 
  new { 
    studentId, 
    eventType, 
    severityScore, 
    timestamp 
  })
```

**Example Usage:**
```csharp
var eventData = new
{
    eventType = "SUSPICIOUS_PROCESS",
    severityScore = 70,
    description = "VirtualBox detected running",
    timestamp = DateTime.UtcNow
};

await _hubConnection.InvokeAsync("SendMonitoringEvent", roomId, studentId, eventData);
```

---

### Server → SAC Broadcasts (Receive)

#### 1. ViolationDetected
**Broadcast by:** Server when violation stored  
**Received by:** All clients in room group  
**Purpose:** Alert IMC dashboard of new violation

```csharp
// Register handler BEFORE connecting:
_hubConnection.On<dynamic>("ViolationDetected", (violationData) =>
{
    // violationData contains:
    // - studentId (int)
    // - eventType (string)
    // - severityScore (int)
    // - timestamp (DateTime)
    
    Console.WriteLine($"Violation: {violationData.eventType} Score:{violationData.severityScore}");
});

// Data Format Received:
{
    "studentId": 1,
    "eventType": "ALT_TAB",
    "severityScore": 45,
    "timestamp": "2026-01-15T14:30:45Z"
}
```

---

#### 2. StudentJoined
**Broadcast by:** Server when student joins exam  
**Received by:** All clients in room group  
**Purpose:** Notify IMC that student is now live

```csharp
_hubConnection.On<int>("StudentJoined", (studentId) =>
{
    Console.WriteLine($"✅ Student {studentId} is now live in exam");
});
```

---

#### 3. StudentDisconnected
**Broadcast by:** Server when student loses connection  
**Received by:** All clients in room group  
**Purpose:** Alert IMC of unexpected disconnection

```csharp
_hubConnection.On<int>("StudentDisconnected", (studentId) =>
{
    Console.WriteLine($"⚠️ Student {studentId} disconnected from exam");
});
```

---

#### 4. JoinFailed
**Broadcast by:** Server when JoinLiveExam fails  
**Received by:** Only the calling client (Unicast)  
**Purpose:** Inform student why join failed

```csharp
_hubConnection.On<string>("JoinFailed", (reason) =>
{
    Console.WriteLine($"❌ Cannot join exam: {reason}");
    // Typical reasons:
    // - "Cannot join room: the instructor has not started the session or has ended it."
});
```

---

## 🔐 REST API Endpoints

### Base URL
```
https://localhost:5001/api
```

### Authentication Endpoints

#### POST /auth/register
**Purpose:** Create new student or instructor account  
**Auth Required:** No

```
POST https://localhost:5001/api/auth/register
Content-Type: application/json

Request Body:
{
  "email": "student@example.com",
  "password": "SecurePass123!",
  "role": "Student"  // or "Instructor"
}

Response (201 Created):
{
  "id": 1,
  "email": "student@example.com",
  "role": "Student",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}

Errors:
- 400: Invalid email/password format
- 409: Email already registered
```

**SAC Usage:**
```csharp
using (var client = new HttpClient())
{
    var registerDto = new { email = "student@example.com", password = "SecurePass123!", role = "Student" };
    var json = JsonSerializer.Serialize(registerDto);
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    
    var response = await client.PostAsync("https://localhost:5001/api/auth/register", content);
    if (response.IsSuccessStatusCode)
    {
        var responseJson = await response.Content.ReadAsStringAsync();
        var user = JsonDocument.Parse(responseJson);
        string token = user.RootElement.GetProperty("token").GetString();
        Console.WriteLine("✅ Registered and got token");
    }
}
```

---

#### POST /auth/login
**Purpose:** Authenticate and get JWT token  
**Auth Required:** No

```
POST https://localhost:5001/api/auth/login
Content-Type: application/json

Request Body:
{
  "email": "student@example.com",
  "password": "SecurePass123!"
}

Response (200 OK):
{
  "id": 1,
  "email": "student@example.com",
  "role": "Student",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}

Errors:
- 400: Invalid credentials
- 404: User not found
- 401: Password incorrect
```

**SAC Usage:**
```csharp
public async Task<string> LoginAsync(string email, string password)
{
    using (var client = new HttpClient())
    {
        var loginDto = new { email, password };
        var json = JsonSerializer.Serialize(loginDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await client.PostAsync("https://localhost:5001/api/auth/login", content);
        
        if (response.IsSuccessStatusCode)
        {
            var responseJson = await response.Content.ReadAsStringAsync();
            var user = JsonDocument.Parse(responseJson);
            return user.RootElement.GetProperty("token").GetString();
        }
        
        throw new Exception("Login failed");
    }
}
```

---

### Room Endpoints

#### GET /rooms/my
**Purpose:** Get all rooms student is enrolled in  
**Auth Required:** Yes (Bearer token)

```
GET https://localhost:5001/api/rooms/my
Authorization: Bearer {token}

Response (200 OK):
[
  {
    "id": 1,
    "subjectName": "Biology 101",
    "instructorId": 1,
    "status": "Active",
    "enrollmentCode": null,
    "createdAt": "2026-01-15T10:30:00Z"
  },
  {
    "id": 2,
    "subjectName": "Chemistry 201",
    "instructorId": 2,
    "status": "Countdown",
    "enrollmentCode": null,
    "createdAt": "2026-01-15T14:00:00Z"
  }
]

Errors:
- 401: Missing or invalid token
- 403: Not a student
```

**SAC Usage:**
```csharp
using (var client = new HttpClient())
{
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    
    var response = await client.GetAsync("https://localhost:5001/api/rooms/my");
    var json = await response.Content.ReadAsStringAsync();
    var rooms = JsonDocument.Parse(json);
    
    foreach (var room in rooms.RootElement.EnumerateArray())
    {
        int roomId = room.GetProperty("id").GetInt32();
        string subject = room.GetProperty("subjectName").GetString();
        Console.WriteLine($"Room {roomId}: {subject}");
    }
}
```

---

#### GET /rooms/{roomId}
**Purpose:** Get specific room details  
**Auth Required:** Yes (Bearer token)

```
GET https://localhost:5001/api/rooms/1
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

Errors:
- 401: Missing or invalid token
- 404: Room not found
```

---

#### GET /rooms/{roomId}/status
**Purpose:** Get room participant status  
**Auth Required:** Yes (Bearer token)

```
GET https://localhost:5001/api/rooms/1/status
Authorization: Bearer {token}

Response (200 OK):
{
  "roomId": 1,
  "subjectName": "Biology 101",
  "status": "Active",
  "createdAt": "2026-01-15T10:30:00Z",
  "totalEnrolled": 25,
  "totalJoined": 23,
  "totalNotJoined": 2
}

// totalJoined: Students currently connected
// totalNotJoined: Students enrolled but not yet joined
```

---

## 🔄 Complete Authentication → Connection Flow

### Step 1: Register or Login (REST)
```csharp
// Get JWT token
var token = await LoginAsync("student@example.com", "SecurePass123!");
// Returns: eyJhbGciOiJIUzI1NiIs...
```

### Step 2: Get Enrolled Rooms (REST)
```csharp
// Fetch available rooms
var rooms = await GetMyRoomsAsync(token);
// Returns: List of Room objects
int roomId = rooms[0].Id;  // e.g., 1
```

### Step 3: Connect to SignalR Hub
```csharp
// Build connection with JWT in query string
_hubConnection = new HubConnectionBuilder()
    .WithUrl("https://localhost:5001/monitoringHub", options =>
    {
        options.AccessTokenProvider = () => Task.FromResult(token);
    })
    .Build();

// Register event handlers
_hubConnection.On<dynamic>("ViolationDetected", OnViolationDetected);

// Start connection
await _hubConnection.StartAsync();
```

### Step 4: Join Exam Room (SignalR)
```csharp
// Join the specific room
await _hubConnection.InvokeAsync("JoinLiveExam", roomId);
// Server broadcasts: StudentJoined to all in room
```

### Step 5: Send Monitoring Events (SignalR)
```csharp
// When violation detected (Phase 8 outputs RiskAssessment)
var eventData = new
{
    eventType = "SUSPICIOUS_PROCESS",
    severityScore = 80,
    description = "VirtualBox detected",
    timestamp = DateTime.UtcNow
};

await _hubConnection.InvokeAsync("SendMonitoringEvent", roomId, studentId, eventData);
// Server: Stores in database
// Server: Broadcasts: ViolationDetected to IMC
```

### Step 6: Handle Disconnect
```csharp
// When done or exam ends:
await _hubConnection.StopAsync();
await _hubConnection.DisposeAsync();
// Server automatically triggers: StudentDisconnected broadcast
```

---

## 📊 Endpoint Summary Table

| Type | Method | Endpoint | Auth | Purpose |
|------|--------|----------|------|---------|
| REST | POST | /auth/register | No | Register account |
| REST | POST | /auth/login | No | Login & get token |
| REST | GET | /rooms/my | Yes | List student's rooms |
| REST | GET | /rooms/{id} | Yes | Get room details |
| REST | GET | /rooms/{id}/status | Yes | Get room status |
| SignalR | Invoke | JoinLiveExam | Yes | Join room for exam |
| SignalR | Invoke | SendMonitoringEvent | Yes | Send violation event |
| SignalR | On | ViolationDetected | - | Receive violation broadcast |
| SignalR | On | StudentJoined | - | Receive join notification |
| SignalR | On | StudentDisconnected | - | Receive disconnect notification |
| SignalR | On | JoinFailed | - | Receive join failure reason |

---

## 🛠️ Error Codes Reference

| Code | Meaning | Example Response |
|------|---------|------------------|
| **200** | OK | Request succeeded |
| **201** | Created | Resource created |
| **400** | Bad Request | Invalid JSON, missing fields |
| **401** | Unauthorized | Missing/invalid JWT token |
| **403** | Forbidden | Not authorized for this action |
| **404** | Not Found | Room/User doesn't exist |
| **409** | Conflict | Email already registered |
| **500** | Server Error | Database error, uncaught exception |

**Example Error Response:**
```json
{
  "message": "Invalid credentials",
  "statusCode": 401
}
```

---

## 🔐 JWT Token Format

**Header:**
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

**Payload:**
```json
{
  "nameid": "1",
  "email": "student@example.com",
  "role": "Student",
  "iat": 1673000000,
  "exp": 1673003600
}
```

**Usage:**
```
Authorization: Bearer {full_token_string}
```

**Token Expiration:** 1 hour from login  
**Action on Expiration:** Must login again to get new token

---

## 📱 SAC Integration Checklist

- [ ] EventLoggerService implements SignalR connection
- [ ] AuthenticateAsync(email, password) method works
- [ ] JoinExamAsync(roomId) method works
- [ ] SendMonitoringEventAsync() transmits events
- [ ] DisconnectAsync() cleans up connection
- [ ] All SignalR event handlers registered
- [ ] Error handling for all network calls
- [ ] Retry logic for failed transmissions
- [ ] Logging of all important events
- [ ] Batch queue management (transmit logic)

---

**Last Updated:** Today  
**Ready for:** SAC Implementation  
**Reference:** SAC_SERVER_INTEGRATION_GUIDE.md for detailed explanations
