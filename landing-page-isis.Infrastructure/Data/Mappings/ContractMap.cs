using landing_page_isis.core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace landing_page_isis.Infrastructure.Data.Mappings;

/// <summary>
/// Configures Entity Framework mapping configurations for the Contract model, including table schema, relationship constraints, PII encryption, and indexing.
/// </summary>
public class ContractMap : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("contracts");
        builder.HasKey(c => c.Id);

        builder.OwnsOne(
            c => c.PrimaryPatient,
            p =>
            {
                p.Property(pt => pt.Name)
                    .IsRequired()
                    .HasMaxLength(150)
                    .HasColumnName("patient_name");

                p.Property(pt => pt.Cpf)
                    .IsRequired(false)
                    .HasMaxLength(255)
                    .HasColumnName("patient_cpf")
                    .HasConversion(
                        v => AesEncryptionService.Encrypt(v ?? ""),
                        v => AesEncryptionService.Decrypt(v)
                    );

                p.Property(pt => pt.Email)
                    .IsRequired(false)
                    .HasMaxLength(255)
                    .HasColumnName("patient_email")
                    .HasConversion(
                        v => AesEncryptionService.Encrypt(v ?? ""),
                        v => AesEncryptionService.Decrypt(v)
                    );

                p.Property(pt => pt.Phone)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("patient_phone")
                    .HasConversion(
                        v => AesEncryptionService.Encrypt(v ?? ""),
                        v => AesEncryptionService.Decrypt(v)
                    );

                p.Property(pt => pt.State)
                    .IsRequired(false)
                    .HasMaxLength(2)
                    .HasColumnName("patient_state");

                p.Property(pt => pt.BirthDate)
                    .IsRequired(false)
                    .HasColumnName("patient_birth_date");

                p.Property(pt => pt.CpfHash)
                    .IsRequired(false)
                    .HasMaxLength(64)
                    .HasColumnName("patient_cpf_hash");

                p.HasIndex(pt => pt.CpfHash)
                    .IsUnique()
                    .HasFilter("\"patient_cpf_hash\" IS NOT NULL");
            }
        );

        builder
            .Property(c => c.TermsAccepted)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("terms_accepted");

        builder.Property(c => c.CreatedAt).IsRequired().HasColumnName("created_at");

        builder.Property(c => c.UpdatedAt).IsRequired(false).HasColumnName("updated_at");

        builder
            .Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnName("status");

        builder.Property(c => c.Price).IsRequired(false).HasColumnName("price");

        builder
            .Property(c => c.InitialAppointments)
            .IsRequired(false)
            .HasColumnName("initial_appointments");

        builder.Property(c => c.PackagePrice).IsRequired(false).HasColumnName("package_price");

        builder
            .Property(c => c.AcceptanceToken)
            .IsRequired(false)
            .HasMaxLength(100)
            .HasColumnName("acceptance_token");

        // Unique index on acceptance tokens ensures uniqueness while ignoring nulls (allowing drafts without tokens generated yet)
        builder
            .HasIndex(c => c.AcceptanceToken)
            .IsUnique()
            .HasFilter("\"acceptance_token\" IS NOT NULL");

        builder
            .Property(c => c.TokenGeneratedAt)
            .IsRequired(false)
            .HasColumnName("token_generated_at");

        builder.Property(c => c.AcceptedAt).IsRequired(false).HasColumnName("accepted_at");

        builder
            .Property(c => c.ContractDocumentHtml)
            .IsRequired(false)
            .HasColumnType("text")
            .HasColumnName("contract_document_html");

        builder.Property(c => c.PackageId).IsRequired(false).HasColumnName("package_id");

        builder
            .HasOne(c => c.Package)
            .WithMany()
            .HasForeignKey(c => c.PackageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(c => c.PatientId).IsRequired(false).HasColumnName("patient_id");

        builder
            .HasOne(c => c.Patient)
            .WithOne(p => p.Contract)
            .HasForeignKey<Contract>(c => c.PatientId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.PatientId).IsUnique().HasFilter("\"patient_id\" IS NOT NULL");

        builder.Property(c => c.CoupleId).IsRequired(false).HasColumnName("couple_id");

        builder
            .HasOne(c => c.Couple)
            .WithOne(c => c.Contract)
            .HasForeignKey<Contract>(c => c.CoupleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.CoupleId).IsUnique().HasFilter("\"couple_id\" IS NOT NULL");

        builder
            .Property(c => c.CoupleName)
            .IsRequired(false)
            .HasMaxLength(150)
            .HasColumnName("couple_name");

        builder.OwnsOne(
            c => c.SecondaryPatient,
            p =>
            {
                p.Property(pt => pt.Name)
                    .IsRequired(false)
                    .HasMaxLength(150)
                    .HasColumnName("patient2_name");

                p.Property(pt => pt.Cpf)
                    .IsRequired(false)
                    .HasMaxLength(255)
                    .HasColumnName("patient2_cpf")
                    .HasConversion(
                        v => AesEncryptionService.Encrypt(v ?? ""),
                        v => AesEncryptionService.Decrypt(v)
                    );

                p.Property(pt => pt.Email)
                    .IsRequired(false)
                    .HasMaxLength(255)
                    .HasColumnName("patient2_email")
                    .HasConversion(
                        v => AesEncryptionService.Encrypt(v ?? ""),
                        v => AesEncryptionService.Decrypt(v)
                    );

                p.Property(pt => pt.Phone)
                    .IsRequired(false)
                    .HasMaxLength(255)
                    .HasColumnName("patient2_phone")
                    .HasConversion(
                        v => AesEncryptionService.Encrypt(v ?? ""),
                        v => AesEncryptionService.Decrypt(v)
                    );

                p.Property(pt => pt.State)
                    .IsRequired(false)
                    .HasMaxLength(2)
                    .HasColumnName("patient2_state");

                p.Property(pt => pt.BirthDate)
                    .IsRequired(false)
                    .HasColumnName("patient2_birth_date");

                p.Property(pt => pt.CpfHash)
                    .IsRequired(false)
                    .HasMaxLength(64)
                    .HasColumnName("patient2_cpf_hash");

                p.HasIndex(pt => pt.CpfHash)
                    .IsUnique()
                    .HasFilter("\"patient2_cpf_hash\" IS NOT NULL");
            }
        );
    }
}
