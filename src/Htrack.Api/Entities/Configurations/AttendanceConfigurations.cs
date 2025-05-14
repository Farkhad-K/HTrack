using HTrack.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HTrack.Api.Entities.Configurations;

public class AttendanceConfigurations : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CheckIn)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CheckOut)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Duration)
            .HasColumnType("interval"); // PostgreSQL-specific type for TimeSpan

        builder.HasOne(x => x.Employee)
            .WithMany(e => e.Attendances)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}