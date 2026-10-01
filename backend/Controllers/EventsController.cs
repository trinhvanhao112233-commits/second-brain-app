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
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EventsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CalendarEvent>>> GetEvents(
            [FromHeader(Name = "X-User-Id")] Guid? userId,
            [FromQuery] int? month, 
            [FromQuery] int? year)
        {
            var query = _context.Events.AsNoTracking().AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(e => e.UserId == userId.Value);
            }

            if (year.HasValue && month.HasValue)
            {
                var startDate = new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
                var endDate = startDate.AddMonths(1);
                query = query.Where(e => (e.StartTime >= startDate && e.StartTime < endDate) || 
                                         (e.EndTime >= startDate && e.EndTime < endDate));
            }

            var events = await query.OrderBy(e => e.StartTime).ToListAsync();
            return Ok(events);
        }

        [HttpPost]
        public async Task<ActionResult<CalendarEvent>> CreateEvent(
            [FromHeader(Name = "X-User-Id")] Guid? userId,
            [FromBody] CreateEventRequest request)
        {
            var calendarEvent = new CalendarEvent
            {
                UserId = userId,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Category = string.IsNullOrWhiteSpace(request.Category) ? "Personal" : request.Category,
                Color = string.IsNullOrWhiteSpace(request.Color) ? "#10b981" : request.Color,
                CreatedAt = DateTime.UtcNow
            };

            _context.Events.Add(calendarEvent);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvents), new { id = calendarEvent.Id }, calendarEvent);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CalendarEvent>> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request)
        {
            var existing = await _context.Events.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Title = request.Title.Trim();
            existing.Description = request.Description?.Trim();
            existing.StartTime = request.StartTime;
            existing.EndTime = request.EndTime;
            existing.Category = request.Category;
            existing.Color = request.Color;
            existing.IsCompleted = request.IsCompleted;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteEvent(Guid id)
        {
            var existing = await _context.Events.FindAsync(id);
            if (existing == null) return NotFound();

            _context.Events.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
