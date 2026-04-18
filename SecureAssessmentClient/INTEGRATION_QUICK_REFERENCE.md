# 🎯 SAC-Server Integration - One-Page Reference Card

**Print this or keep it handy while implementing!**

---

## 🔗 Connection Architecture

```
SAC Client                                    AcademicSentinel.Server
├─ Phase 6: Detection                         ├─ AuthController
├─ Phase 7: Monitoring                        ├─ MonitoringHub
├─ Phase 8: Decision Engine            ────→ ├─ AppDbContext
└─ Phase 9: Event Logger (UPDATED)           └─ Broadcasts to IMC
```

---

## 🔐 Authentication Flow

```
1. POST /api/auth/login
   Request:  {"email": "...", "password": "..."}
   Response: {"id": 1, "token": "eyJ..."}

2. Store token securely

3. Connect to SignalR with token:
   ?access_token={token}

4. Call hub methods with authenticated connection
```

---

## 📡 SignalR Hub Methods

### SAC Calls (→ Server)
```csharp
// Join exam room
await hub.InvokeAsync("JoinLiveExam", roomId);

// Send violation event
await hub.InvokeAsync("SendMonitoringEvent", 
    roomId, studentId, eventData);
```

### SAC Receives (← Server)
```csharp
// Register BEFORE connecting:
hub.On<dynamic>("ViolationDetected", data => {...});
hub.On<int>("StudentJoined", id => {...});
hub.On<int>("StudentDisconnected", id => {...});
hub.On<string>("JoinFailed", reason => {...});
```

---

## 🔄 Event Transformation

**SAC RiskAssessment** → **Server MonitoringEventDto**

```
RiskAssessment          →    MonitoringEventDto
├─ RiskScore: 75        →    severityScore: 75
├─ RiskLevel: "..."     →    (informational only)
├─ RecommendedAction    →    eventType
└─ Rationale            →    description
```

**Example:**
```json
// SAC Output (Phase 8)
{
  "RiskScore": 70,
  "RiskLevel": "Cheating",
  "RecommendedAction": "ENVIRONMENT_VIOLATION",
  "Rationale": "VirtualBox detected"
}

        ↓ Convert ↓

// SAC→Server Transmission
{
  "eventType": "VM_DETECTED",
  "severityScore": 70,
  "description": "VirtualBox process detected",
  "timestamp": "2026-01-15T14:30:45Z"
}

        ↓ Store ↓

// Server Database
MonitoringEvents Table:
EventType: "VM_DETECTED"
SeverityScore: 70
StudentId: 1
RoomId: 1
Timestamp: "2026-01-15T14:30:45Z"
```

---

## 📋 Event Type Classifications

| SAC Detection | Event Type | Severity | DB Storage |
|--------------|-----------|----------|-----------|
| VirtualBox running | `VM_DETECTED` | 85-95 | EventType field |
| WinDbg active | `DEBUGGER_DETECTED` | 90-95 | EventType field |
| Alt+Tab to Discord | `WINDOW_SWITCH_SUSPICIOUS` | 50-75 | EventType field |
| Clipboard from Chrome | `CLIPBOARD_TEXT_COPY` | 20-30 | EventType field |
| 65+ seconds idle | `IDLE_PERIOD` | 15 | EventType field |
| Discord running | `BLACKLISTED_PROCESS_RUNNING` | 75-85 | EventType field |

---

## 🔌 SignalR Connection Setup

```csharp
// 1. Create connection
var hub = new HubConnectionBuilder()
    .WithUrl("https://localhost:5001/monitoringHub", options =>
    {
        options.AccessTokenProvider = () => Task.FromResult(jwtToken);
    })
    .WithAutomaticReconnect()
    .Build();

// 2. Register handlers BEFORE connecting
hub.On<dynamic>("ViolationDetected", OnViolation);

// 3. Start connection
await hub.StartAsync();

// 4. Join room
await hub.InvokeAsync("JoinLiveExam", roomId);

// 5. Send events as detected
await hub.InvokeAsync("SendMonitoringEvent", 
    roomId, studentId, eventDto);
```

---

## 🧪 Quick Test Commands

### Test Authentication
```powershell
# Register
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Pass123!","role":"Student"}' \
  -k

# Login (get token)
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Pass123!"}' \
  -k
```

### Test Room Access
```powershell
# Get student's rooms
curl -X GET https://localhost:5001/api/rooms/my \
  -H "Authorization: Bearer {token}" \
  -k
```

### Test SignalR (via SAC)
```
1. Run SAC test mode
2. Select Option 7 (Server Connection Test)
3. Enter credentials
4. Expected: Connected + Event transmitted
```

---

## 💾 Database Queries

### View stored events
```sql
sqlite3 academicsentinel.db
SELECT * FROM MonitoringEvents ORDER BY Timestamp DESC LIMIT 10;
```

### View student participation
```sql
SELECT * FROM SessionParticipants WHERE StudentId = 1;
```

### Count events by type
```sql
SELECT EventType, COUNT(*) as Count 
FROM MonitoringEvents 
GROUP BY EventType;
```

### Get average severity by type
```sql
SELECT EventType, AVG(SeverityScore) as AvgSeverity
FROM MonitoringEvents
GROUP BY EventType
ORDER BY AvgSeverity DESC;
```

---

## 🛠️ Implementation Checklist

- [ ] Update EventLoggerService.cs (authentication)
- [ ] Update EventLoggerService.cs (SignalR connection)
- [ ] Update EventLoggerService.cs (JoinExamAsync)
- [ ] Update EventLoggerService.cs (SendMonitoringEventAsync)
- [ ] Update App.xaml.cs (initialize connection)
- [ ] Map RiskAssessment → MonitoringEventDto
- [ ] Implement batch queue
- [ ] Implement error handling
- [ ] Implement retry logic
- [ ] Add logging for debugging
- [ ] Test all components
- [ ] Verify database storage

---

## ⚡ Key Code Snippets

### Map RiskAssessment to EventDto
```csharp
var assessment = decisionEngine.AssessEvent(detection);
var eventDto = new
{
    eventType = assessment.RecommendedAction,
    severityScore = assessment.RiskScore,
    description = assessment.Rationale,
    timestamp = DateTime.UtcNow
};
await hub.InvokeAsync("SendMonitoringEvent", roomId, studentId, eventDto);
```

### Handle Received Events
```csharp
hub.On<dynamic>("ViolationDetected", (violation) =>
{
    Console.WriteLine($"ALERT: {violation.eventType} Score:{violation.severityScore}");
    // Update UI or take action
});
```

### Error Handling
```csharp
try
{
    await hub.InvokeAsync("SendMonitoringEvent", roomId, studentId, eventDto);
}
catch (Exception ex)
{
    logger.LogError($"Failed to send event: {ex.Message}");
    // Queue for retry
}
```

---

## 📊 Server Configuration

**File:** `appsettings.json`

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

**Important Values for SAC:**
- Base URL: `https://localhost:5001`
- Hub URL: `https://localhost:5001/monitoringHub`
- JWT Key: (Copy from appsettings.json)
- Issuer: `AcademicSentinelServer`
- Audience: `AcademicSentinelClients`

---

## 🚨 Troubleshooting Quick Fix

| Error | Solution |
|-------|----------|
| "Connection refused" | Start server: `dotnet run` |
| "Unauthorized" | Check JWT token, re-login |
| "Hub method not found" | Verify method name matches exactly |
| "SignalR timeout" | Check server port 5001 is open |
| "Token expired" | Re-authenticate, get new token |
| "Events not storing" | Check JWT authentication, verify studentId |

---

## 📁 File Locations

**SAC Updates:**
```
SecureAssessmentClient/Services/DetectionService/EventLoggerService.cs
SecureAssessmentClient/App.xaml.cs
```

**Server Files (Reference):**
```
AcademicSentinel.Server/Hubs/MonitoringHub.cs
AcademicSentinel.Server/Controllers/AuthController.cs
AcademicSentinel.Server/Models/MonitoringEvent.cs
AcademicSentinel.Server/appsettings.json
```

---

## 🎓 For Your Thesis

**What to demonstrate:**
1. Run SAC and server simultaneously
2. Show real detection (Alt+Tab, etc.)
3. Query database to show stored events
4. Show timestamps and event types
5. Document complete event pipeline

**Data to collect:**
- Total events transmitted: ___
- Event types captured: ___
- Average response time: ___ ms
- Database storage size: ___ KB
- Detection accuracy: ___ %

---

## ✅ Integration Success Indicators

You'll know integration is working when:

✅ Can authenticate and get JWT token  
✅ SignalR connects without errors  
✅ JoinLiveExam executes successfully  
✅ SendMonitoringEvent receives no exceptions  
✅ Events appear in database immediately  
✅ Server broadcasts received by client  
✅ Multiple events can be sent rapidly  
✅ Reconnection works automatically  
✅ No data loss during transmission  
✅ Timestamps are accurate (UTC+8)  

---

## 🔗 Related Documentation

- **Full Guide:** SAC_SERVER_INTEGRATION_GUIDE.md
- **API Reference:** SIGNALR_API_REFERENCE.md
- **Event Mapping:** EVENT_TYPE_MAPPING.md
- **Complete Status:** SAC_SERVER_INTEGRATION_COMPLETE.md

---

**Print or bookmark this page for quick reference!**

**Last Updated:** Today  
**Status:** ✅ READY FOR IMPLEMENTATION
