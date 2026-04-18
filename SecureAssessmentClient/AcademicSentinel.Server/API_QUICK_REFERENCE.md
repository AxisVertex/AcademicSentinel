# API Quick Reference Guide

## Base URL
```
https://localhost:[port]/api
```

## Authentication

### Register
```
POST /auth/register
Content-Type: application/json

{
  "email": "student@example.com",
  "password": "SecurePass123!",
  "role": "Student"  // or "Instructor"
}

Response (201):
{
  "id": 1,
  "email": "student@example.com",
  "role": "Student",
  "token": "eyJhbGciOiJIUzI1NiIs..."
}
```

### Login
```
POST /auth/login
Content-Type: application/json

{
  "email": "student@example.com",
  "password": "SecurePass123!"
}

Response (200):
{
  "id": 1,
  "email": "student@example.com",
  "role": "Student",
  "token": "eyJhbGciOiJIUzI1NiIs..."
}
```

---

## Room Management

### Create Room (Instructor Only)
```
POST /rooms
Authorization: Bearer {token}
Content-Type: application/json

{
  "subjectName": "Biology 101"
}

Response (201):
{
  "id": 1,
  "subjectName": "Biology 101",
  "instructorId": 1,
  "status": "Pending",
  "enrollmentCode": null,
  "codeExpiry": null,
  "createdAt": "2026-01-15T10:30:00Z"
}
```

### Get Instructor's Rooms
```
GET /rooms/instructor
Authorization: Bearer {token}

Response (200):
[
  {
    "id": 1,
    "subjectName": "Biology 101",
    "instructorId": 1,
    "status": "Pending",
    "enrollmentCode": null,
    "createdAt": "2026-01-15T10:30:00Z"
  }
]
```

### Get Student's Rooms
```
GET /rooms/my
Authorization: Bearer {token}

Response (200):
[
  {
    "id": 1,
    "subjectName": "Biology 101",
    "instructorId": 1,
    "status": "Active",
    "enrollmentCode": null,
    "createdAt": "2026-01-15T10:30:00Z"
  }
]
```

### Get Room Details
```
GET /rooms/{roomId}
Authorization: Bearer {token}

Response (200):
{
  "id": 1,
  "subjectName": "Biology 101",
  "instructorId": 1,
  "status": "Active",
  "enrollmentCode": null,
  "createdAt": "2026-01-15T10:30:00Z"
}
```

### Get Room Status
```
GET /rooms/{roomId}/status
Authorization: Bearer {token}

Response (200):
{
  "roomId": 1,
  "subjectName": "Biology 101",
  "status": "Active",
  "createdAt": "2026-01-15T10:30:00Z",
  "totalEnrolled": 25,
  "totalJoined": 23,
  "totalNotJoined": 2
}
```

---

## Room Control

### Update Room Status (Instructor Only)
```
PUT /rooms/{roomId}/status
Authorization: Bearer {token}
Content-Type: application/json

{
  "status": "Countdown"  // or "Active", "Ended"
}

Response (200):
{
  "message": "Room status successfully updated to Countdown",
  "roomId": 1
}
```

---

## Student Assignment

### Assign Students Manually (Instructor Only)
```
POST /rooms/{roomId}/assign
Authorization: Bearer {token}
Content-Type: application/json

{
  "studentIds": [2, 3, 4]
}

Response (200):
{
  "message": "Successfully assigned 3 students."
}
```

### Get Room Participants (Instructor Only)
```
GET /rooms/{roomId}/participants
Authorization: Bearer {token}

Response (200):
[
  {
    "studentId": 2,
    "studentEmail": "student1@example.com",
    "enrollmentSource": "Manual",
    "enrolledAt": "2026-01-15T10:35:00Z",
    "joinedAt": "2026-01-15T10:40:00Z",
    "participationStatus": "Joined",
    "connectionStatus": "Connected",
    "disconnectedAt": null
  },
  {
    "studentId": 3,
    "studentEmail": "student2@example.com",
    "enrollmentSource": "Code",
    "enrolledAt": "2026-01-15T10:36:00Z",
    "joinedAt": null,
    "participationStatus": "NotJoined",
    "connectionStatus": "Disconnected",
    "disconnectedAt": null
  }
]
```

### Get Enrolled Students
```
GET /rooms/{roomId}/students
Authorization: Bearer {token}

Response (200):
[
  {
    "id": 1,
    "roomId": 1,
    "studentId": 2,
    "enrollmentSource": "Manual",
    "enrolledAt": "2026-01-15T10:35:00Z"
  }
]
```

---

## Enrollment Codes

### Generate Enrollment Code (Instructor Only)
```
POST /rooms/{roomId}/generate-code
Authorization: Bearer {token}

Response (200):
{
  "message": "Code generated successfully.",
  "enrollmentCode": "ABC123"
}
```

### Enroll Using Code (Student)
```
POST /rooms/enroll
Authorization: Bearer {token}
Content-Type: application/json

{
  "enrollmentCode": "ABC123"
}

Response (200):
{
  "message": "Successfully enrolled using the code.",
  "roomId": 1
}
```

---

## Detection Settings

### Get Detection Settings
```
GET /rooms/{roomId}/settings
Authorization: Bearer {token}

Response (200):
{
  "id": 1,
  "roomId": 1,
  "enableClipboardMonitoring": true,
  "enableProcessDetection": true,
  "enableIdleDetection": true,
  "idleThresholdSeconds": 300,
  "enableFocusDetection": true,
  "enableVirtualizationCheck": true,
  "strictMode": false,
  "createdAt": "2026-01-15T10:30:00Z"
}
```

### Update Detection Settings (Instructor Only)
```
PUT /rooms/{roomId}/settings
Authorization: Bearer {token}
Content-Type: application/json

{
  "enableClipboardMonitoring": true,
  "enableProcessDetection": true,
  "enableIdleDetection": true,
  "idleThresholdSeconds": 300,
  "enableFocusDetection": true,
  "enableVirtualizationCheck": true,
  "strictMode": false
}

Response (200):
{
  "message": "Settings saved successfully."
}
```

---

## Reports

### Get Full Room Report (Instructor Only)
```
GET /api/reports/room/{roomId}
Authorization: Bearer {token}

Response (200):
{
  "roomId": 1,
  "subjectName": "Biology 101",
  "status": "Ended",
  "createdAt": "2026-01-15T10:30:00Z",
  "totalParticipants": 23,
  "studentSummaries": [
    {
      "studentId": 2,
      "email": "student1@example.com",
      "connectionStatus": "Disconnected",
      "totalViolations": 2,
      "totalSeverityScore": 35,
      "riskLevel": "Suspicious",
      "joinedAt": "2026-01-15T10:40:00Z",
      "disconnectedAt": "2026-01-15T11:30:00Z"
    }
  ]
}
```

### Get Student Report (Instructor Only)
```
GET /api/reports/student/{roomId}/{studentId}
Authorization: Bearer {token}

Response (200):
{
  "studentId": 2,
  "studentEmail": "student1@example.com",
  "roomId": 1,
  "roomSubjectName": "Biology 101",
  "joinedAt": "2026-01-15T10:40:00Z",
  "disconnectedAt": "2026-01-15T11:30:00Z",
  "participationStatus": "Joined",
  "connectionStatus": "Disconnected",
  "totalViolations": 2,
  "totalSeverityScore": 35,
  "riskLevel": "Suspicious",
  "violationTimeline": [
    {
      "violationId": 1,
      "eventType": "RTFM",
      "description": "Alt-Tab detected",
      "severityLevel": "S2",
      "timestamp": "2026-01-15T10:45:00Z"
    },
    {
      "violationId": 2,
      "eventType": "CLIPBOARD",
      "description": "Copy activity detected",
      "severityLevel": "S1",
      "timestamp": "2026-01-15T11:00:00Z"
    }
  ]
}
```

---

## Violations

### Report Violation (SAC - can be public endpoint)
```
POST /violations
Content-Type: application/json

{
  "roomId": 1,
  "studentEmail": "student1@example.com",
  "module": "RTFM",
  "description": "Alt-Tab detected",
  "severityLevel": "S2"
}

Response (201):
{
  "id": 1,
  "roomId": 1,
  "studentEmail": "student1@example.com",
  "module": "RTFM",
  "description": "Alt-Tab detected",
  "severityLevel": "S2",
  "timestamp": "2026-01-15T10:45:00Z"
}
```

### Get Room Violations (Instructor Only)
```
GET /violations/room/{roomId}
Authorization: Bearer {token}

Response (200):
[
  {
    "id": 1,
    "roomId": 1,
    "studentEmail": "student1@example.com",
    "module": "RTFM",
    "description": "Alt-Tab detected",
    "severityLevel": "S2",
    "timestamp": "2026-01-15T10:45:00Z"
  }
]
```

---

## SignalR Hub (/hubs/room)

### Connection
```javascript
// Connect to hub with JWT token
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/room?access_token=" + token)
    .withAutomaticReconnect()
    .build();

connection.start();
```

### Join Live Exam (SAC)
```javascript
// Student calls this after authentication
connection.invoke("JoinLiveExam", roomId);
```

### Send Monitoring Event (SAC)
```javascript
// Send monitoring event in real-time
connection.invoke("SendMonitoringEvent", roomId, studentId, {
  eventType: "ALT_TAB",
  severityScore: 20,
  description: "Alt+Tab detected",
  timestamp: new Date()
});
```

### Receive Countdown Started (SAC)
```javascript
connection.on("SessionCountdownStarted", () => {
  // Show countdown timer to student
});
```

### Receive Session Started (SAC)
```javascript
connection.on("SessionStarted", () => {
  // Activate monitoring modules
});
```

### Receive Session Ended (SAC)
```javascript
connection.on("SessionEnded", () => {
  // Stop monitoring and show completion message
});
```

### Receive Status Changed
```javascript
connection.on("SessionStatusChanged", (status) => {
  // status: "Pending", "Countdown", "Active", "Ended"
});
```

### Receive Student Joined (IMC)
```javascript
connection.on("StudentJoined", (studentId) => {
  // Update dashboard to show student joined
});
```

### Receive Student Disconnected (IMC)
```javascript
connection.on("StudentDisconnected", (studentId) => {
  // Update dashboard to show student disconnected
});
```

---

## Image Management

### Upload Profile Image
```
POST /images/profile
Authorization: Bearer {token}
Content-Type: multipart/form-data

Parameters:
  - image: File (JPG, PNG, GIF, WebP, max 5MB)

Response (200):
{
  "success": true,
  "message": "Profile image uploaded successfully.",
  "url": "/images/profiles/user_5_guid123.jpg",
  "contentType": "image/jpeg",
  "sizeBytes": 245600,
  "uploadedAt": "2026-01-15T10:30:00Z"
}
```

### Get Profile Image
```
GET /images/profile/{userId}

Response (200):
  Binary image file with appropriate Content-Type header

Example:
  GET /images/profile/5
  → Returns: user_5_guid123.jpg (binary)
```

### Get Current User Profile (with image URL)
```
GET /images/profile
Authorization: Bearer {token}

Response (200):
{
  "id": 5,
  "email": "student@university.edu",
  "role": "Student",
  "createdAt": "2026-01-10T08:00:00Z",
  "profileImageUrl": "/images/profiles/user_5_guid123.jpg",
  "profileImageUploadedAt": "2026-01-15T10:30:00Z"
}
```

### Delete Profile Image
```
DELETE /images/profile
Authorization: Bearer {token}

Response (200):
{
  "message": "Profile image deleted successfully."
}
```

### Upload Room Image
```
POST /images/room/{roomId}
Authorization: Bearer {token} (Instructor only)
Content-Type: multipart/form-data

Parameters:
  - roomId: int (route parameter)
  - image: File (JPG, PNG, GIF, WebP, max 5MB)

Response (200):
{
  "success": true,
  "message": "Room image uploaded successfully.",
  "url": "/images/rooms/room_3_guid456.png",
  "contentType": "image/png",
  "sizeBytes": 512000,
  "uploadedAt": "2026-01-15T11:45:00Z"
}
```

### Get Room Image
```
GET /images/room/{roomId}

Response (200):
  Binary image file with appropriate Content-Type header

Example:
  GET /images/room/3
  → Returns: room_3_guid456.png (binary)
```

### Get Room Details with Image URL
```
GET /images/room/{roomId}/details
Authorization: Bearer {token}

Response (200):
{
  "id": 3,
  "subjectName": "Advanced Mathematics",
  "instructorId": 2,
  "status": "Active",
  "enrollmentCode": "ABC123",
  "createdAt": "2026-01-10T09:00:00Z",
  "roomImageUrl": "/images/rooms/room_3_guid456.png",
  "roomImageUploadedAt": "2026-01-15T11:45:00Z"
}
```

### Delete Room Image
```
DELETE /images/room/{roomId}
Authorization: Bearer {token} (Instructor only)

Parameters:
  - roomId: int (route parameter)

Response (200):
{
  "message": "Room image deleted successfully."
}
```

---

## Image Upload Requirements

### Allowed File Types
- JPG/JPEG (image/jpeg)
- PNG (image/png)
- GIF (image/gif)
- WebP (image/webp)

### Size Limit
- Maximum: 5 MB

### Error Examples

```
File Too Large:
POST /images/profile
→ 400 Bad Request
{
  "error": "File size exceeds maximum allowed size of 5 MB."
}

Invalid File Type:
POST /images/profile
→ 400 Bad Request
{
  "error": "File type '.pdf' is not allowed. Only JPG, PNG, GIF, and WebP are supported."
}

Unauthorized:
POST /images/room/3
(Not the instructor of this room)
→ 403 Forbidden
```

### Receive Violation Detected (IMC)
```javascript
connection.on("ViolationDetected", (violationData) => {
  // Display real-time violation alert on dashboard
  // violationData: { studentId, eventType, severityScore, timestamp }
});
```

---

## Error Responses

### 401 Unauthorized
```json
{
  "error": "Unauthorized - No token provided or invalid token"
}
```

### 403 Forbidden
```json
{
  "error": "Forbidden - Insufficient permissions"
}
```

### 404 Not Found
```json
{
  "error": "Room not found."
}
```

### 400 Bad Request
```json
{
  "error": "Cannot modify settings after the room has started."
}
```

---

## Status Codes

- `200` - OK
- `201` - Created
- `400` - Bad Request
- `401` - Unauthorized
- `403` - Forbidden
- `404` - Not Found
- `500` - Internal Server Error

---

## Authorization Headers

All authenticated endpoints require:
```
Authorization: Bearer {jwt_token}
```

Where `{jwt_token}` is obtained from the login or register endpoint.

---

## Room Statuses

- `Pending` - Room created, waiting for instructor to start
- `Countdown` - Countdown timer running on student clients
- `Active` - Monitoring is active, assessments ongoing
- `Ended` - Room completed and monitoring stopped

---

## Enrollment Sources

- `Manual` - Student was manually assigned by instructor
- `Code` - Student enrolled using an enrollment code

---

## Risk Levels

- `Safe` - Total severity score < 20
- `Suspicious` - Total severity score between 20 and 49
- `Cheating` - Total severity score >= 50

---

## Severity Levels

- `S1` - Minor violation (10 points)
- `S2` - Moderate violation (20 points)
- `S3` - Severe violation (50 points)

---

Generated: 2026
Version: 1.0
