using landing_page_isis.core.Models;
using landing_page_isis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace landing_page_isis.Data.Mappings;

public class AppointmentRecordMap : IEntityTypeConfiguration<AppointmentRecord>
{
    public void Configure(EntityTypeBuilder<AppointmentRecord> builder)
    {
        builder.ToTable("appointment_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AppointmentId).IsRequired().HasColumnName("appointment_id");

        builder
            .Property(x => x.Note)
            .IsRequired(false)
            .HasColumnName("note")
            .HasConversion(
                v => AesEncryptionService.Encrypt(v ?? ""),
                v => AesEncryptionService.Decrypt(v)
            );

        builder.Property(x => x.CreatedAt).IsRequired().HasColumnName("created_at");

        builder.Property(x => x.UpdatedAt).IsRequired(false).HasColumnName("updated_at");

        builder
            .HasOne(x => x.Appointment)
            .WithOne(a => a.Record)
            .HasForeignKey<AppointmentRecord>(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
