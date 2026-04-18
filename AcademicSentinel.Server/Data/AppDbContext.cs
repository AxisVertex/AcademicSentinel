using Microsoft.EntityFrameworkCore;
using AcademicSentinel.Server.Models;

namespace AcademicSentinel.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // This property represents your database table
    public DbSet<Room> Rooms { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<RoomDetectionSettings> RoomDetectionSettings { get; set; }
    public DbSet<ViolationLog> ViolationLogs { get; set; }
    public DbSet<RoomEnrollment> RoomEnrollments { get; set; }
    public DbSet<SessionAssignment> SessionAssignments { get; set; }
    public DbSet<SessionParticipant> SessionParticipants { get; set; }
    public DbSet<MonitoringEvent> MonitoringEvents { get; set; }
    public DbSet<RiskSummary> RiskSummaries { get; set; }
    public DbSet<ExamSession> ExamSessions { get; set; }

}