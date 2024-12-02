using Microsoft.EntityFrameworkCore;

namespace DAMmodels
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Soldier> Soldiers { get; set; } // This maps to the Soldiers table

        // Optionally override OnModelCreating to define additional configuration
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}