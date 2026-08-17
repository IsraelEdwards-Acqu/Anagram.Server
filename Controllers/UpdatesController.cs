using Anagram.Server.Data;
using Anagram.Server.Hubs;
using Anagram.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Anagram.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UpdatesController : ControllerBase
    {
        private readonly AnagramDbContext _db;
        private readonly IHubContext<UpdatesHub> _updatesHub;

        public UpdatesController(AnagramDbContext db, IHubContext<UpdatesHub> updatesHub)
        {
            _db = db;
            _updatesHub = updatesHub;
        }

        private string CurrentUsername => User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        [HttpPost]
        public async Task<IActionResult> PostUpdate([FromBody] PostUpdateDto dto)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == CurrentUsername);
            if (user == null) return Unauthorized();

            var update = new Update { UserId = user.Id, Content = dto.Content, MediaUrl = dto.MediaUrl };
            _db.Updates.Add(update);
            await _db.SaveChangesAsync();

            var dtoOut = new
            {
                update.Id,
                Username = user.Username,
                user.AvatarUrl,
                update.Content,
                update.MediaUrl,
                update.Timestamp
            };

            // Broadcast to followers and friends: for simplicity broadcast to all connected clients;
            // client can filter feed by following/friends on receive or server can implement targeted broadcast.
            await _updatesHub.Clients.All.SendAsync("NewUpdate", dtoOut);

            return Ok(dtoOut);
        }

        [HttpGet("feed")]
        public async Task<IActionResult> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            // Simple feed: recent updates from everyone, ordered desc. Later refine to friends+following.
            var updates = await _db.Updates
                .Include(u => u.User)
                .OrderByDescending(u => u.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    Username = u.User.Username,
                    u.User.AvatarUrl,
                    u.Content,
                    u.MediaUrl,
                    u.Timestamp,
                    Likes = u.Likes.Count,
                    Saves = u.Saves.Count
                })
                .ToListAsync();

            return Ok(updates);
        }

        [HttpPost("{id}/like")]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == CurrentUsername);
            if (user == null) return Unauthorized();

            var update = await _db.Updates.FindAsync(id);
            if (update == null) return NotFound();

            var existing = await _db.UpdateLikes.SingleOrDefaultAsync(l => l.UpdateId == id && l.UserId == user.Id);
            if (existing != null)
            {
                _db.UpdateLikes.Remove(existing);
                await _db.SaveChangesAsync();
                await _updatesHub.Clients.All.SendAsync("UpdateLiked", id, user.Username, -1);
                return Ok(new { liked = false });
            }
            else
            {
                var like = new UpdateLike { UpdateId = id, UserId = user.Id };
                _db.UpdateLikes.Add(like);
                await _db.SaveChangesAsync();
                await _updatesHub.Clients.All.SendAsync("UpdateLiked", id, user.Username, 1);
                return Ok(new { liked = true });
            }
        }

        [HttpPost("{id}/save")]
        public async Task<IActionResult> ToggleSave(int id)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == CurrentUsername);
            if (user == null) return Unauthorized();

            var update = await _db.Updates.FindAsync(id);
            if (update == null) return NotFound();

            var existing = await _db.UpdateSaves.SingleOrDefaultAsync(s => s.UpdateId == id && s.UserId == user.Id);
            if (existing != null)
            {
                _db.UpdateSaves.Remove(existing);
                await _db.SaveChangesAsync();
                return Ok(new { saved = false });
            }
            else
            {
                var save = new UpdateSave { UpdateId = id, UserId = user.Id };
                _db.UpdateSaves.Add(save);
                await _db.SaveChangesAsync();
                return Ok(new { saved = true });
            }
        }

        [HttpPost("{id}/share")]
        public async Task<IActionResult> ShareUpdate(int id, [FromBody] ShareUpdateDto dto)
        {
            var from = await _db.Users.SingleOrDefaultAsync(u => u.Username == CurrentUsername);
            if (from == null) return Unauthorized();

            var to = await _db.Users.SingleOrDefaultAsync(u => u.Username == dto.ToUsername);
            if (to == null) return NotFound("Target user not found");

            var update = await _db.Updates.FindAsync(id);
            if (update == null) return NotFound();

            var share = new UpdateShare { UpdateId = id, FromUserId = from.Id, ToUserId = to.Id, Note = dto.Note };
            _db.UpdateShares.Add(share);
            await _db.SaveChangesAsync();

            // Optionally notify recipient via UpdatesHub or SocialHub
            await _updatesHub.Clients.User(to.Username).SendAsync("UpdateShared", new { UpdateId = id, From = from.Username, dto.Note });

            return Ok(new { shared = true });
        }
    }

    public record PostUpdateDto(string Content, string? MediaUrl);
    public record ShareUpdateDto(string ToUsername, string? Note);
}
