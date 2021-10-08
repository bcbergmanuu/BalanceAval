using System.Reflection;
using BalanceAval.Models;
using Microsoft.EntityFrameworkCore;

namespace BalanceAval
{
    public class MyDbContext : DbContext
    {
        public DbSet<MeasurementRow> MeasurementRows { get; set; }
        public DbSet<MeasurementSlot> MeasurementSlots { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=Database.db", options =>
            {
                options.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
            });
            base.OnConfiguring(optionsBuilder);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Map table names
            modelBuilder.Entity<MeasurementRow>().ToTable("Measurements", "Rows");
            modelBuilder.Entity<MeasurementRow>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                //entity.HasIndex(e => e.Title).IsUnique();
                //entity.Property(e => e.DateTimeAdd).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<MeasurementSlot>().ToTable("MeasurementSlots", "Slots");
            modelBuilder.Entity<MeasurementSlot>(entity =>
            {
                entity.HasKey(e => e.Id);
                //entity.HasIndex(e => e.Title).IsUnique();
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Time).HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
            });
            modelBuilder.Entity<MeasurementSlot>()
                .HasMany<MeasurementRow>(slot => slot.MeasurementRows)
                .WithOne(tr => tr.MeasurementSlot).IsRequired();

            base.OnModelCreating(modelBuilder);
        }
    }
}