using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Anagram.Server.Data;
using Anagram.Server.Models;

namespace Anagram.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly AnagramDbContext _context;

        public FilesController(AnagramDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> InitiateFile([FromBody] FileTransfer file)
        {
            file.Timestamp = DateTime.UtcNow;
            _context.FileTransfers.Add(file);
            await _context.SaveChangesAsync();
            return Ok(file);
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetFiles(int userId)
        {
            var files = await _context.FileTransfers
                .Where(f => f.SenderId == userId || f.ReceiverId == userId)
                .OrderByDescending(f => f.Timestamp)
                .ToListAsync();

            return Ok(files);
        }
    }
}
