using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Anagram.Server.Data;
using Anagram.Server.Models;

namespace Anagram.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AnagramDbContext _db;

        public UsersController(AnagramDbContext db)
        {
            _db = db;
        }

        // Helper: current username from JWT (ClaimTypes.Name)
        private string CurrentUsername => User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        // Helper: current user id from JWT (ClaimTypes.NameIdentifier)
        private int? CurrentUserId
        {
            get
            {
                var idClaim = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                return int.TryParse(idClaim, out var id) ? id : (int?)null;
            }
        }

        // Public profile (no auth required)
        [AllowAnonymous]
        [HttpGet("{username}/profile")]
        public async Task<IActionResult> GetProfile(string username)
        {
            var user = await _db.Users
                .Where(u => u.Username == username)
                .Select(u => new UserProfileDto(
                    u.Username,
                    u.Name,
                    u.AvatarUrl,
                    u.Bio,
                    u.Updates.Count,
                    u.Followers.Count,
                    u.Following.Count))
                .SingleOrDefaultAsync();

            if (user == null) return NotFound();
            return Ok(user);
        }

        // Returns follow/friend/pending state for client buttons (auth required)
        [Authorize]
        [HttpGet("{username}/state")]
        public async Task<IActionResult> GetUserState(string username)
        {
            var target = await _db.Users.SingleOrDefaultAsync(u => u.Username == username);
            if (target == null) return NotFound();

            var meId = CurrentUserId;
            if (meId == null)
            {
                // Not authenticated as a user (shouldn't happen because of [Authorize])
                return Ok(new UserStateDto(false, false, false, null));
            }

            var me = await _db.Users.FindAsync(meId.Value);
            if (me == null) return Unauthorized();

            // Following?
            var isFollowing = await _db.Follows.AnyAsync(f => f.FollowerId == me.Id && f.FollowingId == target.Id);

            // Friendship?
            var (a, b) = me.Id < target.Id ? (me.Id, target.Id) : (target.Id, me.Id);
            var isFriend = await _db.Friendships.AnyAsync(f => f.UserAId == a && f.UserBId == b);

            // Pending friend request (either direction)
            var hasPendingRequest = await _db.FriendRequests.AnyAsync(fr =>
                ((fr.FromUserId == me.Id && fr.ToUserId == target.Id) ||
                 (fr.FromUserId == target.Id && fr.ToUserId == me.Id)) &&
                 fr.Status == FriendRequestStatus.Pending);

            // If friends, return friendship id (optional)
            int? friendshipId = null;
            if (isFriend)
            {
                var friendship = await _db.Friendships.SingleOrDefaultAsync(f => f.UserAId == a && f.UserBId == b);
                friendshipId = friendship?.Id;
            }

            return Ok(new UserStateDto(isFollowing, isFriend, hasPendingRequest, friendshipId));
        }

        // Paged user updates (public)
        [AllowAnonymous]
        [HttpGet("{username}/updates")]
        public async Task<IActionResult> GetUserUpdates(string username, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound();

            var updates = await _db.Updates
                .Where(u => u.UserId == user.Id)
                .OrderByDescending(u => u.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UpdateDto(
                    u.Id,
                    user.Username,
                    user.AvatarUrl,
                    u.Content,
                    u.MediaUrl,
                    u.Timestamp,
                    u.Likes.Count,
                    u.Saves.Count))
                .ToListAsync();

            return Ok(updates);
        }

        // Saved updates for a user (only the owner can view their saved list)
        [Authorize]
        [HttpGet("{username}/saved")]
        public async Task<IActionResult> GetSavedUpdates(string username, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            // Only allow viewing saved updates if the requested username is the current user
            if (!string.Equals(username, CurrentUsername, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var me = await _db.Users.SingleOrDefaultAsync(u => u.Username == username);
            if (me == null) return NotFound();

            var saved = await _db.UpdateSaves
                .Where(s => s.UserId == me.Id)
                .OrderByDescending(s => s.SavedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(s => s.Update)
                    .ThenInclude(u => u.User)
                .Select(s => new UpdateDto(
                    s.Update.Id,
                    s.Update.User.Username,
                    s.Update.User.AvatarUrl,
                    s.Update.Content,
                    s.Update.MediaUrl,
                    s.Update.Timestamp,
                    s.Update.Likes.Count,
                    s.Update.Saves.Count))
                .ToListAsync();

            return Ok(saved);
        }

        // Lightweight current user summary
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var me = await _db.Users
                .Where(u => u.Username == CurrentUsername)
                .Select(u => new UserSummaryDto(u.Id, u.Username, u.Name, u.AvatarUrl))
                .SingleOrDefaultAsync();

            if (me == null) return Unauthorized();
            return Ok(me);
        }
        // Public: list of available users (lightweight summary)
        [AllowAnonymous]
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var users = await _db.Users
                .OrderBy(u => u.Username)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserSummaryDto(u.Id, u.Username, u.Name, u.AvatarUrl))
                .ToListAsync();

            return Ok(users);
        }
    }

    // DTOs
    public record UserProfileDto(string Username, string Name, string AvatarUrl, string? Bio, int UpdatesCount, int FollowersCount, int FollowingCount);
    public record UserStateDto(bool IsFollowing, bool IsFriend, bool HasPendingRequest, int? FriendshipId);
    public record UpdateDto(int Id, string Username, string AvatarUrl, string Content, string? MediaUrl, DateTime Timestamp, int Likes, int Saves);
    public record UserSummaryDto(int Id, string Username, string Name, string AvatarUrl);
}
