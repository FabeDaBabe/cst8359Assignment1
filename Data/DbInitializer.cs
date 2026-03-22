using Assignment_1.Models;

namespace Assignment_1.Data
{
    public class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            // If there are already RSVPs in the database, don't seed
            if (context.Events.Any())
            {
                return;
            }


            var freshAttendee = new Attendee[]
            {
                new Attendee { Id = Guid.NewGuid().ToString(), FullName = "John Johnson", Email = "John@example.com" },
                new Attendee { Id = Guid.NewGuid().ToString(), FullName = "Jane Doe", Email = "Jane@example.com" }
        };

            var Event = new Event[]
                {
               new Event
            {
                
                Title = "Conference 2026",
                Date = DateTime.Now.AddDays(10),
                Location = "Main Hall",
                BannerUrl = "",
                Attendees = new List<Attendee>()
                {
                    freshAttendee[0], freshAttendee[1]
                }
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

            context.Events.AddRange(Event);
            context.Attendees.AddRange(freshAttendee);
            context.SaveChanges();
        }

    }
}
