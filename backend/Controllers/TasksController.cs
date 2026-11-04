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
    public class TasksController : OwnedController
    {
        private readonly AppDbContext _context;

        public TasksController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DailyTask>>> GetTasks()
        {
            var query = _context.Tasks.AsNoTracking().AsQueryable();
            if (userId.HasValue)
            {
                query = query.Where(t => t.UserId == userId.Value);
            }

            var tasks = await query
                .OrderBy(t => t.IsCompleted)
                .ThenBy(t => t.DueDate)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Ok(tasks);
        }

        [HttpPost]
        public async Task<ActionResult<DailyTask>> CreateTask(
            [FromBody] CreateTaskRequest request)
        {
            if (request.Id.HasValue) {
                var saved = await _context.Tasks.FirstOrDefaultAsync(x => x.Id == request.Id.Value);
                if (saved != null) return Ok(saved);
            }
            var task = new DailyTask
            {
                Id = request.Id ?? Guid.NewGuid(),
                UserId = userId,
                Title = request.Title.Trim(),
                DueDate = request.DueDate,
                Priority = string.IsNullOrWhiteSpace(request.Priority) ? "Medium" : request.Priority,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTasks), new { id = task.Id }, task);
        }

        [HttpPatch("{id:guid}/toggle")]
        public async Task<ActionResult<DailyTask>> ToggleTask(Guid id)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (task == null) return NotFound();

            task.IsCompleted = !task.IsCompleted;
            await _context.SaveChangesAsync();

            return Ok(task);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<DailyTask>> UpdateTask(Guid id, [FromBody] UpdateTaskRequest request)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (task == null) return NotFound();

            task.Title = request.Title.Trim();
            task.DueDate = request.DueDate;
            task.Priority = request.Priority;
            task.IsCompleted = request.IsCompleted;

            await _context.SaveChangesAsync();
            return Ok(task);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTask(Guid id)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (task == null) return NotFound();

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
