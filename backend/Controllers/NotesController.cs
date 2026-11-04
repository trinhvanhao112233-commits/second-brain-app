using PersonalFinance.API.Security;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.API.Data;
using PersonalFinance.API.DTOs;
using PersonalFinance.API.Models;

namespace PersonalFinance.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotesController : OwnedController
    {
        private readonly AppDbContext _context;

        public NotesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Note>>> GetNotes()
        {
            var query = _context.Notes.AsNoTracking().AsQueryable();
            if (userId.HasValue)
            {
                query = query.Where(n => n.UserId == userId.Value);
            }

            var notes = await query
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.UpdatedAt)
                .ToListAsync();

            return Ok(notes);
        }

        [HttpPost]
        public async Task<ActionResult<Note>> CreateNote(
            [FromBody] CreateNoteRequest request)
        {
            if (request.Id.HasValue) {
                var saved = await _context.Notes.FirstOrDefaultAsync(x => x.Id == request.Id.Value);
                if (saved != null) return Ok(saved);
            }
            var note = new Note
            {
                Id = request.Id ?? Guid.NewGuid(),
                UserId = userId,
                Title = request.Title.Trim(),
                Content = request.Content?.Trim() ?? string.Empty,
                Color = string.IsNullOrWhiteSpace(request.Color) ? "#3b82f6" : request.Color,
                IsPinned = request.IsPinned,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetNotes), new { id = note.Id }, note);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<Note>> UpdateNote(Guid id, [FromBody] UpdateNoteRequest request)
        {
            var note = await _context.Notes.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (note == null) return NotFound();

            note.Title = request.Title.Trim();
            note.Content = request.Content?.Trim() ?? string.Empty;
            note.Color = request.Color;
            note.IsPinned = request.IsPinned;
            note.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(note);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteNote(Guid id)
        {
            var note = await _context.Notes.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (note == null) return NotFound();

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
