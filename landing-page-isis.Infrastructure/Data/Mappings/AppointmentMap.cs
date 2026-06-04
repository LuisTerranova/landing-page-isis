using landing_page_isis.core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace landing_page_isis.Infrastructure.Data.Mappings;

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

        builder.Property(a => a.PatientId).IsRequired(false).HasColumnName("patient_id");

        builder.Property(a => a.CoupleId).IsRequired(false).HasColumnName("couple_id");

        builder.Property(a => a.PackageId).IsRequired(false).HasColumnName("package_id");

        builder
            .Property(a => a.PayerName)
            .IsRequired(false)
            .HasMaxLength(150)
            .HasColumnName("payer_name");

        builder
            .Property(a => a.PayerCpf)
            .IsRequired(false)
            .HasMaxLength(255)
            .HasColumnName("payer_cpf")
            .HasConversion(
                v => AesEncryptionService.Encrypt(v ?? ""),
                v => AesEncryptionService.Decrypt(v)
            );

        builder
            .HasOne(a => a.Package)
            .WithMany()
            .HasForeignKey(a => a.PackageId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
