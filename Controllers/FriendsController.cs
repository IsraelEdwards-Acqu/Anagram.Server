using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Anagram.Server.Data;
using Anagram.Server.Models;
using Anagram.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Anagram.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FriendsController : ControllerBase
    {
        private readonly AnagramDbContext _db;
        private readonly IHubContext<SocialHub> _socialHub;

        public FriendsController(AnagramDbContext db, IHubContext<SocialHub> socialHub)
        {
            _db = db;
            _socialHub = socialHub;
        }

        private string CurrentUsername => User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        private async Task<User?> GetCurrentUserAsync()
        {
            return await _db.Users.SingleOrDefaultAsync(u => u.Username == CurrentUsername);
        }

        [HttpPost("request")]
        public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestDto dto)
        {
            var from = await GetCurrentUserAsync();
            if (from == null) return Unauthorized();

            var to = await _db.Users.SingleOrDefaultAsync(u => u.Username == dto.ToUsername);
            if (to == null) return NotFound("User not found");
            if (to.Id == from.Id) return BadRequest("Cannot friend yourself");

            // Prevent duplicate friendship
            var (a, b) = NormalizePair(from.Id, to.Id);
            var existingFriendship = await _db.Friendships.SingleOrDefaultAsync(f => f.UserAId == a && f.UserBId == b);
            if (existingFriendship != null) return BadRequest("Already friends");

            // Prevent duplicate pending request
            var existingRequest = await _db.FriendRequests
                .SingleOrDefaultAsync(fr => fr.FromUserId == from.Id && fr.ToUserId == to.Id && fr.Status == FriendRequestStatus.Pending);
            if (existingRequest != null) return BadRequest("Request already sent");

            var request = new FriendRequest { FromUserId = from.Id, ToUserId = to.Id };
            _db.FriendRequests.Add(request);
            await _db.SaveChangesAsync();

            // Notify recipient via SignalR (by username)
            await _socialHub.Clients.User(to.Username).SendAsync("FriendRequestSent", new { request.Id, From = from.Username });

            return Ok(new { request.Id });
        }

        [HttpPost("respond")]
        public async Task<IActionResult> Respond([FromBody] RespondFriendRequestDto dto)
        {
            var me = await GetCurrentUserAsync();
            if (me == null) return Unauthorized();

            var request = await _db.FriendRequests.Include(fr => fr.FromUser).Include(fr => fr.ToUser)
                .SingleOrDefaultAsync(fr => fr.Id == dto.RequestId && fr.ToUserId == me.Id);
            if (request == null) return NotFound("Request not found");

            request.Status = dto.Accept ? FriendRequestStatus.Accepted : FriendRequestStatus.Declined;
            request.RespondedAt = DateTime.UtcNow;

            if (dto.Accept)
            {
                // create normalized friendship
                var (a, b) = NormalizePair(request.FromUserId, request.ToUserId);
                var existing = await _db.Friendships.SingleOrDefaultAsync(f => f.UserAId == a && f.UserBId == b);
                if (existing == null)
                {
                    var friendship = new Friendship { UserAId = a, UserBId = b };
                    _db.Friendships.Add(friendship);
                }
            }

            await _db.SaveChangesAsync();

            // Notify both parties
            await _socialHub.Clients.User(request.FromUser.Username).SendAsync("FriendRequestUpdated", new
            {
                Id = request.Id,
                Status = request.Status.ToString()
            });
            await _socialHub.Clients.User(request.ToUser.Username).SendAsync("FriendRequestUpdated", new
            {
                Id = request.Id,
                Status = request.Status.ToString()
            });


            return Ok(new { status = request.Status.ToString() });
        }

        [HttpGet("requests")]
        public async Task<IActionResult> GetIncomingRequests()
        {
            var me = await GetCurrentUserAsync();
            if (me == null) return Unauthorized();

            var requests = await _db.FriendRequests
                .Where(fr => fr.ToUserId == me.Id && fr.Status == FriendRequestStatus.Pending)
                .Select(fr => new { fr.Id, FromUsername = fr.FromUser.Username, fr.CreatedAt })
                .ToListAsync();

            return Ok(requests);
        }

        [HttpGet("list/{username?}")]
        public async Task<IActionResult> GetFriends(string? username = null)
        {
            username ??= CurrentUsername;
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound();

            var friendships = await _db.Friendships
                .Where(f => f.UserAId == user.Id || f.UserBId == user.Id)
                .ToListAsync();

            var friendIds = friendships.Select(f => f.UserAId == user.Id ? f.UserBId : f.UserAId).ToList();
            var friends = await _db.Users.Where(u => friendIds.Contains(u.Id)).Select(u => new { u.Username, u.Name, u.AvatarUrl }).ToListAsync();

            return Ok(friends);
        }

        [HttpDelete("{friendshipId}")]
        public async Task<IActionResult> Unfriend(int friendshipId)
        {
            var me = await GetCurrentUserAsync();
            if (me == null) return Unauthorized();

            var friendship = await _db.Friendships.SingleOrDefaultAsync(f => f.Id == friendshipId &&
                (f.UserAId == me.Id || f.UserBId == me.Id));
            if (friendship == null) return NotFound();

            _db.Friendships.Remove(friendship);
            await _db.SaveChangesAsync();

            // Notify other user
            var otherId = friendship.UserAId == me.Id ? friendship.UserBId : friendship.UserAId;
            var other = await _db.Users.FindAsync(otherId);
            if (other != null)
            {
                await _socialHub.Clients.User(other.Username).SendAsync("Unfriended", new { By = me.Username });
            }

            return NoContent();
        }

        private static (int a, int b) NormalizePair(int id1, int id2)
        {
            return id1 < id2 ? (id1, id2) : (id2, id1);
        }
    }

    public record SendFriendRequestDto(string ToUsername);
    public record RespondFriendRequestDto(int RequestId, bool Accept);
}
