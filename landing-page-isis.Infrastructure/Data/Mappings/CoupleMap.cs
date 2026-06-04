using landing_page_isis.core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace landing_page_isis.Infrastructure.Data.Mappings;

public class CoupleMap : IEntityTypeConfiguration<Couple>
{
    public void Configure(EntityTypeBuilder<Couple> builder)
    {
        builder.ToTable("couples");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(150).HasColumnName("name");

        builder.Property(c => c.Patient1Id).IsRequired().HasColumnName("patient1_id");
        builder.Property(c => c.Patient2Id).IsRequired().HasColumnName("patient2_id");

        builder
            .Property(c => c.PayerName)
            .IsRequired(false)
            .HasMaxLength(150)
            .HasColumnName("payer_name");

        builder
            .Property(c => c.PayerCpf)
            .IsRequired(false)
            .HasMaxLength(255)
            .HasColumnName("payer_cpf")
            .HasConversion(
                v => AesEncryptionService.Encrypt(v ?? ""),
                v => AesEncryptionService.Decrypt(v)
            );

        builder
            .Property(c => c.PolicySigned)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("policy_signed");

        builder
            .HasOne(c => c.Patient1)
            .WithMany()
            .HasForeignKey(c => c.Patient1Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(c => c.Patient2)
            .WithMany()
            .HasForeignKey(c => c.Patient2Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Appointments).WithOne(a => a.Couple).HasForeignKey(a => a.CoupleId);
        builder.HasMany(c => c.Packages).WithOne(p => p.Couple).HasForeignKey(p => p.CoupleId);
    }
}
