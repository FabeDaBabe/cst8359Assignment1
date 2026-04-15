using Assignment_1.Data;
using Assignment_1.Models;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Assignment_1.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Assignment_1.Controllers
{
    [Route("Events")]
    public class EventsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<EventHub> _hubContext; // NEW

        public EventsController(AppDbContext context, IConfiguration configuration, IHubContext<EventHub> hubContext)
        {
            _context = context;
            _configuration = configuration;
            _hubContext = hubContext; 
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

        //INDEX - public
        [AllowAnonymous]
        [Route("")]
        [Route("/")]
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events.Include(e => e.Attendees).ToListAsync();
            return View(events);
        }

        //DETAILS - any authenticated user
        [Authorize]
        [Route("{id}/details")]
        public async Task<IActionResult> Details(int id)
        {
            var ev = await _context.Events.Include(e => e.Attendees).FirstOrDefaultAsync(e => e.Id == id);
            if (ev is null) return RedirectToAction("Index");
            return View(ev);
        }

        //ATTENDEES - any authenticated user can view
        [Authorize]
        [Route("{eventId}/attendees")]
        public async Task<IActionResult> Attendees(int eventId)
        {
            var ev = await _context.Events.Include(e => e.Attendees).FirstOrDefaultAsync(e => e.Id == eventId);
            if (ev is null) return RedirectToAction("Index");
            return View(ev);
        }

        //ADD ATTENDEE - Organizer only
        [Authorize(Roles = "Organizer")]
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

        // CREATE - Organizer only
        [Authorize(Roles = "Organizer")]
        [Route("create")]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "Organizer")]
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

            ev.OrganizerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _context.Events.Add(ev);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // EDIT - Organizer only
        [Authorize(Roles = "Organizer")]
        [Route("{id}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev is null) return RedirectToAction("Index");
            return View(ev);
        }

        [Authorize(Roles = "Organizer")]
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

        // DELETE - Organizer only
        [Authorize(Roles = "Organizer")]
        [Route("{id}/delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Attendees)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (ev is null) return RedirectToAction("Index");
            return View(ev);
        }

        [Authorize(Roles = "Organizer")]
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

        // EDIT ATTENDEE - Organizer only
        [Authorize(Roles = "Organizer")]
        [HttpGet]
        [Route("{eventId}/attendees/{id}/edit")]
        public async Task<IActionResult> EditAttendee(string id, int eventId)
        {
            var attendee = await _context.Attendees.FindAsync(id);
            if (attendee is null) return RedirectToAction("Attendees", new { eventId });
            return View(attendee);
        }

        [Authorize(Roles = "Organizer")]
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

        // DELETE ATTENDEE - Organizer only
        [Authorize(Roles = "Organizer")]
        [Route("{eventId}/attendees/{id}/delete")]
        public async Task<IActionResult> DeleteAttendee(string id, int eventId)
        {
            var attendee = await _context.Attendees.FindAsync(id);
            if (attendee is null) return RedirectToAction("Attendees", new { eventId });
            return View(attendee);
        }

        [Authorize(Roles = "Organizer")]
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

        // SELF-REGISTER - any logged-in user
        [Authorize]
        [HttpPost]
        [Route("{eventId}/register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(int eventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var userName = User.Identity?.Name;

            // Prevent duplicate registration
            var already = await _context.Attendees
                .AnyAsync(a => a.EventId == eventId && a.UserId == userId);

            if (already)
            {
                TempData["Error"] = "You are already registered for this event.";
                return RedirectToAction("Details", new { id = eventId });
            }

            var attendee = new Attendee
            {
                Id = Guid.NewGuid().ToString(),
                EventId = eventId,
                UserId = userId,
                FullName = userName,
                Email = userEmail
            };

            _context.Attendees.Add(attendee);
            await _context.SaveChangesAsync();

            // Get updated attendee count
            var count = await _context.Attendees.CountAsync(a => a.EventId == eventId);

            // Broadcast to everyone viewing this event
            await _hubContext.Clients.Group($"event-{eventId}")
                .SendAsync("AttendeeRegistered", userName, count);

            // Private notification to the event organizer
            var ev = await _context.Events.FindAsync(eventId);
            if (ev?.OrganizerUserId != null)
            {
                await _hubContext.Clients.User(ev.OrganizerUserId)
                    .SendAsync("OrganizerNotification", $"{userEmail} just registered for your {ev.Title}.");
            }

            TempData["Success"] = "You have successfully registered for this event!";
            return RedirectToAction("Details", new { id = eventId });
        }

        // SELF-UNREGISTER - logged-in user removes their own registration
        [Authorize]
        [HttpPost]
        [Route("{eventId}/unregister")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unregister(int eventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var attendee = await _context.Attendees
                .FirstOrDefaultAsync(a => a.EventId == eventId && a.UserId == userId);

            if (attendee != null)
            {
                _context.Attendees.Remove(attendee);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "You have been unregistered from this event.";
            return RedirectToAction("Details", new { id = eventId });
        }
    }
}