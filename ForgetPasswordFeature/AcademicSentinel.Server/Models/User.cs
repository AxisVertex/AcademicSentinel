namespace AcademicSentinel.Server.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    //We now store a Hash, never the real password!
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Student";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Profile image storage
    public string? ProfileImageUrl { get; set; } // URL to stored image
    public string? ProfileImagePath { get; set; } // Local file path
    public string? ProfileImageContentType { get; set; } // MIME type (image/png, image/jpeg, etc.)
    public long? ProfileImageSize { get; set; } // File size in bytes
    public DateTime? ProfileImageUploadedAt { get; set; } // When the image was uploaded

    // Forgot-password flow fields
    public string? PasswordResetCodeHash { get; set; }
    public DateTime? PasswordResetCodeExpiresAt { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiresAt { get; set; }
}
