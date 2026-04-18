# Backend Implementation Roadmap - Secure Assessment Client (SAC)

**Status:** Phase 2 Complete ✅ | Phase 3-9 Pending  
**Developer Focus:** Backend Services Only (Phases 1-3, 5-9)  
**Co-Developer UI:** Deferred until backend services ready  
**Date:** 2024  
**Framework:** .NET 10 | C# 13.0

---

## 📊 Project Overview

The Secure Assessment Client (SAC) is a student-side proctoring application that monitors exam integrity through:

1. **Authentication** - Secure login with JWT tokens
2. **Real-time Communication** - SignalR hub connection for live events
3. **Environmental Monitoring** - VM detection, debugger detection
4. **Behavioral Monitoring** - Window switching, clipboard activity, process detection
5. **Risk Classification** - Real-time risk scoring and categorization
6. **Event Transmission** - Secure logging and server transmission

**Architectural Approach:** Services-based backend with clean separation from UI layer (deferred to co-developer)

---

## ✅ COMPLETED PHASES

### Phase 1: Project Creation & Dependencies ✅
**Status:** COMPLETE  
**Deliverables:**
- ✅ Solution and project setup (.NET 10 WPF project)
- ✅ 6 NuGet packages installed (SignalR, System.Text.Json, log4net, etc.)
- ✅ Configuration infrastructure (ServerConfig.cs + AppSettings.json)
- ✅ Logging system (Logger.cs + log4net.config)
- ✅ Token management (TokenManager.cs with AES-256 encryption)
- ✅ Constants definitions (Constants.cs with event/risk/room types)

**Documentation:** PHASE1_COMPLETION_REPORT.md

---

### Phase 2: Data Models & DTOs ✅
**Status:** COMPLETE  
**Deliverables:**
- ✅ **Authentication Models** (3 files)
  - LoginRequest.cs - Login credentials
  - AuthToken.cs - Token details (AccessToken, TokenType, ExpiresIn)
  - LoginResponse.cs + UserInfo - Complete login response with user context
  
- ✅ **Room Models** (3 files)
  - RoomStatus.cs - Enum (Pending, Countdown, Active, Ended)
  - RoomDto.cs - Assessment room data
  - JoinRoomRequest.cs + EnrollWithCodeRequest - Room join requests

- ✅ **Monitoring Models** (3 files)
  - RiskLevel.cs - Enums (RiskLevel, ViolationType)
  - MonitoringEvent.cs - Individual event record
  - DetectionSettings.cs - Per-room detection configuration

**Build Status:** All 9 models compile with zero errors  
**Documentation:** PHASE2_COMPLETION_REPORT.md

---

## 🚀 PENDING PHASES (Backend Focus)

### Phase 3: Authentication & Login Services 🔄 (NEXT)
**Status:** PENDING  
**Scope:** Backend services only (no UI components)

**Files to Create:**
1. **Services/ApiService.cs**
   - Purpose: HTTP client wrapper for API communication
   - Methods:
     - `LoginAsync(LoginRequest)` → LoginResponse
     - `GetAvailableRoomsAsync()` → List<RoomDto>
     - `JoinRoomAsync(JoinRoomRequest)` → RoomDto
     - `SetAuthToken(token)` - Set Bearer token
   - Features:
     - Automatic token refresh
     - Error handling and logging
     - JSON serialization via System.Text.Json

2. **Services/AuthService.cs**
   - Purpose: Authentication business logic
   - Methods:
     - `LoginAsync(email, password)` → LoginResponse
     - `IsAuthenticatedAsync()` → bool
     - `GetCurrentUserAsync()` → UserInfo
     - `LogoutAsync()` → void
   - Features:
     - Delegates HTTP calls to ApiService
     - Manages token lifecycle via TokenManager
     - Integrates with logging system

**Integration Points:**
- Uses models from Phase 2 (LoginRequest, LoginResponse, UserInfo)
- Uses TokenManager from Phase 1 (for secure token storage)
- Uses Logger from Phase 1 (for event logging)
- Uses AppSettings.json from Phase 1 (for API base URL)

**UI Coordination:**
- UI Developer (co-dev) will create LoginWindow.xaml/cs in Phase 4 that calls AuthService

**Acceptance Criteria:**
- ✅ Both services compile without errors
- ✅ Methods match expected signatures
- ✅ Token is stored securely after login
- ✅ Logging captures authentication events

---

### Phase 5: SignalR Connection Service 🔄
**Status:** PENDING (after Phase 3)

**Files to Create:**
1. **Services/SignalRService.cs**
   - Purpose: Real-time bidirectional communication with server
   - Connection Management:
     - `ConnectAsync(roomId, token)` → Task
     - `DisconnectAsync()` → Task
     - `IsConnectedAsync()` → bool
   - Event Transmission:
     - `SendMonitoringEventAsync(MonitoringEvent)` → Task
     - `SendBatchEventsAsync(List<MonitoringEvent>)` → Task
   - Event Reception:
     - `OnServerMessageReceived` - event handler for server messages
     - `OnConnectionLost` - reconnection handler
     - `OnRoomStatusChanged` - room state change notifications
   - Features:
     - Automatic reconnection with exponential backoff
     - Event buffering during disconnection
     - Secure token-based authentication

**Integration Points:**
- Uses models from Phase 2 (MonitoringEvent, DetectionSettings)
- Receives DetectionSettings from server for configuration
- Consumes AppSettings.json for SignalR hub URL
- Uses Logger from Phase 1

**UI Coordination:**
- No direct UI dependency; services run in background
- UI layer will subscribe to OnRoomStatusChanged events in Phase 10

**Acceptance Criteria:**
- ✅ Establishes WebSocket connection to SignalR hub
- ✅ Handles disconnection/reconnection gracefully
- ✅ Transmits monitoring events in real-time
- ✅ Respects DetectionSettings for behavior configuration

---

### Phase 6: Environment Integrity Detection 🔄
**Status:** PENDING (after Phase 3)

**Files to Create:**
1. **Services/DetectionService/EnvironmentIntegrityService.cs**
   - Purpose: Detect suspicious system environments (VMs, debuggers, etc.)
   - Methods:
     - `CheckVirtualizationAsync()` → bool (is VM?)
     - `CheckDebuggerAsync()` → bool (debugger attached?)
     - `GenerateEnvironmentEventAsync()` → MonitoringEvent
     - `IsEnvironmentSafeAsync()` → bool (comprehensive check)
   - Detection Techniques:
     - Registry checks for VM software
     - Process enumeration for debuggers/suspicious tools
     - System information analysis
     - Windows API CodePack integration
   - Features:
     - Runs on separate thread to avoid UI blocking
     - Configurable via DetectionSettings
     - Generates MonitoringEvents on violations

**Integration Points:**
- Creates MonitoringEvent objects (Phase 2 model)
- Respects DetectionSettings.EnableVirtualizationCheck flag
- Logs detections via Logger from Phase 1
- Events queued for Phase 9 transmission

**UI Coordination:**
- Runs in background; no UI interaction needed
- Results surfaced through MonitoringEvent data model

**Acceptance Criteria:**
- ✅ Detects common VM hypervisors
- ✅ Detects debuggers (WinDbg, dnSpy, etc.)
- ✅ Generates appropriate MonitoringEvents
- ✅ Respects detection settings configuration
- ✅ Handles Windows API errors gracefully

---

### Phase 7: Behavioral Monitoring Modules 🔄
**Status:** PENDING (after Phase 6)

**Files to Create:**
1. **Services/DetectionService/BehavioralMonitoringService.cs**
   - Purpose: Monitor student behavior during exam
   - Methods:
     - `StartMonitoringAsync(DetectionSettings)` → Task
     - `StopMonitoringAsync()` → Task
     - `OnClipboardActivityAsync(action)` → void (copy/paste detection)
     - `OnProcessDetectedAsync(processName)` → void
     - `OnWindowLostFocusAsync()` → void
     - `OnIdleDetectedAsync(duration)` → void
   - Monitoring Hooks:
     - Global clipboard listener (Windows API)
     - Process enumeration timer
     - Window focus change listener
     - Idle time tracker
   - Event Generation:
     - Creates MonitoringEvent for each detected behavior
     - Assigns ViolationType (Passive/Aggressive)
     - Calculates preliminary severity score

**Integration Points:**
- Creates MonitoringEvent objects (Phase 2 model)
- Reads from DetectionSettings for feature flags/thresholds
- Uses Windows API CodePack for system monitoring
- Logs activities via Logger
- Events queued for Phase 8 risk calculation

**UI Coordination:**
- Runs entirely in background
- Events flow to decision engine, not UI directly

**Acceptance Criteria:**
- ✅ Detects clipboard operations
- ✅ Detects suspicious processes
- ✅ Detects window focus changes
- ✅ Detects idle periods
- ✅ Generates MonitoringEvents with appropriate severity
- ✅ Respects configuration flags (can disable specific monitors)

---

### Phase 8: Decision Engine & Risk Classification 🔄
**Status:** PENDING (after Phase 7)

**Files to Create:**
1. **Services/DetectionService/DecisionEngineService.cs**
   - Purpose: Analyze collected events and classify overall risk
   - Methods:
     - `AnalyzeEventsAsync(List<MonitoringEvent>)` → RiskLevel
     - `CalculateRiskScoreAsync()` → int (0-100)
     - `GetDetailedRiskReportAsync()` → RiskAssessment (custom class)
     - `ShouldFlagSessionAsync()` → bool
   - Analysis Logic:
     - Aggregates multiple events into risk score
     - Applies weights to different event types
     - Considers time-window patterns (burst detection)
     - Applies StrictMode multiplier if enabled
     - Generates Risk Level (Safe → Suspicious → Cheating)
   - Features:
     - Real-time continuous analysis
     - Temporal pattern detection
     - Escalation logic (gradual increase vs. critical event)

**Custom Models Needed:**
2. **Models/Monitoring/RiskAssessment.cs** (if needed for detailed report)
   ```csharp
   public class RiskAssessment
   {
       public RiskLevel OverallRisk { get; set; }
       public int RiskScore { get; set; }
       public List<string> ViolationSummary { get; set; }
       public DateTime LastUpdated { get; set; }
   }
   ```

**Integration Points:**
- Consumes MonitoringEvent list from Phase 6-7 services
- Produces RiskLevel classification (Phase 2 enum)
- Respects DetectionSettings.StrictMode
- Logs analysis decisions via Logger
- Output used by Phase 9 for event categorization

**UI Coordination:**
- Provides risk data for MonitoringWindow (Phase 10) to display risk indicator

**Acceptance Criteria:**
- ✅ Classifies events into Safe/Suspicious/Cheating
- ✅ Risk score increases with event frequency/severity
- ✅ Detects burst patterns (sudden spike in violations)
- ✅ StrictMode increases sensitivity appropriately
- ✅ Generates detailed risk assessment report

---

### Phase 9: Event Logging & Transmission 🔄
**Status:** PENDING (after Phase 8)

**Files to Create:**
1. **Services/DetectionService/EventLoggerService.cs**
   - Purpose: Comprehensive event logging and server transmission
   - Methods:
     - `LogEventAsync(MonitoringEvent)` → Task
     - `LogBatchAsync(List<MonitoringEvent>)` → Task
     - `FlushPendingEventsAsync()` → Task
     - `GetSessionLogsAsync()` → List<MonitoringEvent>
   - Logging Destinations:
     - Local file (via log4net from Phase 1)
     - In-memory buffer (for batch transmission)
     - Server (via SignalRService from Phase 5)
   - Features:
     - Configurable batch size and transmission interval
     - Exponential backoff on transmission failure
     - Event deduplication (prevents duplicate transmissions)
     - Session-based organization

2. **Models/Monitoring/LogSession.cs** (if needed)
   ```csharp
   public class LogSession
   {
       public string SessionId { get; set; }
       public DateTime StartTime { get; set; }
       public DateTime? EndTime { get; set; }
       public List<MonitoringEvent> Events { get; set; }
   }
   ```

**Integration Points:**
- Consumes MonitoringEvent from Phase 6-7 services
- Uses Logger from Phase 1 for file logging
- Uses SignalRService from Phase 5 for transmission
- Respects AppSettings.json EventTransmissionInterval
- Produces archival logs for thesis data collection

**UI Coordination:**
- No direct UI dependency
- Logs are background process
- UI may query GetSessionLogsAsync for debugging in Phase 10

**Acceptance Criteria:**
- ✅ Events logged to file successfully
- ✅ Events batched and transmitted to server
- ✅ Transmission respects interval configuration
- ✅ Handles transmission failures gracefully
- ✅ Event timestamps and session IDs preserved

---

## 🔗 PHASE DEPENDENCIES

```
Phase 1: Setup & Config ✅
    ↓
Phase 2: Models & DTOs ✅
    ↓
Phase 3: Authentication Services (NEXT)
    ↓
Phase 5: SignalR Connection ←────────────────┐
    ↓                                         │
Phase 6: Environment Integrity Detection     │
    ↓                                         │
Phase 7: Behavioral Monitoring    (runs parallel with)
    ↓                                         │
Phase 8: Decision Engine & Risk Classification
    ↓
Phase 9: Event Logging & Transmission ────────┘

Phases 4 & 10: UI (Co-Developer, after backend ready)
```

---

## 📋 Handoff to Co-Developer (UI)

Once **all backend Phases 1-3, 5-9 are complete**, the co-developer will integrate UI:

### Phase 4: Room Discovery & UI
- **LoginWindow.xaml/cs** - Calls `AuthService.LoginAsync()`
- **RoomDashboardWindow.xaml/cs** - Displays list from `ApiService.GetAvailableRoomsAsync()`
- **RoomOrbControl.xaml/cs** - Custom visual for room status

### Phase 10: UI Integration & Final Testing
- **MonitoringWindow.xaml/cs** - Subscribes to risk level changes from DecisionEngineService
- **MonitoringIndicator.xaml/cs** - Real-time risk indicator
- **Converters** - RoomStatusToBrushConverter, ConnectionStatusConverter
- **Integration Testing** - End-to-end testing of all services through UI

### Required Service Interfaces for UI Integration
```csharp
// AuthService interface (Phase 3)
public interface IAuthService
{
    Task<LoginResponse> LoginAsync(string email, string password);
    Task<bool> IsAuthenticatedAsync();
    Task LogoutAsync();
}

// ApiService interface (Phase 3)
public interface IApiService
{
    Task<List<RoomDto>> GetAvailableRoomsAsync();
    Task<RoomDto> JoinRoomAsync(JoinRoomRequest request);
}

// SignalRService interface (Phase 5)
public interface ISignalRService
{
    Task ConnectAsync(string roomId, string token);
    event EventHandler<MonitoringEvent> OnEventReceived;
    Task SendEventAsync(MonitoringEvent evt);
}

// DecisionEngineService interface (Phase 8)
public interface IDecisionEngineService
{
    Task<RiskLevel> GetCurrentRiskLevelAsync();
    event EventHandler<RiskLevel> OnRiskLevelChanged;
    Task<RiskAssessment> GetDetailedRiskAsync();
}
```

---

## 📚 Documentation Files

- **SAC_BUILD_GUIDE.md** - Master build guide with phase details
- **PHASE1_COMPLETION_REPORT.md** - Phase 1 deliverables and verification
- **PHASE2_COMPLETION_REPORT.md** - Phase 2 data models specifications
- **BACKEND_IMPLEMENTATION_ROADMAP.md** ← You are here
- **Phase 3-9 Reports** - To be created as each phase completes

---

## ✨ Key Backend Principles

1. **Service Isolation** - Each service responsible for single concern
2. **Model-First** - Data models (Phase 2) drive service contracts
3. **Async/Await** - All I/O operations are non-blocking
4. **Logging Throughout** - Every significant action logged
5. **Configuration-Driven** - Services respect AppSettings.json and DetectionSettings
6. **Error Resilience** - Graceful handling of network/system failures
7. **UI-Agnostic** - No WPF/XAML dependencies in backend services

---

## 🎯 Next Immediate Steps

1. **Phase 3 - Start This Session:**
   - [ ] Create `Services/ApiService.cs` with HTTP client methods
   - [ ] Create `Services/AuthService.cs` with login/logout logic
   - [ ] Test authentication flow locally
   - [ ] Document Phase 3 completion

2. **Prepare Phase 5-9:**
   - [ ] Review SignalR documentation
   - [ ] Plan Windows API integration for behavioral monitoring
   - [ ] Design risk scoring algorithm
   - [ ] Define event transmission batching strategy

3. **Co-Developer Coordination:**
   - [ ] Share updated SAC_BUILD_GUIDE.md
   - [ ] Clarify Phase 4 & 10 scope with co-developer
   - [ ] Agree on service interface contracts
   - [ ] Establish integration testing strategy

---

**Ready to proceed to Phase 3? Let me know and we'll start building the authentication services!**
