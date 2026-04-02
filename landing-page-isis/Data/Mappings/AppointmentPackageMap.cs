using landing_page_isis.core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace landing_page_isis.Data.Mappings;

public class AppointmentPackageMap : IEntityTypeConfiguration<AppointmentPackage>
{
    public void Configure(EntityTypeBuilder<AppointmentPackage> builder)
    {
        builder.ToTable("appointment_packages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PacientId).IsRequired().HasColumnName("pacient_id");

        builder.Property(x => x.TotalAppointments).IsRequired().HasColumnName("total_appointments");

        builder
            .Property(x => x.RemainingAppointments)
            .IsRequired()
            .HasColumnName("remaining_appointments");

        builder
            .Property(x => x.PaymentMethod)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnName("payment_method");

        builder.Property(x => x.Price).IsRequired().HasColumnName("price");

        builder
            .Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnName("status");

        builder.Property(x => x.CreatedAt).IsRequired().HasColumnName("created_at");

        builder.Property(x => x.UpdatedAt).IsRequired(false).HasColumnName("updated_at");

        builder
            .HasOne(x => x.Pacient)
            .WithMany()
            .HasForeignKey(x => x.PacientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
