using landing_page_isis.core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace landing_page_isis.Infrastructure.Data.Mappings;

public class ContractMap : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("contracts");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.PatientName)
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnName("patient_name");

        builder.Property(c => c.PatientCpf)
            .IsRequired(false)
            .HasMaxLength(255)
            .HasColumnName("patient_cpf")
            .HasConversion(
                v => AesEncryptionService.Encrypt(v ?? ""),
                v => AesEncryptionService.Decrypt(v)
            );

        builder.Property(c => c.PatientEmail)
            .IsRequired(false)
            .HasMaxLength(255)
            .HasColumnName("patient_email")
            .HasConversion(
                v => AesEncryptionService.Encrypt(v ?? ""),
                v => AesEncryptionService.Decrypt(v)
            );

        builder.Property(c => c.PatientPhone)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("patient_phone")
            .HasConversion(
                v => AesEncryptionService.Encrypt(v ?? ""),
                v => AesEncryptionService.Decrypt(v)
            );

        builder.Property(c => c.PatientState)
            .IsRequired(false)
            .HasMaxLength(2)
            .HasColumnName("patient_state");

        builder.Property(c => c.PatientBirthDate)
            .IsRequired(false)
            .HasColumnName("patient_birth_date");

        builder.Property(c => c.TermsAccepted)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("terms_accepted");

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(c => c.UpdatedAt)
            .IsRequired(false)
            .HasColumnName("updated_at");

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnName("status");

        builder.Property(c => c.Price)
            .IsRequired(false)
            .HasColumnName("price");

        builder.Property(c => c.InitialAppointments)
            .IsRequired(false)
            .HasColumnName("initial_appointments");

        builder.Property(c => c.Type)
            .IsRequired(false)
            .HasConversion<string>()
            .HasColumnName("type");

        builder.Property(c => c.PackagePrice)
            .IsRequired(false)
            .HasColumnName("package_price");

        builder.Property(c => c.AcceptanceToken)
            .IsRequired(false)
            .HasMaxLength(100)
            .HasColumnName("acceptance_token");

        builder.HasIndex(c => c.AcceptanceToken)
            .IsUnique()
            .HasFilter("\"acceptance_token\" IS NOT NULL");

        builder.Property(c => c.TokenGeneratedAt)
            .IsRequired(false)
            .HasColumnName("token_generated_at");

        builder.Property(c => c.AcceptedAt)
            .IsRequired(false)
            .HasColumnName("accepted_at");

        builder.Property(c => c.ContractDocumentHtml)
            .IsRequired(false)
            .HasColumnType("text")
            .HasColumnName("contract_document_html");

        builder.Property(c => c.PackageId)
            .IsRequired(false)
            .HasColumnName("package_id");

        builder.HasOne(c => c.Package)
            .WithMany()
            .HasForeignKey(c => c.PackageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(c => c.PatientId)
            .IsRequired(false)
            .HasColumnName("patient_id");

        builder.Property(c => c.PatientCpfHash)
            .IsRequired(false)
            .HasMaxLength(64)
            .HasColumnName("patient_cpf_hash");

        builder.HasIndex(c => c.PatientCpfHash)
            .IsUnique()
            .HasFilter("\"patient_cpf_hash\" IS NOT NULL");

        builder.HasOne(c => c.Patient)
            .WithOne(p => p.Contract)
            .HasForeignKey<Contract>(c => c.PatientId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.PatientId)
            .IsUnique()
            .HasFilter("\"patient_id\" IS NOT NULL");
    }
}
