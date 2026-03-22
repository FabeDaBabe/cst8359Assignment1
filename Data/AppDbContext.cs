using Microsoft.EntityFrameworkCore;
using Assignment_1.Models;

namespace Assignment_1.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }

        public DbSet<Attendee> Attendees { get; set; }
    }
}