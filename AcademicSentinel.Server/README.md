# AcademicSentinel.Server Documentation

## Overview
AcademicSentinel.Server is the backend API for AcademicSentinel. It provides authentication, room/session management, monitoring events, reporting, image storage, and real-time updates via SignalR.

- **Framework:** ASP.NET Core (`net10.0`)
- **Database:** SQLite via Entity Framework Core
- **Auth:** JWT Bearer tokens
- **Realtime:** SignalR hub at `/monitoringHub`

## Project Structure
- `Controllers/` - REST API endpoints (`Auth`, `Rooms`, `Violations`, `Reports`, `Images`)
- `Hubs/MonitoringHub.cs` - real-time room join/leave + monitoring events
- `Data/AppDbContext.cs` - EF Core DbContext
- `Models/` - entities (User, Room, SessionParticipant, ViolationLog, etc.)
- `Services/ImageStorageService.cs` - image validation and file operations
- `DTOs/` - API payload contracts

## Configuration
Set these in `appsettings.json` / user secrets:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`

## Run the Server
From `AcademicSentinel.Server`:

1. `dotnet restore`
2. `dotnet ef database update`
3. `dotnet run`

Swagger is available in Development mode.

## Authentication
- `POST /api/auth/register`
- `POST /api/auth/login`

`/api/auth/login` returns a JWT token used in:

`Authorization: Bearer <token>`

## Main API Endpoints
### Rooms
- `POST /api/rooms` (Instructor)
- `GET /api/rooms/{id}`
- `DELETE /api/rooms/{id}` (Instructor)
- `GET /api/rooms/instructor` (Instructor)
- `POST /api/rooms/{roomId}/settings`
- `GET /api/rooms/{roomId}/settings`
- `POST /api/rooms/{roomId}/start-session` (Instructor)
- `PUT /api/rooms/sessions/{sessionId}/end` (Instructor)
- `GET /api/rooms/{roomId}/history`
- `GET /api/rooms/{roomId}/participants` (Instructor)
- `POST /api/rooms/{roomId}/enroll-email` (Instructor)
- `DELETE /api/rooms/{roomId}/unenroll/{studentId}` (Instructor)
- `POST /api/rooms/{roomId}/generate-code` (Instructor)

### Violations
- `POST /api/violations`
- `GET /api/violations/room/{roomId}` (Instructor)

### Reports (Instructor)
- `GET /api/reports/room/{sessionId}`
- `GET /api/reports/student/{sessionId}/{studentId}`

### Images
- `POST /api/images/profile`
- `GET /api/images/profile`
- `GET /api/images/profile/{userId}` (public)
- `DELETE /api/images/profile`
- `POST /api/images/room/{roomId}` (Instructor)
- `GET /api/images/room/{roomId}` (public)
- `GET /api/images/room/{roomId}/details`
- `DELETE /api/images/room/{roomId}` (Instructor)

## SignalR Hub
Endpoint: `/monitoringHub`

Hub methods:
- `JoinRoom(string roomId)` - instructor subscribes to room updates
- `JoinLiveExam(int roomId)` - student joins live room (room must be `Active`)
- `SendMonitoringEvent(int roomId, int studentId, MonitoringEventDto eventData)`

Server broadcasts:
- `StudentJoined`
- `StudentDisconnected`
- `ViolationDetected`
- `SessionStarted`
- `SessionEnded`

## Additional Reference
For request/response examples, see:
- `AcademicSentinel.Server/API_QUICK_REFERENCE.md`
