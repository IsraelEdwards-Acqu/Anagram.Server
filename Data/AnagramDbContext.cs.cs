using Anagram.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Anagram.Server.Data
{
    public class AnagramDbContext : DbContext
    {
        public AnagramDbContext(DbContextOptions<AnagramDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<FileTransfer> FileTransfers { get; set; }
        public DbSet<VoiceNote> VoiceNotes { get; set; }
        public DbSet<Call> Calls { get; set; }
        public DbSet<Update> Updates { get; set; }
        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<UpdateLike> UpdateLikes { get; set; }
        public DbSet<UpdateSave> UpdateSaves { get; set; }
        public DbSet<UpdateShare> UpdateShares { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Relationships for Messages
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationships for FileTransfers
            modelBuilder.Entity<FileTransfer>()
                .HasOne(f => f.Sender)
                .WithMany(u => u.SentFiles)
                .HasForeignKey(f => f.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FileTransfer>()
                .HasOne(f => f.Receiver)
                .WithMany(u => u.ReceivedFiles)
                .HasForeignKey(f => f.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationships for VoiceNotes
            modelBuilder.Entity<VoiceNote>()
                .HasOne(v => v.Sender)
                .WithMany(u => u.SentVoiceNotes)
                .HasForeignKey(v => v.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VoiceNote>()
                .HasOne(v => v.Receiver)
                .WithMany(u => u.ReceivedVoiceNotes)
                .HasForeignKey(v => v.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationships for Calls
            modelBuilder.Entity<Call>()
                .HasOne(c => c.Caller)
                .WithMany(u => u.CallsMade)
                .HasForeignKey(c => c.CallerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Call>()
                .HasOne(c => c.Receiver)
                .WithMany(u => u.CallsReceived)
                .HasForeignKey(c => c.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationships for Updates
            modelBuilder.Entity<Update>()
                .HasOne(u => u.User)
                .WithMany(x => x.Updates)
                .HasForeignKey(u => u.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique username
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // FriendRequest: prevent duplicate pending requests
            modelBuilder.Entity<FriendRequest>()
                .HasIndex(fr => new { fr.FromUserId, fr.ToUserId })
                .IsUnique();

            // Friendship: ensure unique pair (UserAId < UserBId) via check in code or normalized insert
            modelBuilder.Entity<Friendship>()
                .HasIndex(f => new { f.UserAId, f.UserBId })
                .IsUnique();

            // Follow: unique follower-following
            modelBuilder.Entity<Follow>()
                .HasIndex(f => new { f.FollowerId, f.FollowingId })
                .IsUnique();

            // Update interactions: unique per user per update
            modelBuilder.Entity<UpdateLike>()
                .HasIndex(l => new { l.UpdateId, l.UserId })
                .IsUnique();

            modelBuilder.Entity<UpdateSave>()
                .HasIndex(s => new { s.UpdateId, s.UserId })
                .IsUnique();

            // Configure cascade rules carefully (restrict deletes where appropriate)
            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.FromUser).WithMany(u => u.SentFriendRequests).HasForeignKey(fr => fr.FromUserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.ToUser).WithMany(u => u.ReceivedFriendRequests).HasForeignKey(fr => fr.ToUserId).OnDelete(DeleteBehavior.Restrict);

            // Follow relationships
            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Follower).WithMany(u => u.Following).HasForeignKey(f => f.FollowerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Following).WithMany(u => u.Followers).HasForeignKey(f => f.FollowingId).OnDelete(DeleteBehavior.Restrict);

            // Update relationships
            modelBuilder.Entity<Update>()
                .HasOne(u => u.User).WithMany(x => x.Updates).HasForeignKey(u => u.UserId).OnDelete(DeleteBehavior.Cascade);

        }
    }
}
