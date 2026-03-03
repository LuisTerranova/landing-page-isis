using landing_page_isis.core.Models;
using landing_page_isis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace landing_page_isis.Data.Mappings;

public class PacientMap : IEntityTypeConfiguration<Pacient>
{
    public void Configure(EntityTypeBuilder<Pacient> builder)
    {
        builder.ToTable("pacients");
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

        builder.Property(p => p.Email).IsRequired().HasMaxLength(150).HasColumnName("email");

        builder.Property(p => p.Phone).IsRequired().HasMaxLength(11).HasColumnName("phone");

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

        builder.HasMany(p => p.Appointments).WithOne(p => p.Pacient);
    }
}
