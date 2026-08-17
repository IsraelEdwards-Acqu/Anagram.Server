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
    public class FollowController : ControllerBase
    {
        private readonly AnagramDbContext _db;
        private readonly IHubContext<SocialHub> _socialHub;

        public FollowController(AnagramDbContext db, IHubContext<SocialHub> socialHub)
        {
            _db = db;
            _socialHub = socialHub;
        }

        private string CurrentUsername => User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        [HttpPost("{username}")]
        public async Task<IActionResult> Follow(string username)
        {
            var me = await _db.Users.SingleOrDefaultAsync(u => u.Username == CurrentUsername);
            if (me == null) return Unauthorized();

            var target = await _db.Users.SingleOrDefaultAsync(u => u.Username == username);
            if (target == null) return NotFound();

            if (me.Id == target.Id) return BadRequest("Cannot follow yourself");

            var exists = await _db.Follows.AnyAsync(f => f.FollowerId == me.Id && f.FollowingId == target.Id);
            if (exists) return BadRequest("Already following");

            var follow = new Follow { FollowerId = me.Id, FollowingId = target.Id };
            _db.Follows.Add(follow);
            await _db.SaveChangesAsync();

            await _socialHub.Clients.User(target.Username).SendAsync("Followed", new { Follower = me.Username });

            return Ok();
        }

        [HttpDelete("{username}")]
        public async Task<IActionResult> Unfollow(string username)
        {
            var me = await _db.Users.SingleOrDefaultAsync(u => u.Username == CurrentUsername);
            if (me == null) return Unauthorized();

            var target = await _db.Users.SingleOrDefaultAsync(u => u.Username == username);
            if (target == null) return NotFound();

            var follow = await _db.Follows.SingleOrDefaultAsync(f => f.FollowerId == me.Id && f.FollowingId == target.Id);
            if (follow == null) return NotFound();

            _db.Follows.Remove(follow);
            await _db.SaveChangesAsync();

            await _socialHub.Clients.User(target.Username).SendAsync("Unfollowed", new { By = me.Username });

            return NoContent();
        }

        [HttpGet("followers/{username}")]
        public async Task<IActionResult> GetFollowers(string username)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound();

            var followers = await _db.Follows
                .Where(f => f.FollowingId == user.Id)
                .Select(f => new { f.Follower.Username, f.Follower.AvatarUrl })
                .ToListAsync();

            return Ok(followers);
        }

        [HttpGet("following/{username}")]
        public async Task<IActionResult> GetFollowing(string username)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound();

            var following = await _db.Follows
                .Where(f => f.FollowerId == user.Id)
                .Select(f => new { f.Following.Username, f.Following.AvatarUrl })
                .ToListAsync();

            return Ok(following);
        }
    }
}
