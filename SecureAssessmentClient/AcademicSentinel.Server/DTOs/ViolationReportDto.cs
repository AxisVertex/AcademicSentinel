namespace AcademicSentinel.Server.DTOs;

public class ViolationReportDto
{
    public int RoomId { get; set; }
    public string StudentEmail { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SeverityLevel { get; set; } = "S1";
}