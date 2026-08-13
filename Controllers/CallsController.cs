using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Anagram.Server.Data;
using Anagram.Server.Models;

namespace Anagram.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CallsController : ControllerBase
    {
        private readonly AnagramDbContext _context;

        public CallsController(AnagramDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> InitiateCall([FromBody] Call call)
        {
            call.StartTime = DateTime.UtcNow;
            _context.Calls.Add(call);
            await _context.SaveChangesAsync();
            return Ok(call);
        }

        [HttpPut("{id}/end")]
        public async Task<IActionResult> EndCall(int id)
        {
            var call = await _context.Calls.FindAsync(id);
            if (call == null) return NotFound();

            call.Status = "Ended";
            call.EndTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(call);
        }
    }
}
