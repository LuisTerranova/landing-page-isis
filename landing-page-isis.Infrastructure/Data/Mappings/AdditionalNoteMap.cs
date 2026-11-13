using landing_page_isis.core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace landing_page_isis.Infrastructure.Data.Mappings;

/// <summary>
/// Configures the standalone clinical notes table, including PII encryption, ownership constraints, and indexes.
/// </summary>
public class AdditionalNoteMap : IEntityTypeConfiguration<AdditionalNote>
{
    public void Configure(EntityTypeBuilder<AdditionalNote> builder)
    {
        builder.ToTable(
            "additional_notes",
            table =>
                table.HasCheckConstraint(
                    "ck_additional_notes_exactly_one_owner",
                    "(patient_id IS NOT NULL) <> (couple_id IS NOT NULL)"
                )
        );

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PatientId).IsRequired(false).HasColumnName("patient_id");
        builder.Property(x => x.CoupleId).IsRequired(false).HasColumnName("couple_id");

        // Clinical note contents are encrypted at rest to comply with LGPD requirements
        builder
            .Property(x => x.Content)
            .IsRequired()
            .HasColumnName("content")
            .HasConversion(
                v => AesEncryptionService.Encrypt(v ?? ""),
                v => AesEncryptionService.Decrypt(v)
            );

        builder.Property(x => x.NoteDate).IsRequired().HasColumnName("note_date");
        builder.Property(x => x.CreatedAt).IsRequired().HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).IsRequired(false).HasColumnName("updated_at");

        builder
            .Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("is_deleted");

        builder.Property(x => x.DeletedAt).IsRequired(false).HasColumnName("deleted_at");

        builder
            .HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Couple)
            .WithMany()
            .HasForeignKey(x => x.CoupleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.PatientId, x.NoteDate });
        builder.HasIndex(x => new { x.CoupleId, x.NoteDate });
    }
}
