using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using AcademicSentinel.Server.Data;
using AcademicSentinel.Server.Models;
using AcademicSentinel.Server.DTOs;
using System.Security.Claims;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicSentinel.Server.Hubs;

[Authorize] // 1. Secure the Hub so only logged-in apps can connect!
public class MonitoringHub : Hub
{
    private readonly AppDbContext _context;
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly ConcurrentDictionary<int, bool> MonitoringStates = new();

    public MonitoringHub(AppDbContext context, IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _scopeFactory = scopeFactory;
    }

    // =======================================================
    // IMC (TEACHER) CALLS THIS TO LISTEN FOR ALERTS
    // =======================================================
    public async Task JoinRoom(string roomId)
    {
        // Adds the teacher to the SignalR group for this specific exam
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
    }

    public async Task SetMonitoringState(int roomId, bool isActive)
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.Equals(role, "Instructor", StringComparison.OrdinalIgnoreCase))
            return;

        MonitoringStates[roomId] = isActive;
        await Clients.Group(roomId.ToString()).SendAsync("MonitoringStateChanged", isActive);
    }

    public Task<bool> GetMonitoringState(int roomId)
    {
        return Task.FromResult(MonitoringStates.TryGetValue(roomId, out var isActive) && isActive);
    }

    public async Task BeginMonitoringCountdown(int roomId, int delaySeconds, int monitoringDurationSeconds)
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.Equals(role, "Instructor", StringComparison.OrdinalIgnoreCase))
            return;

        MonitoringStates[roomId] = false;
        await Clients.Group(roomId.ToString()).SendAsync("MonitoringCountdownStarted", delaySeconds, monitoringDurationSeconds);

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(0, delaySeconds)));
            MonitoringStates[roomId] = true;
            await Clients.Group(roomId.ToString()).SendAsync("MonitoringStateChanged", true);
        });
    }

    public async Task EndSessionOnDisconnect(int roomId)
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.Equals(role, "Instructor", StringComparison.OrdinalIgnoreCase))
            return;

        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null || room.Status != "Active")
            return;

        var activeSession = await _context.ExamSessions
            .Where(s => s.RoomId == roomId && s.Status == "Active")
            .OrderByDescending(s => s.StartTime)
            .FirstOrDefaultAsync();

        if (activeSession == null)
            return;

        activeSession.Status = "Completed";
        activeSession.EndTime = DateTime.UtcNow;
        room.Status = "Pending";

        MonitoringStates[roomId] = false;
        await _context.SaveChangesAsync();

        await Clients.Group(roomId.ToString()).SendAsync("MonitoringStateChanged", false);
        await Clients.Group(roomId.ToString()).SendAsync("SessionEnded");
    }

    // SAC calls this when the student enters the active exam room
    public async Task JoinLiveExam(int roomId)
    {
        // Extract the Student's ID from their JWT
        var userIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null) return;
        int studentId = int.Parse(userIdString);

        // 1. Verify the room exists and is in Active state
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null) return;

        if (room.Status != "Active")
        {
            // Notify the client that they cannot join yet
            await Clients.Caller.SendAsync("JoinFailed", "Cannot join room: the instructor has not started the session or has ended it.");
            return;
        }

        var activeSession = await _context.ExamSessions
            .Where(s => s.RoomId == roomId && s.Status == "Active")
            .OrderByDescending(s => s.StartTime)
            .FirstOrDefaultAsync();

        // 2. Add connection to the SignalR Room Group
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());

        // 3. Update Database: Mark as officially "Participating" and "Connected"
        var participant = await _context.SessionParticipants
            .Where(p => p.RoomId == roomId && p.StudentId == studentId)
            .OrderByDescending(p => p.JoinedAt)
            .FirstOrDefaultAsync();

        bool shouldCreateNewParticipant = participant == null
            || (activeSession != null && participant.JoinedAt < activeSession.StartTime);

        if (shouldCreateNewParticipant)
        {
            // First join for the current active session
            participant = new SessionParticipant
            {
                RoomId = roomId,
                StudentId = studentId,
                ConnectionStatus = "Connected",
                JoinedAt = DateTime.UtcNow
            };
            _context.SessionParticipants.Add(participant);
        }
        else
        {
            // Reconnecting after a drop
            participant.ConnectionStatus = "Connected";
            participant.JoinedAt = DateTime.UtcNow;
            participant.DisconnectedAt = null;
        }
        await _context.SaveChangesAsync();

        // 4. Notify the IMC Dashboard that the student is live!
        await Clients.Group(roomId.ToString()).SendAsync("StudentJoined", studentId);
    }

    // SignalR AUTOMATICALLY triggers this if a user's app closes or internet drops
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdString != null)
        {
            int studentId = int.Parse(userIdString);

            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var hasCompletedSessionParticipant = await db.SessionParticipants
                    .AnyAsync(p => p.StudentId == studentId && p.ConnectionStatus == "Completed");

                if (hasCompletedSessionParticipant)
                {
                    await base.OnDisconnectedAsync(exception);
                    return;
                }

                // Find any active exam this student was in
                var activeParticipants = await db.SessionParticipants
                    .Where(p => p.StudentId == studentId && p.ConnectionStatus == "Connected")
                    .ToListAsync();

                foreach (var participant in activeParticipants)
                {
                    // Update database to show they dropped
                    participant.ConnectionStatus = "Disconnected";
                    participant.DisconnectedAt = DateTime.UtcNow;
                }

                await db.SaveChangesAsync();

                foreach (var participant in activeParticipants)
                {
                    // Instantly alert the Teacher's Dashboard (IMC)!
                    await Clients.Group(participant.RoomId.ToString()).SendAsync("StudentDisconnected", studentId);
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task ReSyncState(int roomId, int studentId)
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
            return;

        var userIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null) return;

        int authenticatedStudentId = int.Parse(userIdString);
        if (authenticatedStudentId != studentId) return;

        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null)
            return;

        var isSessionEnded = !string.Equals(room.Status, "Active", StringComparison.OrdinalIgnoreCase);

        var leaveAlreadyGranted = await _context.MonitoringEvents
            .AnyAsync(e => e.RoomId == roomId
                        && e.StudentId == studentId
                        && e.EventType == "LEAVE_GRANTED");

        if (isSessionEnded || leaveAlreadyGranted)
        {
            await Clients.Client(Context.ConnectionId).SendAsync("LeaveGranted", studentId);
        }
    }

    /// <summary>
    /// SAC calls this to send monitoring events in real-time
    /// This method receives detected violations from the Secure Assessment Client
    /// and stores them in the database, then relays alerts to the Instructor Monitoring Console
    /// </summary>
    public async Task SendMonitoringEvent(int roomId, int studentId, MonitoringEventDto eventData)
    {
        // Extract the Student's ID from their JWT to verify they're sending their own data
        var userIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null) return;

        int authenticatedStudentId = int.Parse(userIdString);

        // Security check: Students can only report their own events
        if (authenticatedStudentId != studentId) return;

        var latestParticipantState = await _context.SessionParticipants
            .Where(p => p.RoomId == roomId && p.StudentId == studentId)
            .OrderByDescending(p => p.JoinedAt)
            .Select(p => p.ConnectionStatus)
            .FirstOrDefaultAsync();

        if (string.Equals(latestParticipantState, "Completed", StringComparison.OrdinalIgnoreCase))
            return;

        // Verify the room exists
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null) return;

        // Create the monitoring event record
        var monitoringEvent = new MonitoringEvent
        {
            RoomId = roomId,
            StudentId = studentId,
            EventType = eventData.EventType,
            SeverityScore = eventData.SeverityScore,
            Timestamp = DateTime.UtcNow
        };

        _context.MonitoringEvents.Add(monitoringEvent);
        await _context.SaveChangesAsync();

        // Broadcast violation alert to the Instructor Monitoring Console
        // The IMC will display this as a real-time violation alert
        await Clients.Group(roomId.ToString()).SendAsync("ViolationDetected", new
        {
            studentId = studentId,
            eventType = eventData.EventType,
            severityScore = eventData.SeverityScore,
            description = eventData.Description,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task UpdateHardwareState(int roomId, int studentId, bool isVm, bool isRemote)
    {
        await Clients.Group(roomId.ToString()).SendAsync("ReceiveHardwareStateUpdate", studentId, isVm, isRemote);
    }

    public async Task RequestLeave(int roomId, int studentId)
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
            return;

        var userIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null) return;

        int authenticatedStudentId = int.Parse(userIdString);
        if (authenticatedStudentId != studentId) return;

        var isParticipantInRoom = await _context.SessionParticipants
            .AnyAsync(p => p.RoomId == roomId && p.StudentId == studentId);

        if (!isParticipantInRoom)
            return;

        await Clients.Group(roomId.ToString()).SendAsync("LeaveRequested", studentId);
    }

    public async Task GrantLeave(int roomId, int studentId)
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.Equals(role, "Instructor", StringComparison.OrdinalIgnoreCase))
            return;

        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var isParticipantInRoom = await db.SessionParticipants
                .AnyAsync(p => p.RoomId == roomId && p.StudentId == studentId);

            if (!isParticipantInRoom)
                return;

            var leaveGrantedEvent = new MonitoringEvent
            {
                RoomId = roomId,
                StudentId = studentId,
                EventType = "LEAVE_GRANTED",
                SeverityScore = 0,
                Timestamp = DateTime.UtcNow
            };

            db.MonitoringEvents.Add(leaveGrantedEvent);

            var participant = await db.SessionParticipants
                .Where(p => p.RoomId == roomId && p.StudentId == studentId)
                .OrderByDescending(p => p.JoinedAt)
                .FirstOrDefaultAsync();

            if (participant != null)
            {
                participant.ConnectionStatus = "Disconnected";
                participant.DisconnectedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
        }

        await Clients.User(studentId.ToString()).SendAsync("LeaveGranted", studentId);
        await Clients.Group(roomId.ToString()).SendAsync("StudentSafelyLeft", studentId);
        await Clients.Group(roomId.ToString()).SendAsync("LeaveApprovalUpdated", studentId, true);
    }

    public async Task NotifyStudentLeftSafely(int roomId, int studentId)
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
            return;

        var userIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null)
            return;

        int authenticatedStudentId = int.Parse(userIdString);
        if (authenticatedStudentId != studentId)
            return;

        var participant = await _context.SessionParticipants
            .Where(p => p.RoomId == roomId && p.StudentId == studentId)
            .OrderByDescending(p => p.JoinedAt)
            .FirstOrDefaultAsync();

        if (participant != null)
        {
            participant.ConnectionStatus = "Completed";
            participant.DisconnectedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        await Clients.Group(roomId.ToString()).SendAsync("StudentSafelyLeft", studentId);
    }
}