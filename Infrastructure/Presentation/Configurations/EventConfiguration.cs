using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Presentation.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("CK_TotalSeats_GreaterThanZero", "total_seats > 0"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Title)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasMany(b => b.Bookings)
            .WithOne(a => a.Event)
            .HasForeignKey(b => b.EventId);
    }
}