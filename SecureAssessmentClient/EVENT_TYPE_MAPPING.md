# 📡 Event Type Mapping: SAC Detection → Server Storage

**Purpose:** Define how SAC detections map to server event types and database storage  
**Use Case:** Ensure consistent event classification across client-server communication

---

## 📋 Event Type Classification

### Phase 6: Environment Integrity (High Severity Events)

#### Virtual Machine / Emulator Detection
**SAC Detection:** `EnvironmentIntegrityService.CheckVirtualizationArtifacts()`  
**Detection Output:** `(IsVirtual: true, Details: ["VirtualBox detected", "BlueStacks HD-Player running", ...])`

**Map to Server:**
```csharp
// SAC RiskAssessment
{
    RiskScore: 85,
    RiskLevel: "Cheating",
    RecommendedAction: "ENVIRONMENT_VIOLATION",  // Map to eventType
    Rationale: "VirtualBox process detected (vm-related process)"
}

// SAC → Server MonitoringEventDto
{
    eventType: "VM_DETECTED",                    // Classification
    severityScore: 85,                           // Risk score
    description: "VirtualBox process detected",  // Details
    timestamp: DateTime.UtcNow
}

// Server Database Storage
{
    EventType: "VM_DETECTED",
    SeverityScore: 85,
    Description: "VirtualBox process detected",
    Timestamp: "2026-01-15T14:30:45Z",
    RoomId: 1,
    StudentId: 1
}
```

**Process Detection Matrix:**

| SAC Detection | Detected Process | Event Type | Severity | Example |
|---------------|------------------|-----------|----------|---------|
| VM Check | VirtualBox, VMware, QEMU | `VM_DETECTED` | 85-95 | "VirtualBox detected" |
| Emulator Check | BlueStacks, Nox, LDPlayer | `EMULATOR_DETECTED` | 85-95 | "BlueStacks HD-Player detected" |

---

#### Debugging Tools Detection
**SAC Detection:** `EnvironmentIntegrityService.ScanHardwareSoftwareArtifacts() - Part 1`  
**Detection Output:** `(HasAnomalies: true, Details: ["WinDbg detected", "dnSpy detected", ...])`

**Map to Server:**
```csharp
// SAC RiskAssessment
{
    RiskScore: 90,
    RiskLevel: "Cheating",
    RecommendedAction: "DEBUGGING_TOOL_DETECTED",
    Rationale: "Active debugger detected (code manipulation risk)"
}

// SAC → Server
{
    eventType: "DEBUGGER_DETECTED",
    severityScore: 90,
    description: "WinDbg active",
    timestamp: DateTime.UtcNow
}

// Server Storage
{
    EventType: "DEBUGGER_DETECTED",
    SeverityScore: 90,
    Description: "WinDbg active",
    Timestamp: "...",
    RoomId: 1,
    StudentId: 1
}
```

**Debugger Process Matrix:**

| SAC Detection | Process Name | Event Type | Severity | Impact |
|---------------|--------------|-----------|----------|--------|
| Debugger Check | WinDbg, x64dbg, OllyDbg | `DEBUGGER_DETECTED` | 90-95 | CRITICAL - Code manipulation |
| Disassembler Check | IDA Pro, dnSpy | `DECOMPILER_DETECTED` | 85-90 | CRITICAL - Exam content exposure |

---

### Phase 7: Behavioral Monitoring (Medium-Low Severity)

#### Window Focus Detection
**SAC Detection:** `BehavioralMonitoringService.DetectWindowFocus()`  
**Detection Output:** `(SourceApp: "Biology Exam", TargetApp: "Discord", Timestamp: ...)`

**Map to Server:**
```csharp
// SAC RiskAssessment (Decision Engine output)
// RiskScore depends on target application:
// - Switching to suspicious app (Discord, TeamViewer, etc.): 50-75
// - Normal app switch (Notepad, Calculator, etc.): 10-20

{
    RiskScore: 65,
    RiskLevel: "Suspicious",
    RecommendedAction: "WINDOW_FOCUS_SUSPICIOUS",
    Rationale: "Alt+Tab to Discord (communication app)"
}

// SAC → Server
{
    eventType: "WINDOW_SWITCH_SUSPICIOUS",
    severityScore: 65,
    description: "From: Biology Exam → To: Discord",
    timestamp: DateTime.UtcNow
}

// Server Storage
{
    EventType: "WINDOW_SWITCH_SUSPICIOUS",
    SeverityScore: 65,
    Description: "From: Biology Exam → To: Discord",
    Timestamp: "...",
    RoomId: 1,
    StudentId: 1
}
```

**Window Switch Classification:**

| From App | To App | Event Type | Severity | Rationale |
|----------|--------|-----------|----------|-----------|
| Exam | Discord | `WINDOW_SWITCH_SUSPICIOUS` | 65 | Communication attempt |
| Exam | Chrome | `WINDOW_SWITCH_SUSPICIOUS` | 50 | Search/external info |
| Exam | Notepad | `WINDOW_SWITCH_NORMAL` | 15 | Legitimate note-taking |
| Exam | Notepad | `WINDOW_SWITCH_NORMAL` | 15 | Safe app |

---

#### Clipboard Activity Detection
**SAC Detection:** `BehavioralMonitoringService.DetectClipboardActivity()`  
**Detection Output:** `(SourceApp: "Chrome", ContentType: "Text", TextLength: 245, ...)`

**Map to Server:**
```csharp
// SAC RiskAssessment
// Clipboard events usually low severity (monitoring only)
// UNLESS copying large amounts from external source

{
    RiskScore: 30,
    RiskLevel: "Safe",
    RecommendedAction: "CLIPBOARD_ACTIVITY",
    Rationale: "Clipboard access: Chrome copied 245 characters"
}

// SAC → Server
{
    eventType: "CLIPBOARD_TEXT_COPY",
    severityScore: 30,
    description: "From: Chrome | Type: Text | Length: 245 characters",
    timestamp: DateTime.UtcNow
}

// Server Storage
{
    EventType: "CLIPBOARD_TEXT_COPY",
    SeverityScore: 30,
    Description: "From: Chrome | Type: Text | Length: 245 characters",
    Timestamp: "...",
    RoomId: 1,
    StudentId: 1
}
```

**Clipboard Event Classification:**

| Source App | Content Type | Event Type | Severity | Example |
|------------|--------------|-----------|----------|---------|
| Chrome | Text (small) | `CLIPBOARD_TEXT_COPY` | 20-30 | "245 characters from Chrome" |
| Discord | Text (any) | `CLIPBOARD_TEXT_COPY` | 60-75 | "Suspicious: Copying from Discord" |
| Exam Tool | Files | `CLIPBOARD_FILE_COPY` | 50-65 | "Copied file path" |
| External | Images | `CLIPBOARD_IMAGE_COPY` | 40-55 | "Image from browser" |

---

#### Idle Detection
**SAC Detection:** `BehavioralMonitoringService.DetectIdleActivity()`  
**Detection Output:** `(IdleDuration: 65 seconds, TimeStopped: ..., ...)`

**Map to Server:**
```csharp
// SAC RiskAssessment
// Idle detection usually informational (1/3 severity)
// BUT: Repeated idle might indicate cheating strategy

{
    RiskScore: 15,
    RiskLevel: "Safe",
    RecommendedAction: "IDLE_DETECTED",
    Rationale: "Student idle for 65 seconds (normal behavior)"
}

// SAC → Server
{
    eventType: "IDLE_PERIOD",
    severityScore: 15,
    description: "Idle for 65 seconds",
    timestamp: DateTime.UtcNow
}

// Server Storage
{
    EventType: "IDLE_PERIOD",
    SeverityScore: 15,
    Description: "Idle for 65 seconds",
    Timestamp: "...",
    RoomId: 1,
    StudentId: 1
}
```

**Idle Classification:**

| Duration | Frequency | Event Type | Severity | Note |
|----------|-----------|-----------|----------|------|
| 60-120 sec | First | `IDLE_PERIOD` | 15 | Natural break |
| 60-120 sec | 5+ times | `EXCESSIVE_IDLE` | 40-50 | Possible cheating |
| 300+ sec | Any | `LONG_IDLE` | 60-70 | Suspicious inactivity |

---

#### Blacklisted Process Detection
**SAC Detection:** `BehavioralMonitoringService.DetectBlacklistedProcesses()`  
**Detection Output:** `(ProcessName: "discord.exe", Severity: "High", ...)`

**Map to Server:**
```csharp
// SAC RiskAssessment
// Blacklisted process is immediate escalation

{
    RiskScore: 75,
    RiskLevel: "Suspicious",
    RecommendedAction: "BLACKLISTED_PROCESS",
    Rationale: "Detected running process: Discord (communication app)"
}

// SAC → Server
{
    eventType: "BLACKLISTED_PROCESS_RUNNING",
    severityScore: 75,
    description: "Discord (discord.exe) - Communication Application",
    timestamp: DateTime.UtcNow
}

// Server Storage
{
    EventType: "BLACKLISTED_PROCESS_RUNNING",
    SeverityScore: 75,
    Description: "Discord (discord.exe) - Communication Application",
    Timestamp: "...",
    RoomId: 1,
    StudentId: 1
}
```

**Process Classification:**

| Category | Process | Event Type | Severity |
|----------|---------|-----------|----------|
| Communication | discord, telegram, slack, teams | `BLACKLISTED_PROCESS_RUNNING` | 75-85 |
| Remote Access | teamviewer, anydesk, ultravnc | `BLACKLISTED_PROCESS_RUNNING` | 80-90 |
| Screen Capture | obs, ffmpeg, bandicam | `BLACKLISTED_PROCESS_RUNNING` | 75-85 |
| Debugging | windbg, x64dbg, dnspy | `BLACKLISTED_PROCESS_RUNNING` | 85-95 |

---

## 🗂️ Complete Event Type Enum

**Use this enum on both SAC and Server for consistency:**

```csharp
namespace AcademicSentinel.Shared.Enums
{
    public enum EventType
    {
        // Phase 6: Environment Violations
        VM_DETECTED = 100,
        EMULATOR_DETECTED = 101,
        DEBUGGER_DETECTED = 102,
        DECOMPILER_DETECTED = 103,
        
        // Phase 7: Behavioral Violations
        WINDOW_SWITCH_SUSPICIOUS = 200,
        WINDOW_SWITCH_NORMAL = 201,
        CLIPBOARD_TEXT_COPY = 300,
        CLIPBOARD_IMAGE_COPY = 301,
        CLIPBOARD_FILE_COPY = 302,
        CLIPBOARD_HTML_COPY = 303,
        IDLE_PERIOD = 400,
        EXCESSIVE_IDLE = 401,
        LONG_IDLE = 402,
        BLACKLISTED_PROCESS_RUNNING = 500,
        
        // Phase 8: Risk Assessment Results
        RISK_ASSESSMENT_SAFE = 600,
        RISK_ASSESSMENT_SUSPICIOUS = 601,
        RISK_ASSESSMENT_CHEATING = 602,
        
        // System Events
        EXAM_STARTED = 700,
        EXAM_ENDED = 701,
        STUDENT_CONNECTED = 702,
        STUDENT_DISCONNECTED = 703
    }
}
```

---

## 🔀 Severity Score Mapping

**SAC → Server Severity Translation:**

```
SAC Risk Score    →    Server Severity    →    Category
0-20              →    5-10                →    Safe (Green)
21-40             →    25-40               →    Low Risk (Yellow)
41-60             →    45-60               →    Medium Risk (Orange)
61-80             →    65-80               →    High Risk (Red)
81-100            →    85-100              →    Critical (Dark Red)
```

**Database Storage (SQL):**
```sql
INSERT INTO MonitoringEvents 
(RoomId, StudentId, EventType, SeverityScore, Description, Timestamp)
VALUES
(1, 1, 'WINDOW_SWITCH_SUSPICIOUS', 65, 'From: Exam → To: Discord', '2026-01-15T14:30:45Z');
```

---

## 🔄 Event Flow Diagram

```
┌────────────────────────────────────────────────────┐
│ Phase 6-7: Detection (Real-Time Monitoring)        │
├────────────────────────────────────────────────────┤
│ • Alt+Tab detected → To Discord                    │
│ • Clipboard action → Text copy from Chrome         │
│ • Process scan → Discord running                   │
│ • Environment check → No VMs                       │
└──────────────────┬─────────────────────────────────┘
                   │
                   ▼
┌────────────────────────────────────────────────────┐
│ Phase 8: Decision Engine (Assessment)              │
├────────────────────────────────────────────────────┤
│ Event: Alt+Tab to Discord                          │
│ Score: 65 (Suspicious)                             │
│ Action: WINDOW_FOCUS_SUSPICIOUS                    │
│ Reason: Communication attempt during exam          │
└──────────────────┬─────────────────────────────────┘
                   │
                   ▼
┌────────────────────────────────────────────────────┐
│ Phase 9: Event Logger (Transmission)               │
├────────────────────────────────────────────────────┤
│ Convert RiskAssessment → MonitoringEventDto        │
│ EventType: WINDOW_SWITCH_SUSPICIOUS               │
│ SeverityScore: 65                                  │
│ Description: "From: Exam → To: Discord"            │
│ Add to batch queue                                 │
└──────────────────┬─────────────────────────────────┘
                   │
                   ▼ (via SignalR)
┌────────────────────────────────────────────────────┐
│ Server: SignalR Hub (Reception)                    │
├────────────────────────────────────────────────────┤
│ SendMonitoringEvent() called                       │
│ Authenticate studentId from JWT                    │
│ Create MonitoringEvent record                      │
│ Store in database                                  │
└──────────────────┬─────────────────────────────────┘
                   │
                   ▼
┌────────────────────────────────────────────────────┐
│ Server: Database Storage                           │
├────────────────────────────────────────────────────┤
│ MonitoringEvents Table:                            │
│ - EventType: WINDOW_SWITCH_SUSPICIOUS             │
│ - SeverityScore: 65                                │
│ - StudentId: 1                                     │
│ - RoomId: 1                                        │
│ - Timestamp: 2026-01-15T14:30:45Z                  │
└──────────────────┬─────────────────────────────────┘
                   │
                   ▼
┌────────────────────────────────────────────────────┐
│ Server: Broadcast to IMC                           │
├────────────────────────────────────────────────────┤
│ ViolationDetected broadcast sent                   │
│ To all instructors in room group                   │
│ Includes: StudentId, EventType, SeverityScore      │
└────────────────────────────────────────────────────┘
```

---

## 📊 Event Type Statistics for Thesis

**Total Event Types Tracked:** 13+

**By Severity:**
- Critical (90-100): 4 types (Debuggers, VMs)
- High (75-85): 5 types (Emulators, Communication)
- Medium (40-60): 3 types (Clipboard, Suspicious Switches)
- Low (10-30): 4+ types (Idle, Normal Switches)

**By Category:**
- Environment Detection: 4 types
- Behavioral Monitoring: 9+ types
- Risk Assessment: 3 types
- System Events: 3 types

**Detection Rate by Type:**
- VM/Emulator: 32+ processes monitored
- Debuggers: 9 processes monitored
- Communication Apps: 7 processes monitored
- Total: 65+ suspicious processes

---

## 🔍 Query Examples for Thesis Reporting

**Get all violations for a student in exam:**
```sql
SELECT EventType, SeverityScore, Description, Timestamp
FROM MonitoringEvents
WHERE StudentId = 1 AND RoomId = 1
ORDER BY Timestamp DESC;
```

**Violation distribution by type:**
```sql
SELECT EventType, COUNT(*) as Count, AVG(SeverityScore) as AvgSeverity
FROM MonitoringEvents
GROUP BY EventType
ORDER BY Count DESC;
```

**Timeline of events during exam:**
```sql
SELECT Timestamp, EventType, SeverityScore, Description
FROM MonitoringEvents
WHERE RoomId = 1 AND Timestamp BETWEEN '2026-01-15 14:00:00' AND '2026-01-15 15:00:00'
ORDER BY Timestamp ASC;
```

---

**Last Updated:** Today  
**Purpose:** SAC ↔ Server Communication Reference  
**Related:** SAC_SERVER_INTEGRATION_GUIDE.md, SIGNALR_API_REFERENCE.md
