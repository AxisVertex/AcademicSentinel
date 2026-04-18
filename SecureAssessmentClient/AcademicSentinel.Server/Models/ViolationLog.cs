namespace AcademicSentinel.Server.Models;

public class ViolationLog
{
    public int Id { get; set; }

    // Which room did this happen in?
    public int RoomId { get; set; }

    // Who did it?
    public string StudentEmail { get; set; } = string.Empty;

    // Which module caught them? (e.g., "RTFM", "VAC", "IDLE")
    public string Module { get; set; } = string.Empty;

    // What exactly happened? (e.g., "Window focus lost: Alt-Tab detected")
    public string Description { get; set; } = string.Empty;

    // How severe is it based on your thesis? (e.g., "S1", "S2", "S3")
    public string SeverityLevel { get; set; } = "S1";

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}