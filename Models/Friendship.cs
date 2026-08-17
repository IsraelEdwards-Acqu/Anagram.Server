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
    }
}
