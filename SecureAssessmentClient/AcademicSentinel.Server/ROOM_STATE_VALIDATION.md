# 🔐 Room State Validation & Workflow Constraints

## Overview

The AcademicSentinel PSS enforces strict room state validation to ensure proper workflow execution and prevent unauthorized access. Students cannot join or enroll in rooms unless the room is in an appropriate state.

---

## Room State Lifecycle

```
Pending → Countdown → Active → Ended
```

### State Definitions

| State | Description | Student Actions Allowed |
|-------|-------------|------------------------|
| **Pending** | Room created, awaiting teacher to start. Enrollment open. | ✅ Enroll by code or manual assignment |
| **Countdown** | Teacher initiated countdown before exam. | ⏳ Enrolled, but cannot join yet |
| **Active** | Exam is live and students can participate. | ✅ Join live exam and submit events |
| **Ended** | Exam concluded. No further participation. | ❌ No actions allowed |

---

## Validation Rules

### Rule 1: Enrollment (Code-Based or Manual)
**Endpoint:** `POST /api/rooms/enroll`  
**Allowed States:** `Pending` only

**Behavior:**
- ✅ Students CAN enroll when room is `Pending`
- ❌ Students CANNOT enroll when room is `Countdown`, `Active`, or `Ended`
- **Error Response:** 400 Bad Request
  ```json
  {
    "error": "Cannot enroll in a room that is not open for enrollment. The instructor has either not started or already ended this session."
  }
  ```

**Why:** Enrollment establishes the student roster. It must complete before the session starts.

---

### Rule 2: Manual Assignment
**Endpoint:** `POST /api/rooms/{sessionId}/assign`  
**Allowed States:** `Pending` only

**Behavior:**
- ✅ Instructors CAN assign students when room is `Pending`
- ❌ Instructors CANNOT assign students when room is `Countdown`, `Active`, or `Ended`
- **Error Response:** 400 Bad Request
  ```json
  {
    "error": "Cannot assign students to a room that has already started."
  }
  ```

**Why:** Roster changes must complete before the exam begins.

---

### Rule 3: Join Live Exam (SignalR)
**Method:** `JoinLiveExam(roomId)` in MonitoringHub  
**Allowed States:** `Active` only

**Behavior:**
- ✅ Students CAN join only when room is `Active`
- ❌ Students CANNOT join when room is `Pending`, `Countdown`, or `Ended`
- **Error Response:** SignalR `JoinFailed` message
  ```json
  {
    "error": "Cannot join room: the instructor has not started the session or has ended it."
  }
  ```

**Why:** Live exam participation only occurs during the active session window.

---

## Complete Join Workflow

### Step 1: Enrollment Phase (Room = Pending)
```
Student → POST /api/rooms/enroll (with code)
    ↓
PSS checks: room.Status == "Pending" ✓
    ↓
Enrollment recorded
    ↓
Student is now "Enrolled" but NOT "Participating"
```

### Step 2: Room Countdown (Room = Countdown)
```
Instructor → PUT /api/rooms/{id}/status (change to Countdown)
    ↓
PSS broadcasts: SessionCountdownStarted
    ↓
Student UI shows countdown timer
    ↓
Student is enrolled but cannot join yet
```

### Step 3: Exam Active (Room = Active)
```
Instructor → PUT /api/rooms/{id}/status (change to Active)
    ↓
PSS broadcasts: SessionStarted
    ↓
Student → SignalR JoinLiveExam(roomId)
    ↓
PSS checks: room.Status == "Active" ✓
    ↓
Student joins SignalR group
    ↓
SessionParticipant record created
    ↓
Student is now "Participating"
```

### Step 4: Exam Ended (Room = Ended)
```
Instructor → PUT /api/rooms/{id}/status (change to Ended)
    ↓
PSS broadcasts: SessionEnded
    ↓
Students cannot perform any more actions
    ↓
Room moves to history
```

---

## Error Scenarios

### Scenario 1: Student Tries to Enroll After Exam Started
```
Room Status: Active
Action: Student calls POST /api/rooms/enroll
Result: 400 Bad Request
Message: "Cannot enroll in a room that is not open for enrollment..."
```

### Scenario 2: Student Tries to Join During Countdown
```
Room Status: Countdown
Action: Student calls SignalR JoinLiveExam()
Result: JoinFailed event
Message: "Cannot join room: the instructor has not started the session..."
```

### Scenario 3: Student Tries to Join After Exam Ended
```
Room Status: Ended
Action: Student calls SignalR JoinLiveExam()
Result: JoinFailed event
Message: "Cannot join room: the instructor has not started the session..."
```

---

## Implementation Details

### EnrollByCode (RoomsController.cs)
```csharp
// Validation added (line 297-300)
if (room.Status != "Pending")
{
    return BadRequest("Cannot enroll in a room that is not open for enrollment...");
}
```

### AssignStudents (RoomsController.cs)
```csharp
// Existing validation (line 457-460)
if (room.Status != "Pending")
{
    return BadRequest("Cannot assign students to a room that has already started.");
}
```

### JoinLiveExam (MonitoringHub.cs)
```csharp
// Validation added (lines 21-29)
var room = await _context.Rooms.FindAsync(roomId);
if (room == null) return;

if (room.Status != "Active")
{
    await Clients.Caller.SendAsync("JoinFailed", 
        "Cannot join room: the instructor has not started the session or has ended it.");
    return;
}
```

---

## Key Points for Frontend Implementation

### SAC (Student Assessment Client)

1. **Enrollment Screen (Room = Pending)**
   - Show enrollment code input field
   - Allow POST /api/rooms/enroll
   - Store room credentials after successful enrollment

2. **Pre-Exam Screen (Room = Countdown)**
   - Listen for `SessionCountdownStarted` broadcast
   - Display countdown timer
   - Disable "Join" button until `SessionStarted` is received

3. **Exam Screen (Room = Active)**
   - Listen for `SessionStarted` broadcast
   - Call SignalR `JoinLiveExam(roomId)`
   - Handle `JoinFailed` event and display error message
   - Start monitoring after successful join

4. **Post-Exam (Room = Ended)**
   - Listen for `SessionEnded` broadcast
   - Stop monitoring and disconnect
   - Display "Session Ended" message

### IMC (Instructor Monitoring Console)

1. **Room Setup (Room = Pending)**
   - Display "Open for Enrollment"
   - Show generated enrollment code
   - Allow manual student assignment
   - Show participant list

2. **Countdown Initiation (Room → Countdown)**
   - Button: "Start Countdown"
   - Broadcasts countdown signal
   - Participants see countdown timer on SAC

3. **Exam Start (Room → Active)**
   - Button: "Begin Exam"
   - Starts live monitoring
   - Students can now join with JoinLiveExam()
   - Real-time violation alerts begin

4. **Exam End (Room → Ended)**
   - Button: "End Exam"
   - Stops live monitoring
   - Archives session data

---

## Security Implications

This validation prevents:

1. **Unauthorized participation** - Students cannot bypass the enrollment phase
2. **Premature access** - Students cannot access exam before instructor initiates
3. **Post-exam tampering** - Cannot submit events or join after exam ends
4. **Roster modification during exam** - Cannot add/remove students mid-exam

---

## Testing Checklist

- [ ] Enroll in Pending room: Should succeed
- [ ] Enroll in Active room: Should fail with 400
- [ ] Enroll in Ended room: Should fail with 400
- [ ] Join in Active room: Should succeed
- [ ] Join in Pending room: Should fail with JoinFailed
- [ ] Join in Countdown room: Should fail with JoinFailed
- [ ] Join in Ended room: Should fail with JoinFailed
- [ ] Assign students in Pending room: Should succeed
- [ ] Assign students in Active room: Should fail with 400

---

## Related Documentation

- [API_QUICK_REFERENCE.md](API_QUICK_REFERENCE.md) - All endpoint specifications
- [IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md) - Database schema and models
- [README.md](README.md) - Setup and configuration
