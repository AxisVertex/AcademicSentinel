using System;
using System.ComponentModel.DataAnnotations;

namespace AcademicSentinel.Server.Models
{
    public class ExamSession
    {
        [Key]
        public int Id { get; set; }

        public int RoomId { get; set; } // Links back to the permanent Course Room

        // Room-local sequence number (Session 1, Session 2, ...)
        public int SessionNumber { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; } // Nullable because it hasn't ended yet when it starts!

        public string Status { get; set; } = string.Empty; // "Active" or "Completed"

        public string ExamType { get; set; } = "Summative";
    }
}