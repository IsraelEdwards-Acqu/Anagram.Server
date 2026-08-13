using System;

namespace Anagram.Server.Models
{
    public class VoiceNote
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string AudioPath { get; set; } = string.Empty;
        public double Duration { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation
        public User Sender { get; set; }
        public User Receiver { get; set; }
    }
}
