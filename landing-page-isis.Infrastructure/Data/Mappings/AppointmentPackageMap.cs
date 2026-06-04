using landing_page_isis.core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace landing_page_isis.Infrastructure.Data.Mappings;

public class AppointmentPackageMap : IEntityTypeConfiguration<AppointmentPackage>
{
    public void Configure(EntityTypeBuilder<AppointmentPackage> builder)
    {
        builder.ToTable("appointment_packages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PatientId).IsRequired(false).HasColumnName("patient_id");

        builder.Property(x => x.CoupleId).IsRequired(false).HasColumnName("couple_id");

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
            .Property(x => x.PayerName)
            .IsRequired(false)
            .HasMaxLength(150)
            .HasColumnName("payer_name");

        builder
            .Property(x => x.PayerCpf)
            .IsRequired(false)
            .HasMaxLength(255)
            .HasColumnName("payer_cpf")
            .HasConversion(
                v => AesEncryptionService.Encrypt(v ?? ""),
                v => AesEncryptionService.Decrypt(v)
            );

        builder
            .HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
