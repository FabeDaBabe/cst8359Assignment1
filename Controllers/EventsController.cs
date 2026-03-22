using Assignment_1.Data;
using Assignment_1.Models;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assignment_1.Controllers
{
    [Route("Events")]
    public class EventsController : Controller
    {

        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public EventsController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        //Banner Helper
        private async Task<string> UploadBannerAsync(IFormFile file)
        {
            var connectionString = _configuration["AzureStorage:ConnectionString"];
            var containerName = _configuration["AzureStorage:ContainerName"];

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var blobClient = containerClient.GetBlobClient(fileName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: true);

            return blobClient.Uri.ToString();
        }


        //INDEX
        [Route("")]
        [Route("/")]
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events.Include(e => e.Attendees).ToListAsync();
            return View(events);
        }

        //ATTENDEES
        [Route("{eventId}/attendees")]
        public async Task<IActionResult> Attendees(int eventId)
        {

            var ev = await _context.Events.Include(e =>  e.Attendees).FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev is null) return RedirectToAction("Index");
            return View(ev);
        }

        [Route("{eventId}/attendees")]
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
            newAttendee.EventId = eventId;

           
            _context.Attendees.Add(newAttendee);
            await _context.SaveChangesAsync();

            return RedirectToAction("Attendees", new { eventId });
        }

        //DETAILS
        [Route("{id}/details")]
        public async Task<IActionResult> Details(int id)
        {
            var ev = await _context.Events.Include(e => e.Attendees).FirstOrDefaultAsync(e => e.Id == id);
            if (ev is null) return RedirectToAction("Index");
            return View(ev);
        }

        // CREATE
        [Route("create")]
        public IActionResult Create()
        {
            return View();
        }

        [Route("create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event ev, IFormFile? bannerFile)
        {
            if (!ModelState.IsValid) return View(ev);

            if (bannerFile != null && bannerFile.Length > 0)
            {
                ev.BannerUrl = await UploadBannerAsync(bannerFile);
            }

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // EDIT
        [Route("{id}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev is null) return RedirectToAction("Index");
            return View(ev);
        }

        [Route("{id}/edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event ev, IFormFile? bannerFile)
        {
            if (id != ev.Id) return RedirectToAction("Index");
            if (!ModelState.IsValid) return View(ev);
            if (bannerFile != null && bannerFile.Length > 0)
            {
                ev.BannerUrl = await UploadBannerAsync(bannerFile);
            }

            _context.Events.Update(ev);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // DELETE
        [Route("{id}/delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Attendees)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (ev is null) return RedirectToAction("Index");
            return View(ev);
        }

        [Route("{id}/delete")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Attendees)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (ev is not null)
            {
                _context.Attendees.RemoveRange(ev.Attendees);
                _context.Events.Remove(ev);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        // EDIT ATTENDEE
        [HttpGet]
        [Route("{eventId}/attendees/{id}/edit")]
        public async Task<IActionResult> EditAttendee(string id, int eventId)
        {
            var attendee = await _context.Attendees.FindAsync(id);
            if (attendee is null) return RedirectToAction("Attendees", new { eventId });
            return View(attendee);
        }

        [Route("{eventId}/attendees/{id}/edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAttendee(string id, Attendee attendee, int eventId)
        {
            if (id != attendee.Id) return RedirectToAction("Attendees", new { eventId });
            if (!ModelState.IsValid) return View(attendee);
            _context.Attendees.Update(attendee);
            await _context.SaveChangesAsync();
            return RedirectToAction("Attendees", new { eventId });
        }

        // DELETE ATTENDEE
        [Route("{eventId}/attendees/{id}/delete")]
        public async Task<IActionResult> DeleteAttendee(string id, int eventId)
        {
            var attendee = await _context.Attendees.FindAsync(id);
            if (attendee is null) return RedirectToAction("Attendees", new { eventId });
            return View(attendee);
        }

        [Route("{eventId}/attendees/{id}/delete")]
        [HttpPost, ActionName("DeleteAttendee")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttendeeConfirmed(string id, int eventId)
        {
            var attendee = await _context.Attendees.FindAsync(id);
            if (attendee is not null)
            {
                _context.Attendees.Remove(attendee);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Attendees", new { eventId });
        }
    }
}