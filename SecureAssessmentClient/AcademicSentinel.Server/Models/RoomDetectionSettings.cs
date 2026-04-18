namespace AcademicSentinel.Server.Models;

public class RoomDetectionSettings
{
    public int Id { get; set; }

    // This links these settings to a specific Room
    public int RoomId { get; set; }

    // The specific modules to toggle
    public bool EnableClipboardMonitoring { get; set; } = true;
    public bool EnableProcessDetection { get; set; } = true;
    public bool EnableIdleDetection { get; set; } = true;
    public int IdleThresholdSeconds { get; set; } = 300; // Default 5 minutes
    public bool EnableFocusDetection { get; set; } = true;
    public bool EnableVirtualizationCheck { get; set; } = true;
    public bool StrictMode { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}