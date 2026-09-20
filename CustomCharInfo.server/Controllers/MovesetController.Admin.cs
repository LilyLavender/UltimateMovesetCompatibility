// Admin-only notes on movesets. Never part of the public API group and never joined into a public projection.
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace CustomCharInfo.server.Controllers
{
    public partial class MovesetController
    {
        [Authorize]
        [HttpGet("admin-notes")]
        public async Task<IActionResult> GetAdminNotes()
        {
            var user = await _userManager.GetRequesterAsync(_context, User);
            if (!user.IsAdmin())
                return Forbid();

            var notes = await _context.MovesetAdminNotes
                .AsNoTracking()
                .Select(n => new
                {
                    n.MovesetId,
                    n.Note,
                    UpdatedByUserName = n.UpdatedBy != null ? n.UpdatedBy.UserName : null,
                    n.UpdatedAt
                })
                .ToListAsync();

            return Ok(notes);
        }

        // Upserts the note; an empty note removes it.
        [Authorize]
        [HttpPut("{id}/admin-note")]
        public async Task<IActionResult> SetAdminNote(int id, [FromBody] MovesetAdminNoteDto dto)
        {
            var user = await _userManager.GetRequesterAsync(_context, User);
            if (!user.IsAdmin())
                return Forbid();

            if (!await _context.Movesets.AnyAsync(m => m.MovesetId == id))
                return NotFound();

            var text = dto?.Note?.Trim() ?? "";
            var existing = await _context.MovesetAdminNotes.FindAsync(id);

            if (text.Length == 0)
            {
                if (existing != null)
                    _context.MovesetAdminNotes.Remove(existing);
                await _context.SaveChangesAsync();
                return NoContent();
            }

            if (existing == null)
            {
                existing = new MovesetAdminNote { MovesetId = id };
                _context.MovesetAdminNotes.Add(existing);
            }
            existing.Note = text;
            existing.UpdatedByUserId = user.Id;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                existing.MovesetId,
                existing.Note,
                UpdatedByUserName = user.UserName,
                existing.UpdatedAt
            });
        }
    }
}
