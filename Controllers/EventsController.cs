using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Assignment_1.Models;
using Assignment_1.Data;

namespace Assignment_1.Controllers
{
    public class EventsController : Controller
    {

        private readonly AppDbContext _context;

        public EventsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var events = await _context.Events.Include(e => e.Attendees).ToListAsync();
            return View(events);
        }

        public async Task<IActionResult> Attendees(int eventId)
        {

            var ev = await _context.Events.Include(e =>  e.Attendees).FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev is null) return RedirectToAction("Index");
            return View(ev);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Attendees(int eventId, Attendee newAttendee)
        {
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (ev is null) return RedirectToAction("Index");

            if (string.IsNullOrWhiteSpace(newAttendee?.FullName))
            {
                ModelState.AddModelError("Name", "Name is required.");
                return View(ev);
            }

            newAttendee.Id = Guid.NewGuid().ToString();

            _context.Attendees.Add(newAttendee);
            await _context.SaveChangesAsync();

            return RedirectToAction("Attendees", new { eventId });
        }
    }
}