using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Anagram.Server.Data;
using Anagram.Server.Models;

namespace Anagram.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VoiceNotesController : ControllerBase
    {
        private readonly AnagramDbContext _context;

        public VoiceNotesController(AnagramDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> UploadVoiceNote([FromBody] VoiceNote note)
        {
            note.Timestamp = DateTime.UtcNow;
            _context.VoiceNotes.Add(note);
            await _context.SaveChangesAsync();
            return Ok(note);
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetVoiceNotes(int userId)
        {
            var notes = await _context.VoiceNotes
                .Where(v => v.SenderId == userId || v.ReceiverId == userId)
                .OrderByDescending(v => v.Timestamp)
                .ToListAsync();

            return Ok(notes);
        }
    }
}
