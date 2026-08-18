using System;

namespace Anagram.Server.Models
{
    public class Friendship
    {
        public int Id { get; set; }
        public int UserAId { get; set; }
        public User UserA { get; set; }
        public int UserBId { get; set; }
        public User UserB { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // The user who initiated the friendship
        public int UserId { get; set; }
        // The friend (target user)
        public int FriendId { get; set; }
        public User Friend { get; set; }
    }
}
