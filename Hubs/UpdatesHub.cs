using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Anagram.Server.Data;
using Anagram.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Anagram.Server.Hubs
{
    [Authorize]
    public class UpdatesHub : Hub
    {
        private readonly AnagramDbContext _db;

        public UpdatesHub(AnagramDbContext db)
        {
            _db = db;
        }

        public override Task OnConnectedAsync()
        {
            // Optionally map Context.UserIdentifier to username in Startup/Program
            return base.OnConnectedAsync();
        }

        public async Task PostUpdate(string content, string? mediaUrl)
        {
            var username = Context.User?.Identity?.Name ?? throw new HubException("Unauthorized");
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == username);
            if (user == null) throw new HubException("User not found");

            var update = new Update { UserId = user.Id, Content = content, MediaUrl = mediaUrl };
            _db.Updates.Add(update);
            await _db.SaveChangesAsync();

            await Clients.All.SendAsync("NewUpdate", new
            {
                update.Id,
                Username = user.Username,
                user.AvatarUrl,
                update.Content,
                update.MediaUrl,
                update.Timestamp
            });
        }

        public async Task LikeUpdate(int updateId)
        {
            var username = Context.User?.Identity?.Name ?? throw new HubException("Unauthorized");
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == username);
            if (user == null) throw new HubException("User not found");

            var existing = await _db.UpdateLikes.SingleOrDefaultAsync(l => l.UpdateId == updateId && l.UserId == user.Id);
            if (existing != null)
            {
                _db.UpdateLikes.Remove(existing);
                await _db.SaveChangesAsync();
                await Clients.All.SendAsync("UpdateLiked", updateId, username, -1);
            }
            else
            {
                _db.UpdateLikes.Add(new UpdateLike { UpdateId = updateId, UserId = user.Id });
                await _db.SaveChangesAsync();
                await Clients.All.SendAsync("UpdateLiked", updateId, username, 1);
            }
        }

        public async Task SaveUpdate(int updateId)
        {
            var username = Context.User?.Identity?.Name ?? throw new HubException("Unauthorized");
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == username);
            if (user == null) throw new HubException("User not found");

            var existing = await _db.UpdateSaves.SingleOrDefaultAsync(s => s.UpdateId == updateId && s.UserId == user.Id);
            if (existing != null)
            {
                _db.UpdateSaves.Remove(existing);
                await _db.SaveChangesAsync();
                await Clients.Caller.SendAsync("UpdateSaved", updateId, false);
            }
            else
            {
                _db.UpdateSaves.Add(new UpdateSave { UpdateId = updateId, UserId = user.Id });
                await _db.SaveChangesAsync();
                await Clients.Caller.SendAsync("UpdateSaved", updateId, true);
            }
        }

        public async Task ShareUpdate(int updateId, string toUsername, string? note)
        {
            var fromUsername = Context.User?.Identity?.Name ?? throw new HubException("Unauthorized");
            var from = await _db.Users.SingleOrDefaultAsync(u => u.Username == fromUsername);
            var to = await _db.Users.SingleOrDefaultAsync(u => u.Username == toUsername);
            if (from == null || to == null) throw new HubException("User not found");

            var update = await _db.Updates.FindAsync(updateId);
            if (update == null) throw new HubException("Update not found");

            var share = new UpdateShare { UpdateId = updateId, FromUserId = from.Id, ToUserId = to.Id, Note = note };
            _db.UpdateShares.Add(share);
            await _db.SaveChangesAsync();

            await Clients.User(to.Username).SendAsync("UpdateShared", new { UpdateId = updateId, From = from.Username, note });
        }
    }
}
