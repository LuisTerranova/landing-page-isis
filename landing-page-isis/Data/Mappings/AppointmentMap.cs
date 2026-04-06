using landing_page_isis.core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace landing_page_isis.Data.Mappings;

public class AppointmentMap : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AppointmentDate).IsRequired().HasColumnName("appointment_date");

        builder
            .Property(a => a.AppointmentStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnName("appointment_status");

        builder.Property(a => a.Price).IsRequired().HasColumnName("appointment_price");

        builder.Property(a => a.ReminderSent).IsRequired().HasColumnName("reminder_sent");

        builder.Property(a => a.PackageId).IsRequired(false).HasColumnName("package_id");

        builder
            .HasOne(a => a.Package)
            .WithMany()
            .HasForeignKey(a => a.PackageId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
