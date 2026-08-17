using System;

namespace Anagram.Server.Models
{
    public enum FriendRequestStatus { Pending, Accepted, Declined, Cancelled }

    public class FriendRequest
    {
        public int Id { get; set; }
        public int FromUserId { get; set; }
        public User FromUser { get; set; }
        public int ToUserId { get; set; }
        public User ToUser { get; set; }
        public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }
    }
}
