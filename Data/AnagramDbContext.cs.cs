using Anagram.Server.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Relationships
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
        }
    }
}
