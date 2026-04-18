# Idle Detection Test - Quick Reference

## 🚀 Running the Comprehensive Idle Detection Test

### Quick Start
1. Build the project: `dotnet build`
2. Run: `dotnet run -- --test`
3. Press **`C`** at menu for Comprehensive Idle Detection Test
4. Follow on-screen instructions (remain idle for 10 seconds)

### What to Expect

**Timeline:**
- **0-10 seconds:** Idle monitoring (no mouse/keyboard input)
- **Results:** Event created, assessed, batched, and transmission status shown

**Verdict (What Success Looks Like):**
```
✅ COMPREHENSIVE IDLE DETECTION TEST: PASSED
   ✓ Idle activity detected
   ✓ Events created with correct type and severity
   ✓ Risk assessments generated
   ✓ Batches created and queued for transmission
```

---

## 📊 What the Test Verifies

### Phase 3 Verification Checklist

| Component | Test Check | Expected Result |
|-----------|-----------|-----------------|
| **Event Creation** | DetectIdleActivity() | ✓ IDLE event generated |
| **Event Properties** | Type, Severity, Details | ✓ IDLE / Passive / 1/3 |
| **Risk Scoring** | AssessEvent() | ✓ 5/100 (Safe level) |
| **Assessment** | Decision Engine | ✓ "none" recommended action |
| **Batching** | EventLogger buffering | ✓ Batch created |
| **Transmission** | SignalR queue | ✓ Status shown (transmitted/pending) |
| **Persistence** | Local storage | ✓ Batch JSON saved |

---

## 🔍 Interpreting Results

### Success Indicators

```
✓ IDLE events detected: 1
  • Event Type: IDLE
  • Violation Type: Passive
  • Severity: 1/3
  • Details: Student idle for 10 seconds (threshold: 10s)
```
**Means:** System successfully detected inactivity and created monitoring event.

```
✓ Risk assessments generated: 1
  [Safe]
    Risk Score: 5/100
    Recommended Action: none
```
**Means:** Decision Engine analyzed the event and assigned it a low-risk classification.

```
✓ Event batches created: 1
  • Assessments: 1
  • Status: transmitted
```
**Means:** EventLogger successfully batched and transmitted the event to server.

### Possible Status Values

- **transmitted:** ✅ Batch successfully sent to server
- **pending:** ⚠️ Waiting for flush interval or buffer to fill
- **failed:** ❌ Transmission failed; in retry queue
- **acknowledged:** ✅ Server confirmed receipt

---

## 🛠️ Troubleshooting

### Problem: No idle events detected

**Check:**
1. Are you staying idle? (no mouse movement, no keyboard input)
2. Is EnableIdleDetection = true? (should be in test setup)
3. Is IdleThresholdSeconds = 10? (for testing)

**Solution:**
- Run test again, being very careful not to move mouse
- Check logs in: `%APPDATA%\SecureAssessmentClient\Logs\`

### Problem: Events created but risk score is low

**Expected Behavior:**
- First idle event = 5 points (Safe)
- Pattern detected (3+ events) = escalates to Suspicious

**Solution:**
- Run multiple tests to see pattern escalation
- Check DecisionEngine scoring in code

### Problem: Batch shows "pending" not "transmitted"

**Possible Causes:**
- Server (SignalR hub) offline
- Hub URL incorrect
- First batch hasn't flushed yet

**Solution:**
1. Use menu option "9. [SERVER]" to test connection
2. Wait 5+ seconds for auto-flush
3. Use menu option "6. [STATUS]" to see batch queue

---

## 📝 Logging & Debugging

### View Logs
- **Main logs:** `%APPDATA%\SecureAssessmentClient\Logs\`
- **Event batches:** `%APPDATA%\SecureAssessmentClient\EventLogs\{SessionId}\`

### Key Log Entries to Look For
```
[BEHAVIOR] Idle activity detected: 10.X seconds of inactivity
[DECISION] Risk Assessment: Safe (Score: 5, Event: IDLE)
[LOGGER] Assessment logged: Safe (Score: 5)
[LOGGER] Batch created: 12345... with 1 assessments
[LOGGER] Batch 12345... transmitted successfully
```

### Examine Persisted Batch
```json
{
  "BatchId": "12345678-90ab-cdef-1234-567890abcdef",
  "SessionId": "a1b2c3d4",
  "CreatedAt": "2024-01-15T14:32:45.1234567Z",
  "Assessments": [
    {
      "RiskScore": 5,
      "RiskLevel": "Safe",
      "RecommendedAction": "none"
    }
  ],
  "Status": "transmitted"
}
```

---

## 🎯 Integration Testing Flow

### Test Sequence
1. **Setup:** Initialize test runner with idle threshold = 10s
2. **Monitor:** Wait 10 seconds idle (no input)
3. **Detect:** System generates IDLE event
4. **Assess:** Decision engine scores it (5 points, Safe)
5. **Batch:** EventLogger queues the assessment
6. **Transmit:** SignalR sends to server
7. **Verify:** Confirm all 6 steps completed

### Full Audit Trail Example
```
[14:32:45.000] User becomes idle
[14:32:55.000] DetectIdleActivity() → MonitoringEvent created
[14:32:55.001] AssessEvent() → RiskAssessment generated (5 pts, Safe)
[14:32:55.002] LogAssessment() → Assessment queued
[14:33:00.000] FlushTimer → Batch created
[14:33:00.500] TransmitBatchAsync() → SignalR sends
[14:33:00.700] Status: transmitted ✓
```

---

## 📋 Test Requirements

### System Requirements
- .NET 9 runtime
- Windows OS (for mouse/keyboard monitoring via P/Invoke)
- Keyboard and mouse available

### Configuration
- IdleThresholdSeconds: 10 (for testing)
- EnableIdleDetection: true
- Monitoring service must be running

### Test Duration
- Full test: ~12 seconds
- Idle period: 10 seconds
- Results processing: 2 seconds

---

## ✅ Sign-Off Checklist

After running the comprehensive idle detection test:

- [ ] Test completed without errors
- [ ] Idle events detected (count ≥ 1)
- [ ] Risk assessments generated (count ≥ 1)
- [ ] Batches created successfully
- [ ] Transmission status visible
- [ ] Verdict shows "PASSED"
- [ ] Logs saved to %APPDATA%\SecureAssessmentClient\Logs\
- [ ] Batch JSON file exists in EventLogs folder

---

## 📞 Support

For issues or questions:
1. Review detailed rebuild summary: `IDLE_DETECTION_REBUILD_SUMMARY.md`
2. Check main testing guide: `REAL_DETECTION_TESTING_GUIDE.md`
3. Examine logs in %APPDATA%\SecureAssessmentClient\
4. Review code comments in BehavioralMonitoringService.cs

---

**Status:** ✅ Ready for Thesis Testing  
**Last Updated:** [Rebuild Date]
