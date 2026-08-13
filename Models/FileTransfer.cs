using System;

namespace Anagram.Server.Models
{
    public class FileTransfer
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string StoragePath { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation
        public User Sender { get; set; }
        public User Receiver { get; set; }
    }
}
