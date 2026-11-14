using Microsoft.EntityFrameworkCore;
using RideTogether.Entities;
using RideTogether.Models;

namespace RideTogether.Context;

public partial class RideTogetherDbContext : DbContext
{
    public RideTogetherDbContext()
    {
    }

    public RideTogetherDbContext(DbContextOptions<RideTogetherDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<Ride> Rides { get; set; }
    public DbSet<RideBooking> RideBookings => Set<RideBooking>();
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user => { user.ToTable("User"); });

        modelBuilder.Entity<Ride>(ride =>
        {
            ride.ToTable("Ride");
            ride.OwnsOne(r => r.Info, info =>
            {
                info.Property(i => i.StartAt).HasColumnName("StartAt");
                info.Property(i => i.RideTime).HasColumnName("RideTime");
                info.Property(i => i.MeetPlace).HasColumnName("MeetPlace");
                info.Property(i => i.Seats).HasColumnName("Seats");
                info.Property(i => i.UserId).HasColumnName("UserId");
                info.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(i => i.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
           
        });

        modelBuilder.Entity<RideBooking>(rideBooking =>
        {
            rideBooking.ToTable("RideBooking");
            rideBooking.HasKey(booking => booking.Id);

            rideBooking.HasIndex(booking => new { booking.RideId, booking.UserId })
                .IsUnique()
                .HasDatabaseName("RideBooking_RideId_UserId_uindex");

            rideBooking.HasOne<Ride>()
                .WithMany()
                .HasForeignKey(booking => booking.RideId)
                .OnDelete(DeleteBehavior.Cascade);

            rideBooking.HasOne<User>()
                .WithMany()
                .HasForeignKey(booking => booking.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}