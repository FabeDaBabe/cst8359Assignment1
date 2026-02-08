using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Assignment_1.Models;

namespace Assignment_1.Controllers
{
    public class EventsController : Controller
    {
        private static readonly List<Event> _events = new()
        {
            new Event
            {
                Id = 1,
                Title = "Conference 2026",
                Date = DateTime.Now.AddDays(10),
                Location = "Main Hall",
                Attendees = new List<Attendee>
                {
                    new Attendee { Id = 1, FullName = "John Johnson", Email = "John@example.com" }
                }
            },
            new Event
            {
                Id = 2,
                Title = "Monthly Meetup",
                Date = DateTime.Now.AddDays(30),
                Location = "Room 5",
                Attendees = new List<Attendee>()
            },
             new Event
            {
                Id = 3,
                Title = "Job Fair",
                Date = DateTime.Now.AddDays(55),
                Location = "Gym",
                Attendees = new List<Attendee>()
            }
        };

        public IActionResult Index()
        {
            return View(_events);
        }

        public IActionResult Attendees(int eventId)
        {
            var ev = _events.FirstOrDefault(e => e.Id == eventId);
            if (ev is null) return RedirectToAction("Index");
            return View(ev);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Attendees(int eventId, Attendee newAttendee)
        {
            var ev = _events.FirstOrDefault(e => e.Id == eventId);
            if (ev is null) return RedirectToAction("Index");

            if (string.IsNullOrWhiteSpace(newAttendee?.FullName))
            {
                ModelState.AddModelError("FullName", "Name is required.");
                return View(ev);
            }

            var nextId = _events.SelectMany(e => e.Attendees).Select(a => a.Id).DefaultIfEmpty(0).Max() + 1;
            newAttendee.Id = nextId;
            ev.Attendees.Add(newAttendee);

            return RedirectToAction("Index");
        }
    }
}