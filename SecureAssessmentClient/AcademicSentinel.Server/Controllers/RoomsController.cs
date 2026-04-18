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

// This sets the URL path to: http://localhost:[port]/api/rooms
[Route("api/[controller]")]
[ApiController]
[Authorize] // (Assuming you have this from earlier)
public class RoomsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<MonitoringHub> _hubContext; // <-- ADD THIS MEGAPHONE

    // Update constructor to accept the Hub Context
    public RoomsController(AppDbContext context, IHubContext<MonitoringHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // POST: api/rooms
    // This endpoint creates a new room. The instructor's app will call this.
    [HttpPost]
    [Authorize(Roles = "Instructor")] // Lock this to Instructors
    public async Task<ActionResult<Room>> CreateRoom([FromBody] Room roomRequest)
    {
        // 1. Extract the Instructor's ID from their JWT Badge
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null) return Unauthorized();

        // 2. Set the initial state
        roomRequest.InstructorId = int.Parse(userIdString);
        roomRequest.Status = "Pending";
        roomRequest.EnrollmentCode = null; // Starts as null (Manual Room)
        roomRequest.CreatedAt = DateTime.UtcNow;

        // 3. Save to the database
        _context.Rooms.Add(roomRequest);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRoom), new { id = roomRequest.Id }, roomRequest);
    }

    // GET: api/rooms/{id}
    // This endpoint fetches a specific room to check its details.
    [HttpGet("{id}")]
    public async Task<ActionResult<Room>> GetRoom(int id)
    {
        var room = await _context.Rooms.FindAsync(id);

        if (room == null)
        {
            return NotFound("Room not found.");
        }

        return room;
    }

    // GET: api/rooms/{roomId}/settings
    // The SAC calls this to know what rules to enforce
    [HttpGet("{roomId}/settings")]
    public async Task<ActionResult<RoomDetectionSettings>> GetRoomSettings(int roomId)
    {
        var settings = await _context.RoomDetectionSettings
                                     .FirstOrDefaultAsync(s => s.RoomId == roomId);

        if (settings == null)
        {
            return NotFound("Settings for this room have not been created yet.");
        }

        return Ok(settings);
    }

    // POST: api/rooms/{roomId}/settings
    // The IMC calls this to save the instructor's rules
    [HttpPost("{roomId}/settings")]
    public async Task<ActionResult<RoomDetectionSettings>> SaveRoomSettings(int roomId, [FromBody] RoomDetectionSettings settingsRequest)
    {
        // 1. Ensure the room actually exists first
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null) return NotFound("Room not found.");

        // 2. Prevent changing rules if the room is already Active
        if (room.Status != "Pending")
        {
            return BadRequest("Cannot modify settings after the room has started.");
        }

        // 3. Check if settings already exist. If they do, update them. If not, add them.
        var existingSettings = await _context.RoomDetectionSettings
                                             .FirstOrDefaultAsync(s => s.RoomId == roomId);

        if (existingSettings != null)
        {
            // Update existing
            existingSettings.EnableClipboardMonitoring = settingsRequest.EnableClipboardMonitoring;
            existingSettings.EnableProcessDetection = settingsRequest.EnableProcessDetection;
            existingSettings.EnableIdleDetection = settingsRequest.EnableIdleDetection;
            existingSettings.IdleThresholdSeconds = settingsRequest.IdleThresholdSeconds;
            existingSettings.EnableFocusDetection = settingsRequest.EnableFocusDetection;
            existingSettings.EnableVirtualizationCheck = settingsRequest.EnableVirtualizationCheck;
            existingSettings.StrictMode = settingsRequest.StrictMode;
        }
        else
        {
            // Add new
            settingsRequest.RoomId = roomId;
            settingsRequest.CreatedAt = DateTime.UtcNow;
            _context.RoomDetectionSettings.Add(settingsRequest);
        }

        await _context.SaveChangesAsync();
        return Ok("Settings saved successfully.");
    }

    // PUT: api/rooms/{id}/status
    // The IMC calls this when the instructor clicks "Start Exam" or "End Exam"
    [HttpPut("{id}/status")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> UpdateRoomStatus(int id, [FromBody] RoomStatusUpdateDto request)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room == null) return NotFound("Room not found.");

        var validStatuses = new[] { "Pending", "Countdown", "Active", "Ended" };
        if (!validStatuses.Contains(request.Status))
        {
            return BadRequest("Invalid status. Must be Pending, Countdown, Active, or Ended.");
        }

        // 1. Update database
        room.Status = request.Status;
        await _context.SaveChangesAsync();

        // 2. BROADCAST THE EXACT SIGNALS YOUR THESIS REQUIRES!
        string groupId = room.Id.ToString();

        // Always notify everyone that the status changed generally
        await _hubContext.Clients.Group(groupId).SendAsync("SessionStatusChanged", request.Status);

        // Fire the specific triggers for the SAC apps to react to
        if (request.Status == "Countdown")
        {
            await _hubContext.Clients.Group(groupId).SendAsync("SessionCountdownStarted");
        }
        else if (request.Status == "Active")
        {
            await _hubContext.Clients.Group(groupId).SendAsync("SessionStarted");
        }
        else if (request.Status == "Ended")
        {
            await _hubContext.Clients.Group(groupId).SendAsync("SessionEnded");
        }

        return Ok(new { message = $"Room status successfully updated to {room.Status}", roomId = room.Id });
    }

    // GET: api/rooms/{roomId}/students
    // IMC calls this to see who is currently in the room and how they joined
    [HttpGet("{roomId}/students")]
    public async Task<IActionResult> GetEnrolledStudents(int roomId)
    {
        var students = await _context.RoomEnrollments
                                     .Where(e => e.RoomId == roomId)
                                     .ToListAsync();
        return Ok(students);
    }

    // GET: api/rooms/history
    // IMC calls this to load the "Historical Room Tracking" dashboard
    [HttpGet("history")]
    public async Task<IActionResult> GetSessionHistory()
    {
        // Fetch all rooms that have officially "Ended", sorted by newest first
        var historicalRooms = await _context.Rooms
                                            .Where(r => r.Status == "Ended")
                                            .OrderByDescending(r => r.CreatedAt)
                                            .ToListAsync();
        return Ok(historicalRooms);
    }

    // GET: api/rooms/instructor
    // IMC calls this to load the Teacher's main dashboard
    [HttpGet("instructor")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetInstructorRooms()
    {
        // 1. Extract the Instructor's ID directly from their JWT Badge
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null) return Unauthorized();
        int instructorId = int.Parse(userIdString);

        // 2. Fetch only the rooms created by this specific instructor
        var rooms = await _context.Rooms
            .Where(r => r.InstructorId == instructorId)
            .ToListAsync();

        return Ok(rooms);
    }

    // GET: api/rooms/my
    // SAC calls this to display the clickable "Room Orbs" for the student
    [HttpGet("my")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetStudentRooms()
    {
        // 1. Extract the Student's ID from their JWT Badge
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null) return Unauthorized();
        int studentId = int.Parse(userIdString);

        // 2. Look up which rooms this student is officially enrolled in
        var enrolledRoomIds = await _context.RoomEnrollments
            .Where(e => e.StudentId == studentId) // Assuming we update Enrollment to use ID
            .Select(e => e.RoomId)
            .ToListAsync();

        // 3. Fetch the actual Room details for those IDs
        var myRooms = await _context.Rooms
            .Where(r => enrolledRoomIds.Contains(r.Id))
            .ToListAsync();

        return Ok(myRooms);
    }

    // POST: api/rooms/{sessionId}/generate-code
    // The IMC calls this when the teacher clicks "Generate Invite Code"
    [HttpPost("{sessionId}/generate-code")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GenerateRoomCode(int sessionId)
    {
        // 1. Find the room
        var room = await _context.Rooms.FindAsync(sessionId);
        if (room == null) return NotFound("Room not found.");

        // 2. Verify this instructor owns the room
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null) return Unauthorized();
        int instructorId = int.Parse(userIdString);
        if (room.InstructorId != instructorId) return Forbid();

        // 3. Prevent generating codes for active or ended rooms
        if (room.Status != "Pending")
        {
            return BadRequest("Cannot generate codes for a room that has already started.");
        }

        // 4. Generate a random 6-character uppercase alphanumeric code
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        string newCode;
        bool codeExists;

        // Loop just in case it randomly generates a code that already exists
        do
        {
            newCode = new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            codeExists = await _context.Rooms.AnyAsync(r => r.EnrollmentCode == newCode);
        } while (codeExists);

        // 5. Save the code to the room
        room.EnrollmentCode = newCode;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Code generated successfully.", enrollmentCode = newCode });
    }

    // POST: api/rooms/enroll
    // Student calls this to enroll using a room enrollment code
    [HttpPost("enroll")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> EnrollByCode([FromBody] EnrollByCodeDto request)
    {
        // 1. Extract the Student's ID from their JWT
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null) return Unauthorized();
        int studentId = int.Parse(userIdString);

        // 2. Find the room by enrollment code
        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.EnrollmentCode == request.EnrollmentCode);
        if (room == null) return NotFound("Invalid enrollment code.");

        // 3. Validate room status - students can only enroll in Pending rooms
        if (room.Status != "Pending")
        {
            return BadRequest("Cannot enroll in a room that is not open for enrollment. The instructor has either not started or already ended this session.");
        }

        // 4. Check if code is still valid (not expired)
        if (room.CodeExpiry.HasValue && room.CodeExpiry < DateTime.UtcNow)
        {
            return BadRequest("Enrollment code has expired.");
        }

        // 5. Check if student is already enrolled in this room
        var existingEnrollment = await _context.RoomEnrollments
            .FirstOrDefaultAsync(e => e.RoomId == room.Id && e.StudentId == studentId);

        if (existingEnrollment != null)
        {
            return BadRequest("Student is already enrolled in this room.");
        }

        // 6. Create enrollment record with "Code" source
        var enrollment = new RoomEnrollment
        {
            RoomId = room.Id,
            StudentId = studentId,
            EnrollmentSource = "Code",
            EnrolledAt = DateTime.UtcNow
        };

        _context.RoomEnrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Successfully enrolled using the code.", roomId = room.Id });
    }

    // GET: api/rooms/{roomId}/participants
    // IMC calls this to see all participants in a room
    [HttpGet("{roomId}/participants")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetRoomParticipants(int roomId)
    {
        // 1. Verify room exists
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null) return NotFound("Room not found.");

        // 2. Verify this instructor owns the room
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null) return Unauthorized();
        int instructorId = int.Parse(userIdString);
        if (room.InstructorId != instructorId) return Forbid();

        // 3. Get all enrolled students
        var enrollments = await _context.RoomEnrollments
            .Where(e => e.RoomId == roomId)
            .ToListAsync();

        var studentIds = enrollments.Select(e => e.StudentId).ToList();

        // 4. Get user emails
        var users = await _context.Users
            .Where(u => studentIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email);

        // 5. Get participation info for each student
        var participants = await _context.SessionParticipants
            .Where(p => p.RoomId == roomId)
            .ToListAsync();

        var participantDictionary = participants.ToDictionary(p => p.StudentId);

        // 6. Build response DTO
        var result = enrollments.Select(enrollment =>
        {
            string participationStatus = "NotJoined";
            string connectionStatus = "Disconnected";
            DateTime? joinedAt = null;
            DateTime? disconnectedAt = null;

            if (participantDictionary.TryGetValue(enrollment.StudentId, out var participant))
            {
                participationStatus = "Joined";
                connectionStatus = participant.ConnectionStatus;
                joinedAt = participant.JoinedAt;
                disconnectedAt = participant.DisconnectedAt;
            }

            return new ParticipantDto
            {
                StudentId = enrollment.StudentId,
                StudentEmail = users.ContainsKey(enrollment.StudentId) ? users[enrollment.StudentId] : "Unknown",
                EnrollmentSource = enrollment.EnrollmentSource,
                EnrolledAt = enrollment.EnrolledAt,
                JoinedAt = joinedAt,
                ParticipationStatus = participationStatus,
                ConnectionStatus = connectionStatus,
                DisconnectedAt = disconnectedAt
            };
        }).ToList();

        return Ok(result);
    }

    // GET: api/rooms/{roomId}/status
    // Get room status and participant counts
    [HttpGet("{roomId}/status")]
    public async Task<IActionResult> GetRoomStatus(int roomId)
    {
        // 1. Verify room exists
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null) return NotFound("Room not found.");

        // 2. Get enrollment count
        var totalEnrolled = await _context.RoomEnrollments
            .Where(e => e.RoomId == roomId)
            .CountAsync();

        // 3. Get participant count (joined students)
        var totalJoined = await _context.SessionParticipants
            .Where(p => p.RoomId == roomId)
            .CountAsync();

        // 4. Calculate not joined
        var totalNotJoined = totalEnrolled - totalJoined;

        var statusDto = new RoomStatusDto
        {
            RoomId = room.Id,
            SubjectName = room.SubjectName,
            Status = room.Status,
            CreatedAt = room.CreatedAt,
            TotalEnrolled = totalEnrolled,
            TotalJoined = totalJoined,
            TotalNotJoined = totalNotJoined
        };

        return Ok(statusDto);
    }

    // POST: api/rooms/{sessionId}/assign
    // Instructor calls this to manually assign students
    [HttpPost("{sessionId}/assign")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> AssignStudents(int sessionId, [FromBody] List<int> studentIds)
    {
        // 1. Find the room
        var room = await _context.Rooms.FindAsync(sessionId);
        if (room == null) return NotFound("Room not found.");

        // 2. Verify this instructor owns the room
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null) return Unauthorized();
        int instructorId = int.Parse(userIdString);
        if (room.InstructorId != instructorId) return Forbid();

        // 3. Prevent assigning to active or ended rooms
        if (room.Status != "Pending")
        {
            return BadRequest("Cannot assign students to a room that has already started.");
        }

        // 4. Verify all student IDs exist
        var existingStudents = await _context.Users
            .Where(u => u.Role == "Student" && studentIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync();

        var invalidIds = studentIds.Except(existingStudents).ToList();
        if (invalidIds.Any())
        {
            return BadRequest($"Invalid student IDs: {string.Join(", ", invalidIds)}");
        }

        // 5. Check for existing assignments or enrollments
        var existingEnrollments = await _context.RoomEnrollments
            .Where(e => e.RoomId == sessionId && studentIds.Contains(e.StudentId))
            .Select(e => e.StudentId)
            .ToListAsync();

        var newStudentIds = studentIds.Except(existingEnrollments).ToList();

        if (!newStudentIds.Any())
        {
            return Ok(new { message = "All students are already enrolled in this room." });
        }

        // 6. Create enrollment records with "Manual" source
        var enrollments = newStudentIds.Select(studentId => new RoomEnrollment
        {
            RoomId = sessionId,
            StudentId = studentId,
            EnrollmentSource = "Manual",
            EnrolledAt = DateTime.UtcNow
        }).ToList();

        _context.RoomEnrollments.AddRange(enrollments);

        // 7. Also create SessionAssignment records for tracking
        var assignments = newStudentIds.Select(studentId => new SessionAssignment
        {
            RoomId = sessionId,
            StudentId = studentId,
            AssignedAt = DateTime.UtcNow
        }).ToList();

        _context.SessionAssignments.AddRange(assignments);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Successfully assigned {newStudentIds.Count} students." });
    }

    // Helper method to generate a random 6-character alphanumeric code
    private string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}