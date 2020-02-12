using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Models
{
    public class IssueTrackerContext : DbContext
    {
        public IssueTrackerContext(DbContextOptions options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Comment> Comments { get; set; }
    }
}