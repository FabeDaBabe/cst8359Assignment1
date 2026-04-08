using Assignment_1.Data;
using Assignment_1.Models;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    }
}