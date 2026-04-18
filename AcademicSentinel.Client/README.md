# AcademicSentinel.Client Documentation

## Overview
AcademicSentinel.Client is a WPF Instructor Monitoring Console (IMC) application for managing rooms and monitoring live exam sessions.

- **Framework:** WPF on `.NET 10` (`net10.0-windows`)
- **UI library:** MaterialDesignThemes
- **Realtime client:** `Microsoft.AspNetCore.SignalR.Client`

## Current Scope
The current desktop flow is instructor-focused:
- Instructor registration/login
- Profile image upload
- Room creation and deletion
- Student assignment/removal
- Session setup and live monitoring UI

Student login/usage is intentionally limited in current UI flow.

## Project Structure
- `Views/Shared/` - Login, Register, Help windows
- `Views/IMC/` - Teacher dashboard, room details, student list, live monitoring
- `Services/AuthService.cs` - login/register API calls
- `Services/SessionManager.cs` - in-memory JWT and current user state
- `Constants/ApiEndpoints.cs` - backend base URL and endpoint constants

## Backend Dependency
The client calls `AcademicSentinel.Server` APIs.

Update `ApiEndpoints.BaseUrl` if needed:
- `AcademicSentinel.Client/Constants/ApiEndpoints.cs`

Default is:
- `https://localhost:7123`

## Run the Client
From `AcademicSentinel.Client`:

1. `dotnet restore`
2. `dotnet run`

Make sure the server is running first and JWT configuration is valid.

## Key Windows and Flows
### Login/Register
- `Views/Shared/LoginWindow.xaml.cs`
- `Views/Shared/RegisterWindow.xaml.cs`

`AuthService` calls:
- `POST /api/auth/login`
- `POST /api/auth/register`

On successful login, JWT and user data are saved in `SessionManager`.

### Teacher Dashboard
- `Views/IMC/Teacherdashboard.xaml.cs`

Main actions:
- Load instructor rooms (`GET /api/rooms/instructor`)
- Upload profile image (`POST /api/images/profile`)
- Create room (`POST /api/rooms`)
- Delete room (`DELETE /api/rooms/{id}`)

### Room Detail / Student Management
- `Views/IMC/RoomDetailWindow.xaml.cs`
- `Views/IMC/StudentListWindow.xaml.cs`
- `Views/IMC/AddStudentDialog.xaml.cs`

Main actions:
- Load room history (`GET /api/rooms/{roomId}/history`)
- View participants (`GET /api/rooms/{roomId}/participants`)
- Add student by email (`POST /api/rooms/{roomId}/enroll-email`)
- Remove student (`DELETE /api/rooms/{roomId}/unenroll/{studentId}`)

### Live Session Monitoring
- `Views/IMC/CreateSessionSetupWindow.xaml.cs`
- `Views/IMC/LiveSessionMonitoringWindow.xaml.cs`

Main actions:
- Save detection settings (`POST /api/rooms/{roomId}/settings`)
- Start session (`POST /api/rooms/{roomId}/start-session`)
- End session (`PUT /api/rooms/sessions/{sessionId}/end`)
- Connect to SignalR hub (`/monitoringHub`)

## Notes
- Session state and identity are managed in-memory (`SessionManager`), so data resets when app closes.
- Most protected calls require a valid JWT set in `Authorization: Bearer <token>`.
