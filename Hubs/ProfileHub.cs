using Anagram.Server.Data;
using Anagram.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Linq;
using System.Threading.Tasks;

[Authorize]
public class ProfileHub : Hub
{
    private readonly AnagramDbContext _db;
    public ProfileHub(AnagramDbContext db) { _db = db; }

    // ---------------- PROFILE ----------------
    public async Task UpdateProfile(string name, string email, string avatarUrl)
    {
        var userEmail = Context.User?.Identity?.Name;
        var user = _db.Users.SingleOrDefault(u => u.Email == userEmail);
        if (user != null)
        {
            user.Name = name;
            user.Email = email;
            user.AvatarUrl = avatarUrl;
            await _db.SaveChangesAsync();

            await Clients.All.SendAsync("ProfileUpdated", user.Email, user.Name, user.Email, user.AvatarUrl);
        }
    }

    public async Task RequestProfile()
    {
        var userEmail = Context.User?.Identity?.Name;
        var user = _db.Users.SingleOrDefault(u => u.Email == userEmail);
        if (user != null)
        {
            await Clients.Caller.SendAsync("ProfileData", user.Email, user.Name, user.Email, user.AvatarUrl);
        }
    }

    public async Task RequestProfileByUsername(string username)
    {
        var callerEmail = Context.User?.Identity?.Name;
        var caller = _db.Users.SingleOrDefault(u => u.Email == callerEmail);
        var target = _db.Users.SingleOrDefault(u => u.Username == username);

        if (caller == null || target == null) return;

        bool allowed = _db.Friendships.Any(f =>
            (f.UserAId == caller.Id && f.UserBId == target.Id) ||
            (f.UserAId == target.Id && f.UserBId == caller.Id));

        if (allowed)
        {
            await Clients.Caller.SendAsync("ProfileData", target.Email, target.Name, target.Email, target.AvatarUrl);
        }
        else
        {
            await Clients.Caller.SendAsync("ProfileData", "", "Access denied", "", "/images/default-avatar.png");
        }
    }

    // ---------------- FRIEND REQUESTS ----------------
    public async Task SendFriendRequest(string toUsername)
    {
        var callerEmail = Context.User?.Identity?.Name;
        var caller = _db.Users.SingleOrDefault(u => u.Email == callerEmail);
        var target = _db.Users.SingleOrDefault(u => u.Username == toUsername);

        if (caller == null || target == null) return;

        if (!_db.FriendRequests.Any(fr => fr.FromUserId == caller.Id && fr.ToUserId == target.Id))
        {
            var request = new FriendRequest
            {
                FromUserId = caller.Id,
                ToUserId = target.Id
            };
            _db.FriendRequests.Add(request);
            await _db.SaveChangesAsync();

            await Clients.User(target.Email).SendAsync("FriendRequestReceived", caller.Username);
        }
    }

    public async Task AcceptFriendRequest(int requestId)
    {
        var callerEmail = Context.User?.Identity?.Name;
        var caller = _db.Users.SingleOrDefault(u => u.Email == callerEmail);

        var request = _db.FriendRequests.SingleOrDefault(fr => fr.Id == requestId && fr.ToUserId == caller.Id);
        if (request == null) return;

        request.IsAccepted = true;

        int userAId = System.Math.Min(request.FromUserId, request.ToUserId);
        int userBId = System.Math.Max(request.FromUserId, request.ToUserId);

        if (!_db.Friendships.Any(f => f.UserAId == userAId && f.UserBId == userBId))
        {
            var friendship = new Friendship
            {
                UserAId = userAId,
                UserBId = userBId
            };
            _db.Friendships.Add(friendship);
        }

        await _db.SaveChangesAsync();

        var fromUser = _db.Users.SingleOrDefault(u => u.Id == request.FromUserId);
        var toUser = _db.Users.SingleOrDefault(u => u.Id == request.ToUserId);

        if (fromUser != null && toUser != null)
        {
            await Clients.User(fromUser.Email).SendAsync("FriendRequestAccepted", toUser.Username);
            await Clients.User(toUser.Email).SendAsync("FriendRequestAccepted", fromUser.Username);
        }
    }

    public async Task RejectFriendRequest(int requestId)
    {
        var callerEmail = Context.User?.Identity?.Name;
        var caller = _db.Users.SingleOrDefault(u => u.Email == callerEmail);

        var request = _db.FriendRequests.SingleOrDefault(fr => fr.Id == requestId && fr.ToUserId == caller.Id);
        if (request == null) return;

        _db.FriendRequests.Remove(request);
        await _db.SaveChangesAsync();

        await Clients.User(request.FromUser.Email).SendAsync("FriendRequestRejected", caller.Username);
    }

    // ---------------- FOLLOW ----------------
    public async Task FollowUser(string targetUsername)
    {
        var callerEmail = Context.User?.Identity?.Name;
        var caller = _db.Users.SingleOrDefault(u => u.Email == callerEmail);
        var target = _db.Users.SingleOrDefault(u => u.Username == targetUsername);

        if (caller == null || target == null) return;

        if (!_db.Follows.Any(f => f.FollowerId == caller.Id && f.FollowingId == target.Id))
        {
            var follow = new Follow
            {
                FollowerId = caller.Id,
                FollowingId = target.Id
            };
            _db.Follows.Add(follow);
            await _db.SaveChangesAsync();

            await Clients.User(target.Email).SendAsync("FollowChanged", caller.Username, true);
        }
    }

    public async Task UnfollowUser(string targetUsername)
    {
        var callerEmail = Context.User?.Identity?.Name;
        var caller = _db.Users.SingleOrDefault(u => u.Email == callerEmail);
        var target = _db.Users.SingleOrDefault(u => u.Username == targetUsername);

        if (caller == null || target == null) return;

        var follow = _db.Follows.SingleOrDefault(f => f.FollowerId == caller.Id && f.FollowingId == target.Id);
        if (follow != null)
        {
            _db.Follows.Remove(follow);
            await _db.SaveChangesAsync();

            await Clients.User(target.Email).SendAsync("FollowChanged", caller.Username, false);
        }
    }
}
