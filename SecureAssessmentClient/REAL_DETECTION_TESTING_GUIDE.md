# Real Detection Testing Guide - Secure Assessment Client
## Thesis-Ready Detection Pipeline Validation

**Status:** ✅ Production Ready  
**Framework:** .NET 9  
**Detection Pipeline:** Phase 6 → Phase 7 → Phase 8 → Phase 9  
**Mode:** Real-time behavioral monitoring with manual triggers

---

## 📋 Overview

The **Real Detection Testing Console** validates your complete detection pipeline with **actual system monitoring** instead of synthetic events. This is thesis-ready and demonstrates:

- ✅ **Real environment integrity checks** (VM/debugger detection at startup)
- ✅ **Live behavioral monitoring** (window switching, clipboard, idle, processes)
- ✅ **Manual trigger scenarios** (user performs behavior → system detects → event logged)
- ✅ **Full event pipeline** (Phase 6 detection → Phase 8 assessment → Phase 9 logging)
- ✅ **Continuous monitoring** (runs until manually stopped)

---

## 🚀 Quick Start

### 1. Build the Project
```powershell
cd "E:\...SecureAssessmentClient"
dotnet build
```

### 2. Launch Testing Console
```powershell
dotnet run -- --test
```

**Expected Output:**
```
╔════════════════════════════════════════════════════════════╗
║   SECURE ASSESSMENT CLIENT - DETECTION PIPELINE TEST       ║
║                   Backend Validation Harness                ║
╚════════════════════════════════════════════════════════════╝

[STARTUP] Running environment integrity checks...
[✓] Virtualization Check: PASSED
[✓] Debugger/Artifacts Check: PASSED
[✓] Environment Safety: ALL CHECKS PASSED

Detection pipeline initialized successfully
✓ Test Session ID: a1b2c3d4
```

### 3. Select Test Option
```
╔════════════════════════════════════════════════════════════╗
║      REAL DETECTION TESTING CONSOLE - THESIS MODE          ║
╚════════════════════════════════════════════════════════════╝

ENVIRONMENT DETECTION:
  1. [ENV]     Re-check Environment Integrity

BEHAVIORAL TRIGGERS:
  2. [ALT-TAB] Alt-Tab Window Switching Test
  3. [CLIP]    Clipboard Activity Test
  4. [IDLE]    Idle Detection Test (70 seconds)
  5. [PROC]    Process Detection Test

MONITORING & STATUS:
  6. [STATUS]  Display System Status
  7. [HISTORY] Show Event History
  8. [ASSESS]  Show Risk Assessments
  9. [HELP]    Show Testing Guide
  0. [EXIT]    Shutdown & Exit

Select option (0-9):
```

---

## 🔍 Test Scenarios

### Test 1: Environment Integrity Check (Option 1)

**What It Does:**
- Re-checks current system for VMs, debuggers, remote access tools
- Uses real registry scanning and process enumeration (Phase 6)
- Reports findings with severity

**How to Use:**
```
1. Press "1" at main menu
2. System immediately scans environment
3. Results displayed with [✓] PASSED or [⚠️] DETECTED
```

**Thesis Value:**
- Demonstrates Phase 6 EnvironmentIntegrityService is functional
- Real registry checks (VirtualBox, Hyper-V, VMware, etc.)
- Real process detection (WinDbg, x64dbg, dnSpy, etc.)
- Generates actual MonitoringEvent objects

---

### Test 2: Alt-Tab Window Switching (Option 2)

**What It Does:**
- Monitors for excessive window switching (Alt+Tab)
- Runs for 10 seconds continuously
- Detects each window change
- Generates event for violations (>5 switches)

**How to Use:**
```
1. Press "2" at main menu
2. Press Enter to start monitoring
3. System displays instructions:
   "Press Alt+Tab to switch windows 5-6 times quickly"
4. Perform window switching with Alt+Tab
5. Monitor captures each switch
6. System displays detected events and risk assessment
```

**Real Detection Flow:**
1. **Phase 6/7:** BehavioralMonitoringService.DetectWindowFocus() detects change
2. **Phase 7:** Creates MonitoringEvent with WINDOW_SWITCH type
3. **Phase 8:** DecisionEngineService assesses severity (Passive, 2/3 score)
4. **Phase 9:** EventLoggerService batches and queues for transmission

**Example Output:**
```
[MONITORING] Watching for window switching... (monitoring for 10 seconds)
Switch windows now with Alt+Tab!

Detected 6 window switch events!
[EVENT] WINDOW_SWITCH
  Type: WINDOW_SWITCH
  Violation: Passive (Severity: 2/3)
  Details: Excessive window switching detected (6 switches)
[ASSESSMENT] Risk: Suspicious (Score: 45/100)
  Action: monitor
  Rationale: 6 aggressive window switches detected
  → Logged for transmission (Pending: 1)
```

**Thesis Notes:**
- Shows real-time detection of actual user behavior
- Demonstrates windowing API integration
- Risk score escalates with multiple violations
- Event properly categorized and transmitted

---

### Test 3: Clipboard Activity Detection (Option 3)

**What It Does:**
- Monitors clipboard for copy/paste operations
- Runs for 15 seconds
- Detects clipboard sequence number changes
- Each access is logged as violation

**How to Use:**
```
1. Press "3" at main menu
2. Press Enter to start monitoring
3. System displays:
   "Copy text from any application (Ctrl+C)"
   "Paste text somewhere (Ctrl+V)"
4. Perform 3-4 copy/paste operations
5. Monitor captures each clipboard access
6. System displays detected events
```

**Real Detection Flow:**
1. **Phase 7:** GetClipboardSequenceNumber() tracks changes
2. **Phase 7:** DetectClipboardActivity() detects each change
3. **Phase 8:** Each access scored as Passive violation (2/3)
4. **Phase 9:** Batch transmitted with other events

**Example Output:**
```
[MONITORING] Watching for clipboard activity... (15 seconds)
Copy and paste text now!

✓ Detected 3 clipboard access events!
[EVENT] CLIPBOARD_ACCESS
  Type: CLIPBOARD_COPY
  Violation: Passive (Severity: 2/3)
  Details: Clipboard activity detected: content copied/accessed
[ASSESSMENT] Risk: Safe (Score: 32/100)
```

**Thesis Notes:**
- Windows API-based detection (privacy-preserving)
- Doesn't read clipboard content, only detects changes
- Suitable for exam proctoring (legitimate exam notes allowed)
- Multiple accesses indicate potential cheating (cross-referencing)

---

### Test 4: Idle Detection Test (Option 4)

**What It Does:**
- Monitors for prolonged inactivity (no keyboard/mouse)
- Runs for 70 seconds (threshold is 60s)
- Detects when idle time exceeds threshold
- Logs as Passive violation

**How to Use:**
```
1. Press "4" at main menu
2. Press Enter to start monitoring
3. System displays:
   "DON'T move mouse or press keys for 70 seconds"
4. REMAIN COMPLETELY INACTIVE for test duration
5. System counts down every 10 seconds
6. After 70 seconds, displays idle detection results
```

**Real Detection Flow:**
1. **Phase 7:** Tracks last activity time
2. **Phase 7:** DetectIdleActivity() checks duration threshold
3. **Phase 7:** Generates event when idle > 60 seconds
4. **Phase 8:** Risk assessed (Passive, low severity = 1/3)
5. **Phase 9:** Logged with session context

**Example Output:**
```
[MONITORING] Watching for idle activity...
Please remain inactive (no mouse/keyboard) for 70 seconds!

Countdown:
  70 seconds remaining...
  60 seconds remaining...
  ...
  5 seconds remaining...

✓ Detected 1 idle period event!
[EVENT] IDLE_DETECTED
  Type: IDLE
  Violation: Passive (Severity: 1/3)
  Details: Student idle for 70 seconds (threshold: 60s)
[ASSESSMENT] Risk: Safe (Score: 10/100)
```

**Thesis Notes:**
- Demonstrates time-based monitoring
- Useful for detecting disengagement
- Risk escalates if multiple idle events occur
- Legitimate activity pauses don't trigger (activity = reset)

---

### Test 5: Process Detection Test (Option 5)

**What It Does:**
- Monitors for unauthorized processes (Discord, Slack, TeamViewer, etc.)
- Runs for 30 seconds
- Detects 30+ blacklisted applications
- Logs as Aggressive violation (highest severity)

**How to Use:**
```
1. Press "5" at main menu
2. Press Enter to start monitoring
3. System displays blacklisted processes:
   • Discord.exe
   • Slack.exe
   • TeamViewer.exe
   • AnyDesk.exe
   • (and 20+ others)
4. Launch one of the detected applications
5. System detects within 1-2 seconds
6. Displays event with aggressive violation
```

**Real Detection Flow:**
1. **Phase 7:** Process.GetProcessesByName() scans for 30+ apps
2. **Phase 7:** Verifies not from System32 (legitimate instance)
3. **Phase 7:** Generates AGGRESSIVE violation event (3/3 severity)
4. **Phase 8:** Risk immediately escalates to "Suspicious" or "Cheating"
5. **Phase 9:** Flagged for immediate transmission + local logging

**Example Output:**
```
[MONITORING] Watching for unauthorized processes...
Launch a suspicious process now!

⚠️ DETECTED 1 suspicious process(es)!
[EVENT] PROCESS_DETECTED
  Type: PROCESS_DETECTED
  Violation: Aggressive (Severity: 3/3)
  Details: Unauthorized process detected: discord.exe
[ASSESSMENT] Risk: Suspicious (Score: 75/100)
  Action: flag_for_review
  Rationale: Aggressive violation detected (3/3 severity)
  → Logged for transmission (Pending: 1)
```

**Thesis Notes:**
- Demonstrates real-time process enumeration
- Aggressive violations = immediate escalation
- Risk score jumps significantly (45+ points)
- Multiple aggressive events → "Cheating" classification
- Perfect demonstration of detection pipeline impact

---

## 📊 Status & History Display

### System Status (Option 6)
```
[STATUS] System Status

Detection Pipeline Status:
  Session ID: a1b2c3d4
  Running: True
  Behavioral Monitoring: ACTIVE

Event History:
  Total Events: 5
  Total Assessments: 5

Decision Engine Statistics:
  Total Events Analyzed: 5
  Aggressive Violations: 1
  Passive Violations: 4
  Session Duration: 120s

Event Logger Statistics:
  Assessments Logged: 5
  Pending (Buffer): 0
  Batches Created: 1
  Batches Transmitted: 0
  Failed Batches: 0

SignalR Connection Status:
  Connected: NO
  Hub URL: http://localhost:5000/exam-hub
```

### Event History (Option 7)
Shows all MonitoringEvent objects detected:
- Timestamp
- Event type
- Violation severity
- Details

### Risk Assessments (Option 8)
Shows RiskAssessment objects:
- Risk level (Safe/Suspicious/Cheating)
- Risk score (0-100)
- Recommendation
- Rationale

---

## 🎯 Workflow for Thesis Validation

### Scenario A: Single Violation
```
1. Choose environment test (Option 1)
   → Shows clean or detected status
2. Choose Alt-Tab test (Option 2)
   → Perform 5-6 tab switches
   → System detects and logs
3. View STATUS (Option 6)
   → Shows 1 event detected
   → Risk: Safe (score ~45)
4. View HISTORY (Option 7)
   → Shows WINDOW_SWITCH event
5. View ASSESSMENTS (Option 8)
   → Shows risk analysis
```

### Scenario B: Multiple Violations (Escalation)
```
1. Alt-Tab test (Option 2) → 6 switches → Risk: Suspicious
2. Then Clipboard test (Option 3) → 4 accesses → Risk: Suspicious
3. Then Process test (Option 5) → Discord detected → Risk: Cheating
4. View STATUS → Aggressive violations detected
5. System recommends: "flag_for_review"
```

### Scenario C: Complete Session (30 minutes)
```
- Run environment check at start
- Keep console running
- Perform various behaviors
- Monitor risk escalation
- View complete event history
- Demonstrate to thesis advisor
```

---

## 🔧 Technical Details

### Detection Services Used
- **Phase 6:** EnvironmentIntegrityService (real registry/process scanning)
- **Phase 7:** BehavioralMonitoringService (real window/clipboard/idle/process monitoring)
- **Phase 8:** DecisionEngineService (risk scoring and classification)
- **Phase 9:** EventLoggerService (batch creation and persistence)

### Event Pipeline
```
Detection Event Created
    ↓
ProcessDetectionEvent() called
    ↓
MonitoringEvent logged to history
    ↓
DecisionEngineService.AssessEvent()
    ↓
RiskAssessment generated
    ↓
EventLoggerService.LogAssessment()
    ↓
Batch queued (max 10 events or 5 seconds)
    ↓
Ready for SignalR transmission
```

### Configuration
- **Idle Threshold:** 60 seconds
- **Window Switch Threshold:** >5 switches to trigger
- **Clipboard Check Interval:** 500ms
- **Process Scan Interval:** Per trigger (Option 5)
- **Batch Size:** 10 events or 5 seconds (whichever first)

---

## 🎓 For Thesis Demonstration

### What to Show Advisor
1. **Environment Detection**
   - "System is not running in VM"
   - "No debuggers detected"
   - Real registry checks

2. **Behavioral Monitoring**
   - Alt-Tab test: "6 switches detected → Risk: Suspicious"
   - Clipboard test: "3 accesses detected → Risk increases"
   - Process test: "Discord detected → Risk: Cheating"

3. **Decision Engine**
   - Risk scoring algorithm working
   - Escalation logic (Safe → Suspicious → Cheating)
   - Each event contributes to overall risk

4. **Event Logging**
   - Events batched and queued
   - Session ID tracking
   - Ready for server transmission

### Key Talking Points
- "Real-time detection of actual user behavior"
- "Complete event pipeline from detection to logging"
- "Extensible to server-side analysis via SignalR"
- "Scalable to N students in exam session"

---

## ⚙️ Troubleshooting

### Issue: No events detected during Alt-Tab test
**Solution:** Make sure to press Alt+Tab rapidly (5-6 times) during the 10-second window

### Issue: Idle test doesn't detect after 70 seconds
**Solution:** Even tiny mouse movements reset the idle timer. Keep hands completely away from keyboard/trackpad

### Issue: Process test doesn't detect Discord
**Solution:** 
- Make sure Discord.exe is running (check Task Manager)
- Verify it's not running from System32 (should be from AppData/Local)

### Issue: "SignalR connection failed" message
**Solution:** This is expected - testing harness works offline. Events are logged locally and queued for when server is available

---

## 📈 Next Steps

### After Testing
1. **Document Results**
   - Screenshot system status
   - Record event history
   - Note risk escalation patterns

2. **Server Integration** (when ready)
   - Set up SignalR hub on exam server
   - Update Hub URL in configuration
   - Enable real-time event transmission

3. **UI Integration** (Phase 4/10)
   - Co-developer creates UI components
   - Uses these services for real student proctoring
   - Real student event data flows through same pipeline

---

## 📝 Session Log Example

```
[STARTUP] Running environment integrity checks...
✓ Environment Safety: ALL CHECKS PASSED

[ENV RECHECK] User selected environment check
✓ Virtualization Check: PASSED
✓ Debugger/Artifacts Check: PASSED

[ALT-TAB TEST] User selected window switching test
→ Monitoring for 10 seconds...
✓ Detected 6 window switch events!
→ Risk assessment: Suspicious (45/100)

[HISTORY] User viewed event history
Total events: 6
Total assessments: 6

[STATUS] User viewed system status
Aggressive violations: 0
Passive violations: 6
Pending assessments: 1

[EXIT] User selected exit
→ Session closed, logs preserved
```

---

## ✅ Verification Checklist

Before thesis submission, verify:

- [ ] Environment check runs at startup
- [ ] Alt-Tab test detects window switches
- [ ] Clipboard test detects copy/paste
- [ ] Idle test detects inactivity
- [ ] Process test detects unauthorized apps
- [ ] Risk scores calculate correctly
- [ ] Events flow through complete pipeline
- [ ] System status displays accurate data
- [ ] Event history preserved
- [ ] Build compiles with zero errors

---

**Ready to test?** Run `dotnet run -- --test` and select option 1!

