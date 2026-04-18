using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using AcademicSentinel.Server.Data;
using AcademicSentinel.Server.Models;
using AcademicSentinel.Server.DTOs;
using System.Security.Claims;

namespace AcademicSentinel.Server.Hubs;

[Authorize] // 1. Secure the Hub so only logged-in apps can connect!
public class MonitoringHub : Hub
{
    private readonly AppDbContext _context;

    public MonitoringHub(AppDbContext context)
    {
        _context = context;
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

        // 2. Add connection to the SignalR Room Group
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());

        // 3. Update Database: Mark as officially "Participating" and "Connected"
        var participant = await _context.SessionParticipants
            .FirstOrDefaultAsync(p => p.RoomId == roomId && p.StudentId == studentId);

        if (participant == null)
        {
            // First time joining the live session
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

            // Find any active exam this student was in
            var activeParticipants = await _context.SessionParticipants
                .Where(p => p.StudentId == studentId && p.ConnectionStatus == "Connected")
                .ToListAsync();

            foreach (var participant in activeParticipants)
            {
                // Update database to show they dropped
                participant.ConnectionStatus = "Disconnected";
                participant.DisconnectedAt = DateTime.UtcNow;

                // Instantly alert the Teacher's Dashboard (IMC)!
                await Clients.Group(participant.RoomId.ToString()).SendAsync("StudentDisconnected", studentId);
            }

            await _context.SaveChangesAsync();
        }

        await base.OnDisconnectedAsync(exception);
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
            timestamp = DateTime.UtcNow
        });
    }
}