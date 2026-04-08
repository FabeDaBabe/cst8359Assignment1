using Assignment_1.Models;
using Microsoft.AspNetCore.Identity;

namespace Assignment_1.Data
{
    public class DbInitializer
    {
        public static async Task Initialize(AppDbContext context, IServiceProvider serviceProvider)
        {
            // Seed events if none exist
            if (!context.Events.Any())
            {
                var freshAttendee = new Attendee[]
                {
                    new Attendee { Id = Guid.NewGuid().ToString(), FullName = "John Johnson", Email = "John@example.com" },
                    new Attendee { Id = Guid.NewGuid().ToString(), FullName = "Jane Doe", Email = "Jane@example.com" }
                };

                var events = new Event[]
                {
                    new Event
                    {
                        Title = "Conference 2026",
                        Date = DateTime.Now.AddDays(10),
                        Location = "Main Hall",
                        BannerUrl = "",
                        Attendees = new List<Attendee>() { freshAttendee[0], freshAttendee[1] }
                    },
                    new Event
                    {
                        Title = "Monthly Meetup",
                        Date = DateTime.Now.AddDays(30),
                        Location = "Room 5",
                        BannerUrl = "",
                        Attendees = new List<Attendee>()
                    },
                    new Event
                    {
                        Title = "Job Fair",
                        Date = DateTime.Now.AddDays(55),
                        Location = "Gym",
                        BannerUrl = "",
                        Attendees = new List<Attendee>()
                    }
                };

                context.Events.AddRange(events);
                context.Attendees.AddRange(freshAttendee);
                context.SaveChanges();
            }

            // Seed roles and users
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // Create roles if they don't exist
            string[] roles = { "Organizer", "Attendee" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed Organizer user
            if (await userManager.FindByEmailAsync("organizer@example.com") == null)
            {
                var organizer = new IdentityUser
                {
                    UserName = "organizer@example.com",
                    Email = "organizer@example.com",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(organizer, "Organizer123!");
                await userManager.AddToRoleAsync(organizer, "Organizer");
            }

            // Seed Attendee user
            if (await userManager.FindByEmailAsync("attendee@example.com") == null)
            {
                var attendee = new IdentityUser
                {
                    UserName = "attendee@example.com",
                    Email = "attendee@example.com",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(attendee, "Attendee123!");
                await userManager.AddToRoleAsync(attendee, "Attendee");
            }
        }
    }
}