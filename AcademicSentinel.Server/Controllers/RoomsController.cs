using AcademicSentinel.Server.Data;
using AcademicSentinel.Server.DTOs;
using AcademicSentinel.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using AcademicSentinel.Server.Hubs;
using System.Linq;

namespace AcademicSentinel.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<MonitoringHub> _hubContext;

    public RoomsController(AppDbContext context, IHubContext<MonitoringHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // ==========================================
    // BASIC ROOM MANAGEMENT
    // ==========================================

    [HttpPost]
    [Authorize(Roles = "Instructor")]
    public async Task<ActionResult<Room>> CreateRoom([FromBody] Room roomRequest)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null) return Unauthorized();

        roomRequest.InstructorId = int.Parse(userIdString);
        roomRequest.Status = "Pending";
        // REMOVED the roomRequest.EnrollmentCode = null; line so it actually saves!
        roomRequest.CreatedAt = DateTime.UtcNow;

        _context.Rooms.Add(roomRequest);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRoom), new { id = roomRequest.Id }, roomRequest);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Room>> GetRoom(int id)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room == null) return NotFound("Room not found.");
        return room;
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room == null) return NotFound("Room not found.");

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null) return Unauthorized();
        int instructorId = int.Parse(userIdString);

        if (room.InstructorId != instructorId)
        {
            return StatusCode(403, "You do not have permission to delete this room.");
        }

        var settings = await _context.RoomDetectionSettings.Where(s => s.RoomId == id).ToListAsync();
        var enrollments = await _context.RoomEnrollments.Where(e => e.RoomId == id).ToListAsync();
        var examSessions = await _context.ExamSessions.Where(s => s.RoomId == id).ToListAsync();
        var sessionParticipants = await _context.SessionParticipants.Where(p => p.RoomId == id).ToListAsync();
        var monitoringEvents = await _context.MonitoringEvents.Where(m => m.RoomId == id).ToListAsync();
        var violations = await _context.ViolationLogs.Where(v => v.RoomId == id).ToListAsync();
        var assignments = await _context.SessionAssignments.Where(a => a.RoomId == id).ToListAsync();
        var riskSummaries = await _context.RiskSummaries.Where(r => r.RoomId == id).ToListAsync();

        if (settings.Count > 0) _context.RoomDetectionSettings.RemoveRange(settings);
        if (enrollments.Count > 0) _context.RoomEnrollments.RemoveRange(enrollments);
        if (examSessions.Count > 0) _context.ExamSessions.RemoveRange(examSessions);
        if (sessionParticipants.Count > 0) _context.SessionParticipants.RemoveRange(sessionParticipants);
        if (monitoringEvents.Count > 0) _context.MonitoringEvents.RemoveRange(monitoringEvents);
        if (violations.Count > 0) _context.ViolationLogs.RemoveRange(violations);
        if (assignments.Count > 0) _context.SessionAssignments.RemoveRange(assignments);
        if (riskSummaries.Count > 0) _context.RiskSummaries.RemoveRange(riskSummaries);

        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Room deleted successfully." });
    }

    // ==========================================
    // EXAM SESSION MANAGEMENT (STEP 4)
    // ==========================================

    // POST: api/rooms/{roomId}/start-session
    // This creates a NEW session record every time an exam starts
    [HttpPost("{roomId}/start-session")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> StartExamSession(int roomId, [FromBody] StartSessionDto? request)
    {
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null) return NotFound("Room not found.");

        // Check if there is already an active session for this room
        var activeSession = await _context.ExamSessions
            .FirstOrDefaultAsync(s => s.RoomId == roomId && s.Status == "Active");

        if (activeSession != null)
        {
            return Ok(new { message = "Session already running", sessionId = activeSession.Id });
        }

        var examType = string.IsNullOrWhiteSpace(request?.ExamType) ? "Summative" : request.ExamType;

        var nextSessionNumber = (await _context.ExamSessions
            .Where(s => s.RoomId == roomId)
            .Select(s => (int?)s.SessionNumber)
            .MaxAsync() ?? 0) + 1;

        var newSession = new ExamSession
        {
            RoomId = roomId,
            SessionNumber = nextSessionNumber,
            StartTime = DateTime.UtcNow,
            Status = "Active",
            ExamType = examType
        };

        _context.ExamSessions.Add(newSession);

        // Update Room Status to Active
        room.Status = "Active";

        await _context.SaveChangesAsync();

        // Broadcast to SignalR so students know it started
        await _hubContext.Clients.Group(roomId.ToString()).SendAsync("SessionStarted");

        return Ok(new { message = "Session started successfully", sessionId = newSession.Id });
    }

    // PUT: api/rooms/sessions/{sessionId}/end
    // This marks a specific session as finished
    [HttpPut("sessions/{sessionId}/end")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> EndExamSession(int sessionId)
    {
        var session = await _context.ExamSessions.FindAsync(sessionId);
        if (session == null) return NotFound("Session not found.");

        session.EndTime = DateTime.UtcNow;
        session.Status = "Completed";

        // Also update the Room status back to Pending so it can be reused
        var room = await _context.Rooms.FindAsync(session.RoomId);
        if (room != null) room.Status = "Pending";

        await _context.SaveChangesAsync();

        // Broadcast to SignalR that the session is over
        await _hubContext.Clients.Group(session.RoomId.ToString()).SendAsync("SessionEnded");

        return Ok(new { message = "Session officially ended and logged in history." });
    }

    // GET: api/rooms/{roomId}/history
    // Fetches all past completed sessions for this specific room
    [HttpGet("{roomId}/history")]
    public async Task<IActionResult> GetRoomHistory(int roomId)
    {
        var history = await _context.ExamSessions
            .Where(s => s.RoomId == roomId && s.Status == "Completed")
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();

        var result = history.Select(session =>
        {
            var endTime = session.EndTime ?? DateTime.UtcNow;

            var participantCount = _context.SessionParticipants
                .Where(p => p.RoomId == roomId && p.JoinedAt >= session.StartTime && p.JoinedAt <= endTime)
                .Select(p => p.StudentId)
                .Distinct()
                .Count();

            return new
            {
                session.Id,
                session.SessionNumber,
                session.RoomId,
                session.StartTime,
                session.EndTime,
                session.Status,
                session.ExamType,
                ParticipantCount = participantCount
            };
        }).ToList();

        return Ok(result);
    }

    // ==========================================
    // SETTINGS, ENROLLMENT, & STUDENT LISTS
    // ==========================================

    [HttpGet("{roomId}/settings")]
    public async Task<ActionResult<RoomDetectionSettings>> GetRoomSettings(int roomId)
    {
        var settings = await _context.RoomDetectionSettings.FirstOrDefaultAsync(s => s.RoomId == roomId);
        if (settings == null) return NotFound("Settings not found.");
        return Ok(settings);
    }

    [HttpPost("{roomId}/settings")]
    public async Task<ActionResult<RoomDetectionSettings>> SaveRoomSettings(int roomId, [FromBody] RoomSetupDto setupRequest)
    {
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null) return NotFound("Room not found.");

        if (room.Status == "Active") return BadRequest("Cannot modify settings while an exam is running.");

        var existingSettings = await _context.RoomDetectionSettings.FirstOrDefaultAsync(s => s.RoomId == roomId);
        if (existingSettings != null)
        {
            existingSettings.EnableClipboardMonitoring = setupRequest.EnableClipboardMonitoring;
            existingSettings.EnableProcessDetection = setupRequest.EnableProcessDetection;
            existingSettings.EnableIdleDetection = setupRequest.EnableIdleDetection;
            existingSettings.IdleThresholdSeconds = setupRequest.IdleThresholdSeconds;
            existingSettings.EnableFocusDetection = setupRequest.EnableFocusDetection;
            existingSettings.EnableVirtualizationCheck = setupRequest.EnableVirtualizationCheck;
            existingSettings.StrictMode = setupRequest.StrictMode;
        }
        else
        {
            var settings = new RoomDetectionSettings
            {
                RoomId = roomId,
                EnableClipboardMonitoring = setupRequest.EnableClipboardMonitoring,
                EnableProcessDetection = setupRequest.EnableProcessDetection,
                EnableIdleDetection = setupRequest.EnableIdleDetection,
                IdleThresholdSeconds = setupRequest.IdleThresholdSeconds,
                EnableFocusDetection = setupRequest.EnableFocusDetection,
                EnableVirtualizationCheck = setupRequest.EnableVirtualizationCheck,
                StrictMode = setupRequest.StrictMode,
                CreatedAt = DateTime.UtcNow
            };

            _context.RoomDetectionSettings.Add(settings);
        }

        await _context.SaveChangesAsync();
        return Ok("Settings saved successfully.");
    }

    [HttpGet("{roomId}/participants")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetRoomParticipants(int roomId)
    {
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null) return NotFound("Room not found.");

        var enrollments = await _context.RoomEnrollments.Where(e => e.RoomId == roomId).ToListAsync();
        var studentIds = enrollments.Select(e => e.StudentId).ToList();

        // Fetch users including the new FullName field
        var users = await _context.Users
            .Where(u => studentIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => new { u.Email, u.FullName, u.ProfileImageUrl });

        var participants = await _context.SessionParticipants.Where(p => p.RoomId == roomId).ToListAsync();
        var participantDictionary = participants
            .GroupBy(p => p.StudentId)
            .Select(g => g.OrderByDescending(p => p.JoinedAt).First())
            .ToDictionary(p => p.StudentId);

        var result = enrollments.Select(enrollment => {
            string participationStatus = "NotJoined";
            if (participantDictionary.TryGetValue(enrollment.StudentId, out var p)) participationStatus = "Joined";

            users.TryGetValue(enrollment.StudentId, out var user);

            return new ParticipantDto
            {
                StudentId = enrollment.StudentId,
                StudentEmail = user?.Email ?? "Unknown",
                StudentName = user?.FullName ?? "No Name Set", // Sending the real name
                ProfileImageUrl = user?.ProfileImageUrl,
                EnrollmentSource = enrollment.EnrollmentSource,
                ParticipationStatus = participationStatus,
                ConnectionStatus = participantDictionary.TryGetValue(enrollment.StudentId, out var latestParticipant)
                    ? latestParticipant.ConnectionStatus
                    : "Disconnected"
            };
        }).ToList();

        return Ok(result);
    }

    // NEW: Actual Delete logic for unenrollment [cite: 72]
    [HttpDelete("{roomId}/unenroll/{studentId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> UnenrollStudent(int roomId, int studentId)
    {
        var enrollments = await _context.RoomEnrollments
            .Where(e => e.RoomId == roomId && e.StudentId == studentId)
            .ToListAsync();

        if (enrollments.Count == 0) return NotFound("Enrollment record not found.");

        _context.RoomEnrollments.RemoveRange(enrollments);

        var participantRecords = await _context.SessionParticipants
            .Where(p => p.RoomId == roomId && p.StudentId == studentId)
            .ToListAsync();
        if (participantRecords.Count > 0)
            _context.SessionParticipants.RemoveRange(participantRecords);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Student unenrolled successfully." });
    }

    [HttpPost("{roomId}/sessions/remove/{studentId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> RemoveStudentFromCurrentSession(int roomId, int studentId)
    {
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null) return NotFound("Room not found.");

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null) return Unauthorized();
        int instructorId = int.Parse(userIdString);

        if (room.InstructorId != instructorId)
            return StatusCode(403, "You do not have permission to modify this room session.");

        var latestParticipant = await _context.SessionParticipants
            .Where(p => p.RoomId == roomId && p.StudentId == studentId)
            .OrderByDescending(p => p.JoinedAt)
            .FirstOrDefaultAsync();

        if (latestParticipant == null)
            return NotFound("Student is not part of this session.");

        latestParticipant.ConnectionStatus = "Disconnected";
        latestParticipant.DisconnectedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _hubContext.Clients.Group(roomId.ToString()).SendAsync("StudentDisconnected", studentId);
        await _hubContext.Clients.User(studentId.ToString()).SendAsync("RemovedFromSession", roomId);

        return Ok(new { message = "Student removed from current session." });
    }

    [HttpGet("instructor")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetInstructorRooms()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null) return Unauthorized();
        int instructorId = int.Parse(userIdString);

        var rooms = await _context.Rooms.Where(r => r.InstructorId == instructorId).ToListAsync();
        return Ok(rooms);
    }

    // ==========================================
    // STUDENT DASHBOARD ENDPOINTS
    // ==========================================

    [HttpGet("student")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetStudentRooms()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null) return Unauthorized();
        int studentId = int.Parse(userIdString);

        // Find all room IDs this student is enrolled in
        var enrolledRoomIds = await _context.RoomEnrollments
            .Where(e => e.StudentId == studentId)
            .Select(e => e.RoomId)
            .ToListAsync();

        // Fetch the actual room details for those IDs
        var rooms = await _context.Rooms
            .Where(r => enrolledRoomIds.Contains(r.Id))
            .ToListAsync();

        var instructorIds = rooms.Select(r => r.InstructorId).Distinct().ToList();
        var instructors = await _context.Users
            .Where(u => instructorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Email);

        var result = rooms.Select(r =>
        {
            string subjectName = r.SubjectName;
            string section = string.Empty;

            if (!string.IsNullOrWhiteSpace(r.SubjectName))
            {
                var split = r.SubjectName.Split(new[] { " - " }, 2, StringSplitOptions.None);
                subjectName = split[0];
                if (split.Length > 1)
                {
                    section = split[1];
                }
            }

            return new
            {
                Id = r.Id,
                SubjectName = subjectName,
                Section = section,
                EnrollmentCode = r.EnrollmentCode,
                Status = r.Status,
                CourseImagePath = r.RoomImageUrl,
                RoomDescription = r.SubjectName,
                CreatedBy = instructors.TryGetValue(r.InstructorId, out var creator) ? creator : "Unknown Instructor"
            };
        })
            .ToList();

        return Ok(result);
    }

    [HttpPost("enroll-code")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> EnrollStudentByCode([FromBody] EnrollByCodeDto request)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null) return Unauthorized();
        int studentId = int.Parse(userIdString);

        // 1. Find the room matching the code exactly
        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.EnrollmentCode == request.EnrollmentCode);
        if (room == null) return BadRequest("Invalid course code. Please check with your instructor.");

        if (room.Status != "Pending")
            return BadRequest("Enrollment is only allowed while the room is in Pending status.");

        // 2. Check if already enrolled
        var existingEnrollment = await _context.RoomEnrollments
            .AnyAsync(e => e.RoomId == room.Id && e.StudentId == studentId);

        if (existingEnrollment) return BadRequest("You are already enrolled in this course.");

        // 3. Save the new enrollment
        var enrollment = new RoomEnrollment
        {
            RoomId = room.Id,
            StudentId = studentId,
            EnrollmentSource = "Code",
            EnrolledAt = DateTime.UtcNow
        };

        _context.RoomEnrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Successfully enrolled." });
    }

    [HttpPost("{sessionId}/generate-code")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GenerateRoomCode(int sessionId)
    {
        var room = await _context.Rooms.FindAsync(sessionId);
        if (room == null) return NotFound();

        string newCode = GenerateRoomCode();
        room.EnrollmentCode = newCode;
        await _context.SaveChangesAsync();

        return Ok(new { enrollmentCode = newCode });
    }

    [HttpPost("{roomId}/enroll-email")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> EnrollStudentByEmail(int roomId, [FromBody] string studentEmail)
    {
        // 1. Find the student by their unique email [cite: 104]
        var student = await _context.Users.FirstOrDefaultAsync(u => u.Email == studentEmail && u.Role == "Student");
        if (student == null) return NotFound("Student not found. Ask them to register first.");

        // 2. Prevent duplicate enrollments in the same room [cite: 109]
        var existing = await _context.RoomEnrollments
            .AnyAsync(e => e.RoomId == roomId && e.StudentId == student.Id);

        if (existing) return BadRequest("Student is already in this list.");

        // 3. Save the enrollment with source "Manual" [cite: 39, 45]
        var enrollment = new RoomEnrollment
        {
            RoomId = roomId,
            StudentId = student.Id,
            EnrollmentSource = "Manual",
            EnrolledAt = DateTime.UtcNow
        };

        _context.RoomEnrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Student added successfully." });
    }

    // ==========================================
    // HELPERS
    // ==========================================
    private string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}