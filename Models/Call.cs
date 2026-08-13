using System;

namespace Anagram.Server.Models
{
    public class Call
    {
        public int Id { get; set; }
        public int CallerId { get; set; }
        public int ReceiverId { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Active, Ended
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }

        // Navigation
        public User Caller { get; set; }
        public User Receiver { get; set; }
    }
}
