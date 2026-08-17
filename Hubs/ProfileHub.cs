using Anagram.Server.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;

[Authorize]
public class ProfileHub : Hub
{
    private readonly AnagramDbContext _db;
    public ProfileHub(AnagramDbContext db) { _db = db; }

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
        }

        await Clients.All.SendAsync("ProfileUpdated", userEmail, name, email, avatarUrl);
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
}
