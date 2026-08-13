using System;
using System.Collections.Generic;

namespace Anagram.Server.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public string Status { get; set; } = "Offline";
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Message> SentMessages { get; set; }
        public ICollection<Message> ReceivedMessages { get; set; }
        public ICollection<FileTransfer> SentFiles { get; set; }
        public ICollection<FileTransfer> ReceivedFiles { get; set; }
        public ICollection<VoiceNote> SentVoiceNotes { get; set; }
        public ICollection<VoiceNote> ReceivedVoiceNotes { get; set; }
        public ICollection<Call> CallsMade { get; set; }
        public ICollection<Call> CallsReceived { get; set; }
    }
}
