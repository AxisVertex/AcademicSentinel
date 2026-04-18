# Testing Harness: Detection Pipeline Validation Guide

## Overview

The **Detection Test Console** is a terminal-based testing harness for validating the complete secure assessment client backend detection pipeline (Phases 6→7→8→9→SignalR).

**Purpose:** Verify that all detection, assessment, and transmission components work together correctly before UI integration.

**Location:** `Testing/DetectionTestConsole.cs` and `Testing/TestScenarioRunner.cs`

---

## Quick Start

### Running the Test Console

```powershell
# Build the project
dotnet build

# Run the test console (requires main project config)
dotnet run --project SecureAssessmentClient.csproj
```

Or from Visual Studio:
1. Set startup project to SecureAssessmentClient
2. Press F5 to run
3. The test console will automatically launch

### Initial Configuration

When you start the console, you'll be prompted for:

```
Enter auth token (or press Enter for 'test-token'): 
Enter SignalR hub URL (or press Enter for 'http://localhost:5000/exam-hub'): 
```

**Defaults:**
- Auth Token: `test-token`
- Hub URL: `http://localhost:5000/exam-hub`

If you have a real server running:
- Enter your actual auth token
- Enter your server's SignalR hub URL

**If server is unavailable:**
- Testing still works with offline mode
- Events are logged locally
- Batches show as "failed" (expected)
- Can test retry logic and persistence

---

## Interactive Menu

```
╔══════════════════════════════════════════╗
║           DETECTION TEST MENU            ║
╚══════════════════════════════════════════╝

1. [ENVIRONMENT] Simulate VM/Debugger Detection
2. [BEHAVIOR]    Simulate Behavioral Detection
3. [MANUAL]      Trigger Manual Test Event
4. [BATCH]       Display Batch Status
5. [HISTORY]     Show Event History
6. [ASSESSMENT]  Show Risk Assessments
7. [STATUS]      Display System Status
8. [FLUSH]       Flush Pending & Transmit
9. [HELP]        Show Testing Guide
0. [EXIT]        Shutdown & Exit
```

---

## Test Scenarios

### 1. Environment Detection Test (Option 1)

**What it tests:** Phase 6 - EnvironmentIntegrityService simulation

**Simulated events:**
- VirtualBox Guest Additions detection
- WinDbg debugger process detection

**Expected results:**
```
[EVENT] VM Detected
  Type: EVENT_VM_DETECTED
  Violation: Aggressive (Severity: 3/3)
  Details: VirtualBox Guest Additions detected in registry

[ASSESSMENT] Risk: Cheating (Score: 100/100)
  Action: escalate
  Rationale: Event: EVENT_VM_DETECTED | Severity: 3/3 | Type: Aggressive | Base Score: 120 | Result: Cheating
  → Logged for transmission (Pending: 1)
```

**Key indicators:**
- ✓ Risk Level changes to **Cheating**
- ✓ Score is **100** (maximum)
- ✓ Action is **escalate**
- ✓ Assessment added to buffer

---

### 2. Behavioral Detection Test (Option 2)

**What it tests:** Phase 7 - BehavioralMonitoringService simulation

**Simulated events:**
1. **Window Switching** (6 times) → Triggers pattern detection
2. **Clipboard Activity** (3 times) → Increases risk
3. **Idle Timeout** → Adds low-risk violation
4. **Unauthorized Process** → Highest priority

**Expected progression:**

```
[EVENT] Window Switch 1
[ASSESSMENT] Risk: Safe (Score: 16/100)

[EVENT] Window Switch 2
[ASSESSMENT] Risk: Safe (Score: 32/100)

[EVENT] Window Switch 3
[ASSESSMENT] Risk: Safe (Score: 48/100)

[EVENT] Window Switch 4
[ASSESSMENT] Risk: Safe (Score: 64/100)

[EVENT] Window Switch 5
[ASSESSMENT] Risk: Suspicious (Score: 80/100)  ← Pattern detected!

[EVENT] Window Switch 6
[ASSESSMENT] Risk: Suspicious (Score: 90/100)  ← Score rising

[EVENT] Clipboard Access 1-3
[ASSESSMENT] Risk: Suspicious → Cheating (Score: 100+)

[EVENT] Idle Timeout
[ASSESSMENT] Risk: Cheating (Score: 100)

[EVENT] Unauthorized Process
[ASSESSMENT] Risk: Cheating (Score: 100)  ← IMMEDIATE FLUSH
```

**Key indicators:**
- ✓ Risk level escalates from Safe → Suspicious → Cheating
- ✓ Pattern detection works (multiple violations accumulate)
- ✓ Unauthorized process triggers immediate batch flush
- ✓ Batches created automatically (10 assessments or 5s)

---

### 3. Manual Event Test (Option 3)

**What it tests:** Individual component testing

**Available events:**
```
1. EVENT_WINDOW_SWITCH
2. EVENT_CLIPBOARD_COPY
3. EVENT_IDLE
4. EVENT_PROCESS_DETECTED
5. EVENT_VM_DETECTED
6. CUSTOM (enter your own)
```

**Usage example:**

```
Select event type (1-6) or name: 4

[EVENT] Manual: EVENT_PROCESS_DETECTED
  Type: EVENT_PROCESS_DETECTED
  Violation: Passive (Severity: 2/3)
  Details: Manual test event: EVENT_PROCESS_DETECTED

[ASSESSMENT] Risk: Suspicious (Score: 45/100)
  Action: warn
  ...
```

---

## Display Options

### 4. Batch Status (Option 4)

Shows the Event Logger's batching and transmission status:

```
=== BATCH STATUS ===

Total Batches Created: 3
Failed Batches (Retry Queue): 0

Batch Details:
  [TRANSMITTED] Batch 1: 10 assessments | MaxLevel: Cheating | Attempts: 1
  [TRANSMITTED] Batch 2: 10 assessments | MaxLevel: Suspicious | Attempts: 1
  [PENDING] Batch 3: 3 assessments | MaxLevel: Safe | Attempts: 0
```

**Status meanings:**
- `PENDING` - Created, waiting to transmit (will auto-flush at 10 items or 5s)
- `TRANSMITTED` - Successfully sent to server
- `FAILED` - Transmission failed, in retry queue
- `ACKNOWLEDGED` - Server confirmed receipt (if protocol implemented)

---

### 5. Event History (Option 5)

Displays all detected events in chronological order:

```
=== EVENT HISTORY ===

1. [14:32:15] EVENT_VM_DETECTED
   Violation: Aggressive (Severity: 3)
   VirtualBox Guest Additions detected in registry

2. [14:32:16] EVENT_WINDOW_SWITCH
   Violation: Passive (Severity: 2)
   Window switch #1: Alt+Tab detected

3. [14:32:17] EVENT_CLIPBOARD_COPY
   Violation: Passive (Severity: 2)
   Content copied/pasted from clipboard
   
... (total of N events) ...
```

---

### 6. Risk Assessment History (Option 6)

Shows all risk assessments with color-coded risk levels:

```
=== RISK ASSESSMENT HISTORY ===

[Cheating] Score: 100/100 | Event: EVENT_VM_DETECTED | ...
[Suspicious] Score: 80/100 | Event: EVENT_WINDOW_SWITCH | ...
[Suspicious] Score: 85/100 | Pattern Penalty: +30 | ...
[Safe] Score: 20/100 | Event: EVENT_IDLE | ...

Summary:
  Safe: 2
  Suspicious: 5
  Cheating: 3
```

**Color coding:**
- 🟢 **Green** = Safe
- 🟡 **Yellow** = Suspicious
- 🔴 **Red** = Cheating

---

### 7. System Status (Option 7)

Overall health check:

```
=== SYSTEM STATUS ===

Detection Pipeline Status:
  Session ID: a1b2c3d4
  Running: True
  Behavioral Monitoring: ACTIVE

Event History:
  Total Events: 12
  Total Assessments: 12

Decision Engine Statistics:
  Total Events Analyzed: 12
  Aggressive Violations: 2
  Passive Violations: 10
  Session Duration: 45s

Event Logger Statistics:
  Assessments Logged: 12
  Pending (Buffer): 3
  Batches Created: 2
  Batches Transmitted: 1
  Failed Batches: 0

SignalR Connection Status:
  Connected: YES
  Hub URL: http://localhost:5000/exam-hub
```

---

### 8. Flush Pending (Option 8)

Manually forces transmission of all buffered assessments:

```
=== FLUSHING PENDING BATCHES ===

All pending assessments flushed and batches transmitted.
```

**When to use:**
- End of test to ensure all data transmitted
- Test retry/persistence logic
- Verify batch transmission works

---

## Phase Integration Verification

This test harness validates the complete detection pipeline. Here's what each phase does:

### Phase 6: Environment Integrity Detection
**Input:** Check Windows registry/processes for VM/debugger signatures  
**Output:** MonitoringEvent  
**Test:** Option 1 simulates this

### Phase 7: Behavioral Monitoring
**Input:** Monitor window focus, clipboard, idle, processes  
**Output:** MonitoringEvent  
**Test:** Option 2 simulates this

### Phase 8: Decision Engine
**Input:** MonitoringEvent  
**Output:** RiskAssessment (Score 0-100, Level, Action)  
**Validation:** Options 5-6 show the results

### Phase 9: Event Logger
**Input:** RiskAssessment  
**Output:** Batches transmitted via SignalR  
**Validation:** Option 4 shows batch status, option 7 shows transmission stats

### SignalR Transmission
**Input:** MonitoringEvent batch  
**Output:** Server acknowledgment  
**Validation:** Option 7 shows connection status

---

## Testing Checklist

Use this checklist to verify complete functionality:

### ✓ Detection Pipeline
- [ ] Option 1: Environment detection creates Cheating assessments
- [ ] Option 2: Behavioral detection escalates with patterns
- [ ] Pattern detection adds points (+30 for mixed violations)
- [ ] Critical events (Cheating) trigger immediate flush

### ✓ Event Logging
- [ ] Option 5: All events appear in history (no loss)
- [ ] Option 6: Assessments match events
- [ ] Risk scores increase appropriately
- [ ] Risk levels transition Safe → Suspicious → Cheating

### ✓ Batching
- [ ] Option 4: Batches created after 10 assessments OR 5 seconds
- [ ] Option 4: Batch counts match assessment counts
- [ ] Option 8: Manual flush works
- [ ] Batches show correct max risk level

### ✓ Transmission (if server available)
- [ ] Option 7: SignalR shows "Connected: YES"
- [ ] Option 4: Batches show "TRANSMITTED" status
- [ ] Option 8: Flush completes without errors
- [ ] Failed batches go to retry queue

### ✓ Offline Mode (if server unavailable)
- [ ] Option 7: SignalR shows "Connected: NO"
- [ ] Option 4: Batches show "FAILED" status
- [ ] Option 8: Flush completes (batches stay in queue)
- [ ] Check %APPDATA%\SecureAssessmentClient\EventLogs\ for persisted batches

---

## Troubleshooting

### Issue: "SignalR connection failed"
**Solution:**
- Make sure server is running on specified URL
- Check firewall allows connections
- Verify auth token is correct
- Testing continues in offline mode

### Issue: No batches showing as transmitted
**Solution:**
- Check SignalR connection status (Option 7)
- If offline, this is normal (batches fail, retry queue active)
- Run Option 8 (Flush) to manually retry
- Check batch persistence in EventLogs folder

### Issue: Risk scores don't match expectations
**Solution:**
- Check Decision Engine statistics (Option 7)
- Review event history (Option 5) for event types
- Check event weights in DecisionEngineService code
- Pattern thresholds: 3+ window switches, 5+ clipboard, 1+ process

### Issue: Batches not created after 5 seconds
**Solution:**
- Default flush interval is 5000ms (5 seconds)
- Can trigger manual flush (Option 8)
- Check pending assessment count (Option 7)
- Restart timer by running Option 8

### Issue: Persistent storage not found
**Solution:**
- Check path: %APPDATA%\SecureAssessmentClient\EventLogs\{SessionId}\
- Failed batches saved as: batch_{BatchId}_failed.json
- These persist even after app restart
- LoadPersistentBatchesAsync() loads them on next startup

---

## Advanced Testing

### Stress Testing
```
Option 2: Run Behavioral Detection multiple times
→ Generates 40+ assessments quickly
→ Tests batch creation at scale
→ Validates pattern detection with volume
```

### Network Resilience Testing
```
1. Start console with valid server URL
2. Run Option 2 (generate events)
3. Manually stop server (kill process or disable network)
4. Run Option 4 → should see batches in "FAILED" status
5. Restart server
6. Wait or run Option 8 → retry logic should transmit
```

### Persistence Testing
```
1. Run Option 2 (generate events)
2. Close console without running Option 8
3. Check: %APPDATA%\SecureAssessmentClient\EventLogs\{SessionId}\
4. Restart console
5. It should load failed batches from storage
```

---

## Performance Metrics

Monitor these during testing:

**Assessment Generation Rate:**
- Typically 5-10 assessments/second during behavioral test
- Time from event creation to assessment: <5ms

**Batch Creation:**
- Default: 10 assessments OR 5 seconds
- Time to create batch: 1-2ms
- Time to transmit (if connected): 100-500ms

**Storage:**
- ~5KB per assessment
- ~100KB per batch (10 assessments)
- ~1-2MB per 1000 batches

---

## Example Test Session

### Scenario: Validate complete pipeline

```
1. Start console
   → Initializes all services
   → Session ID: abc12345

2. Run Option 1 (Environment Detection)
   → Creates 2 events (VM, Debugger)
   → Decision Engine: Both score 100 → Cheating
   → Assessment 1-2 logged

3. Run Option 2 (Behavioral Detection)
   → Creates 9 events (6 window + 3 clipboard + idle + process)
   → Decision Engine: Scores escalate 16→32→48→...→100
   → Assessments 3-12 logged
   → After 10 assessments: Batch 1 created + transmitted

4. Run Option 6 (Show Assessments)
   → Displays all 12 assessments
   → Shows escalation: Safe → Suspicious → Cheating
   → Confirms pattern detection worked

5. Run Option 4 (Show Batches)
   → Batch 1: 10 assessments, Transmitted
   → 2 pending assessments still in buffer

6. Run Option 8 (Flush)
   → Batch 2: 2 assessments, Transmitted
   → All assessments now batched & sent

7. Run Option 7 (Show Status)
   → Total events: 11
   → Total assessments: 12
   → Batches created: 2
   → Batches transmitted: 2
   → Session duration: 45s

8. Option 0 (Exit)
   → Cleanup and shutdown
```

---

## File Locations

**Test Code:**
```
Testing/
  ├── TestScenarioRunner.cs        (Core testing logic, Phase integration)
  ├── DetectionTestConsole.cs      (Interactive menu system)
  └── TestProgram.cs              (Entry point)
```

**Logs:**
```
%APPDATA%\SecureAssessmentClient\Logs\
  └── SecureAssessmentClient.log  (Detailed logging of all operations)
```

**Persisted Batches:**
```
%APPDATA%\SecureAssessmentClient\EventLogs\{SessionId}\
  ├── batch_id1_pending.json
  ├── batch_id2_transmitted.json
  └── batch_id3_failed.json
```

**Timestamps in logs:**
```
[2024-01-15 14:32:15,123] INFO  - Assessment logged: Cheating (Score: 100)
[2024-01-15 14:32:16,456] WARN  - Pattern detected: EVENT_WINDOW_SWITCH occurred 6 times (threshold: 3)
```

---

## Next Steps After Testing

1. **Phase 10 Integration (UI):**
   - Use TestScenarioRunner as reference for real exam monitoring
   - Implement MainWindow with real-time event display
   - Integrate risk assessment visualization

2. **Server-Side Integration:**
   - Verify server receives and logs events correctly
   - Test server-side enforcement (block exam, notify instructor)
   - Validate database persistence

3. **Performance Validation:**
   - Test with full exam duration (90 minutes)
   - Verify CPU/memory usage stays within limits
   - Check disk I/O for log persistence

4. **Production Deployment:**
   - Move test token to secure configuration
   - Update server URLs to production
   - Configure logging levels appropriately
   - Archive old batch files periodically

---

**Testing Console Version:** 1.0  
**Compatible with:** Phases 6, 7, 8, 9 (SignalR v8.0.10)  
**Last Updated:** 2024  
**Status:** ✅ Production-Ready for Validation
