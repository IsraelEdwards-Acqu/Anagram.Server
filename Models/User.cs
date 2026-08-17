using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Anagram.Server.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;
        // Authentication
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        // Profile
        public string Name { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = "/images/default-avatar.png";

        // Status
        public string Status { get; set; } = "Offline";
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        // <-- Add Bio (nullable)
        public string? Bio { get; set; }
        // Navigation
        public ICollection<Message> SentMessages { get; set; }
        public ICollection<Message> ReceivedMessages { get; set; }
        public ICollection<FileTransfer> SentFiles { get; set; }
        public ICollection<FileTransfer> ReceivedFiles { get; set; }
        public ICollection<VoiceNote> SentVoiceNotes { get; set; }
        public ICollection<VoiceNote> ReceivedVoiceNotes { get; set; }
        public ICollection<Call> CallsMade { get; set; }
        public ICollection<Call> CallsReceived { get; set; }
        public ICollection<Update> Updates { get; set; }

        public ICollection<FriendRequest> SentFriendRequests { get; set; }
        public ICollection<FriendRequest> ReceivedFriendRequests { get; set; }
        public ICollection<Friendship> FriendshipsA { get; set; }
        public ICollection<Friendship> FriendshipsB { get; set; }
        public ICollection<Follow> Followers { get; set; }
        public ICollection<Follow> Following { get; set; }
        public ICollection<UpdateLike> LikedUpdates { get; set; }
        public ICollection<UpdateSave> SavedUpdates { get; set; }
        public ICollection<UpdateShare> SentShares { get; set; }
        public ICollection<UpdateShare> ReceivedShares { get; set; }

    }

}
