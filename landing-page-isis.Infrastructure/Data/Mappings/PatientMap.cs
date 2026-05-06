using landing_page_isis.core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace landing_page_isis.Infrastructure.Data.Mappings;

public class PatientMap : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(150).HasColumnName("name");

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

        builder
            .Property(p => p.Email)
            .IsRequired(false)
            .HasMaxLength(255)
            .HasColumnName("email")
            .HasConversion(
                v => AesEncryptionService.Encrypt(v ?? ""),
                v => AesEncryptionService.Decrypt(v)
            );

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

        builder.HasMany(p => p.Appointments).WithOne(p => p.Patient);
    }
}
