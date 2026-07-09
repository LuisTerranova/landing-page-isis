using landing_page_isis.core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace landing_page_isis.Infrastructure.Data.Mappings;

/// <summary>
/// Configures Entity Framework mapping configurations for the Patient model, including schema fields, PII encryption, and indexing.
/// </summary>
public class PatientMap : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(150).HasColumnName("name");

        // Encrypt CPF in the database to secure sensitive patient data (complying with GDPR/LGPD requirements)
        builder
            .Property(p => p.Cpf)
            .IsRequired(false)
            .HasMaxLength(255)
            .HasColumnName("cpf")
            .HasConversion(
                v => AesEncryptionService.Encrypt(v ?? ""),
                v => AesEncryptionService.Decrypt(v)
            );

        builder.Property(p => p.BirthDate).IsRequired(false).HasColumnName("birth_date");

        // Encrypt Email in the database to secure sensitive patient data (complying with GDPR/LGPD requirements)
        builder
            .Property(p => p.Email)
            .IsRequired(false)
            .HasMaxLength(255)
            .HasColumnName("email")
            .HasConversion(
                v => AesEncryptionService.Encrypt(v ?? ""),
                v => AesEncryptionService.Decrypt(v)
            );

        // Encrypt Phone in the database to secure sensitive patient data (complying with GDPR/LGPD requirements)
        builder
            .Property(p => p.Phone)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("phone")
            .HasConversion(
                v => AesEncryptionService.Encrypt(v ?? ""),
                v => AesEncryptionService.Decrypt(v)
            );

        builder
            .Property(p => p.StateOfResidency)
            .IsRequired(false)
            .HasMaxLength(2)
            .HasColumnName("state_of_residency");

        builder
            .Property(p => p.PolicySigned)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("policy_signed");

        builder
            .Property(p => p.PayerName)
            .IsRequired(false)
            .HasMaxLength(150)
            .HasColumnName("payer_name");

        // Encrypt Payer CPF in the database to secure sensitive patient data (complying with GDPR/LGPD requirements)
        builder
            .Property(p => p.PayerCpf)
            .IsRequired(false)
            .HasMaxLength(255)
            .HasColumnName("payer_cpf")
            .HasConversion(
                v => AesEncryptionService.Encrypt(v ?? ""),
                v => AesEncryptionService.Decrypt(v)
            );

        builder.HasMany(p => p.Appointments).WithOne(p => p.Patient);

        builder.Property(p => p.CpfHash)
            .IsRequired(false)
            .HasMaxLength(64)
            .HasColumnName("cpf_hash");

        // Unique index on CPF hash ensures uniqueness only when populated, allowing multiple records with null hashes
        builder.HasIndex(p => p.CpfHash)
            .IsUnique()
            .HasFilter("\"cpf_hash\" IS NOT NULL");
    }
}
