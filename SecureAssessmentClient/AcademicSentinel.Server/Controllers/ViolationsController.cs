using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcademicSentinel.Server.Data;
using AcademicSentinel.Server.Models;
using AcademicSentinel.Server.DTOs;
using Microsoft.AspNetCore.SignalR;
using AcademicSentinel.Server.Hubs;
using Microsoft.AspNetCore.Authorization;

namespace AcademicSentinel.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ViolationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<MonitoringHub> _hubContext; // Add this line

    // Update the constructor to accept the hub context
    public ViolationsController(AppDbContext context, IHubContext<MonitoringHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // POST: api/violations
    // The SAC calls this when it catches a student cheating
    [HttpPost]
    public async Task<ActionResult<ViolationLog>> ReportViolation([FromBody] ViolationReportDto request)
    {
        // 1. Ensure the room actually exists
        var room = await _context.Rooms.FindAsync(request.RoomId);
        if (room == null) return NotFound("Room not found.");

        // 2. Create the log entry
        var log = new ViolationLog
        {
            RoomId = request.RoomId,
            StudentEmail = request.StudentEmail,
            Module = request.Module,
            Description = request.Description,
            SeverityLevel = request.SeverityLevel,
            Timestamp = DateTime.UtcNow
        };

        // 3. Save to database
        _context.ViolationLogs.Add(log);
        await _context.SaveChangesAsync();

        // 4. BROADCAST TO THE INSTRUCTOR IN REAL-TIME!
        // We convert the RoomId to a string because SignalR groups use strings
        await _hubContext.Clients.Group(request.RoomId.ToString())
                                 .SendAsync("ReceiveViolationAlert", log);

        return StatusCode(201, log);

        //return CreatedAtAction(nameof(GetRoomViolations), new { roomId = log.RoomId }, log);
    }

    // GET: api/violations/room/{roomId}
    // The IMC calls this to fetch all logs for their exam dashboard
    [HttpGet("room/{roomId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<ActionResult<IEnumerable<ViolationLog>>> GetRoomViolations(int roomId)
    {
        // Verify room exists and instructor owns it
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null) return NotFound("Room not found.");

        // Fetch all logs for this specific room, sorted from newest to oldest
        var logs = await _context.ViolationLogs
                                 .Where(v => v.RoomId == roomId)
                                 .OrderByDescending(v => v.Timestamp)
                                 .ToListAsync();

        return Ok(logs);
    }
}