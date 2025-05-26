using Microsoft.EntityFrameworkCore;

namespace CarRentalAPIWebApp.Models
{
    public class CarRentalAPIContext : DbContext
    {
        public virtual DbSet<Car> Cars { get; set; }
        public virtual DbSet<Review> Reviews { get; set; }
        public virtual DbSet<Booking> Bookings { get; set; }
        public virtual DbSet<CarStatusType> CarStatusTypes { get; set; }
        public virtual DbSet<BookingStatusType> BookingStatusTypes { get; set; }

        public CarRentalAPIContext(DbContextOptions<CarRentalAPIContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Зв’язок Booking -> Car
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Car)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.CarId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(true);

            // Зв’язок Booking -> BookingStatusType
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Status)
                .WithMany()
                .HasForeignKey(b => b.StatusId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(true);

            // Зв’язок Car -> CarStatusType
            modelBuilder.Entity<Car>()
                .HasOne(c => c.Status)
                .WithMany()
                .HasForeignKey(c => c.StatusId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(true);

            // Конфігурація для PricePerDay
            modelBuilder.Entity<Car>()
                .Property(c => c.PricePerDay)
                .HasColumnType("decimal(18,2)");
        }
    }
}