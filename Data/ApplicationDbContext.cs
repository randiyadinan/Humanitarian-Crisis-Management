using Microsoft.EntityFrameworkCore;
using HumanitarianAPI.Models;

namespace HumanitarianAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Database Tables
        public DbSet<AssistanceRequest> AssistanceRequests { get; set; }
        public DbSet<Shelter> Shelters { get; set; }
        public DbSet<ShelterOccupant> ShelterOccupants { get; set; }
        public DbSet<Volunteer> Volunteers { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<Donation> Donations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<CrisisEvent> CrisisEvents { get; set; }
        public DbSet<Organisation> Organisations { get; set; }
        public DbSet<AIRecommendation> AIRecommendations { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // Database Relationships
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // AI Recommendation - Assistance Request
            modelBuilder.Entity<AIRecommendation>()
                .HasOne<AssistanceRequest>()
                .WithMany()
                .HasForeignKey(a => a.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // Shelter - Crisis Event
            modelBuilder.Entity<Shelter>()
                .HasOne<CrisisEvent>()
                .WithMany()
                .HasForeignKey(s => s.CrisisEventId)
                .OnDelete(DeleteBehavior.SetNull);

            // Resource - Crisis Event
            modelBuilder.Entity<Resource>()
                .HasOne<CrisisEvent>()
                .WithMany()
                .HasForeignKey(r => r.CrisisEventId)
                .OnDelete(DeleteBehavior.SetNull);

            // Volunteer - Crisis Event
            modelBuilder.Entity<Volunteer>()
                .HasOne<CrisisEvent>()
                .WithMany()
                .HasForeignKey(v => v.CrisisEventId)
                .OnDelete(DeleteBehavior.SetNull);

            // Donation - Crisis Event
            modelBuilder.Entity<Donation>()
                .HasOne<CrisisEvent>()
                .WithMany()
                .HasForeignKey(d => d.CrisisEventId)
                .OnDelete(DeleteBehavior.SetNull);

            // Resource - Shelter
            modelBuilder.Entity<Resource>()
                .HasOne<Shelter>()
                .WithMany()
                .HasForeignKey(r => r.ShelterId)
                .OnDelete(DeleteBehavior.SetNull);

            // Volunteer - Shelter
            modelBuilder.Entity<Volunteer>()
                .HasOne<Shelter>()
                .WithMany()
                .HasForeignKey(v => v.ShelterId)
                .OnDelete(DeleteBehavior.SetNull);

            // Database Integrity Constraints

            // Resource Quantity must not be negative
            modelBuilder.Entity<Resource>()
                .ToTable(table => table.HasCheckConstraint(
                    "CK_Resources_Quantity_NonNegative",
                    "\"Quantity\" >= 0"));

            // Shelter Capacity and Occupancy constraints
            modelBuilder.Entity<Shelter>()
                .ToTable(table =>
                {
                    table.HasCheckConstraint(
                        "CK_Shelters_Capacity_NonNegative",
                        "\"Capacity\" >= 0");

                    table.HasCheckConstraint(
                        "CK_Shelters_Occupancy_NonNegative",
                        "\"CurrentOccupancy\" >= 0");

                    table.HasCheckConstraint(
                        "CK_Shelters_Occupancy_WithinCapacity",
                        "\"CurrentOccupancy\" <= \"Capacity\"");
                });
        }
    }
}
