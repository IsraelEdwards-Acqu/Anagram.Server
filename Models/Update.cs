using System;
using System.Collections.Generic;

namespace Anagram.Server.Models
{
    public class Update
    {
        public int Id { get; set; }
        public int UserId { get; set; }            // owner
        public User User { get; set; }
        public string Content { get; set; } = string.Empty; // short text or caption
        public string? MediaUrl { get; set; }     // optional image/file reference
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Interaction collections
        public ICollection<UpdateLike> Likes { get; set; }
        public ICollection<UpdateSave> Saves { get; set; }
        public ICollection<UpdateShare> Shares { get; set; }
    }

    public class UpdateLike
    {
        public int Id { get; set; }
        public int UpdateId { get; set; }
        public Update Update { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public DateTime LikedAt { get; set; } = DateTime.UtcNow;
    }

    public class UpdateSave
    {
        public int Id { get; set; }
        public int UpdateId { get; set; }
        public Update Update { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }

    public class UpdateShare
    {
        public int Id { get; set; }
        public int UpdateId { get; set; }
        public Update Update { get; set; }
        public int FromUserId { get; set; }
        public User FromUser { get; set; }
        public int ToUserId { get; set; }         // share target (friend or follower)
        public User ToUser { get; set; }
        public DateTime SharedAt { get; set; } = DateTime.UtcNow;
        public string? Note { get; set; }         // optional message when sharing
    }

}
