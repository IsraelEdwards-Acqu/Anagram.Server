using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Anagram.Server.Data;
using Anagram.Server.Models;

namespace Anagram.Server.Services
{
    public class SocialService
    {
        private readonly AnagramDbContext _db;
        public SocialService(AnagramDbContext db) { _db = db; }

        public (int a, int b) NormalizePair(int id1, int id2) => id1 < id2 ? (id1, id2) : (id2, id1);

        public async Task<bool> AreFriendsAsync(int id1, int id2)
        {
            var (a, b) = NormalizePair(id1, id2);
            return await _db.Friendships.AnyAsync(f => f.UserAId == a && f.UserBId == b);
        }
    }
}
