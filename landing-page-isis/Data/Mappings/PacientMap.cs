using landing_page_isis.core;
using landing_page_isis.core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace landing_page_isis.Data.Mappings;

public class PacientMap : IEntityTypeConfiguration<Pacient>
{
    public void Configure(EntityTypeBuilder<Pacient> builder)
    {
        builder.ToTable("pacients");
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnName("name");

        builder.Property(p => p.Cpf)
            .IsRequired()
            .HasMaxLength(11)
            .HasColumnName("cpf");
        
        builder.Property(p => p.BirthDate)
            .IsRequired()
            .HasColumnName("birth_date");
        
        builder.Property(p => p.Email)
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnName("email");
        
        builder.Property(p => p.Phone)
            .IsRequired()
            .HasMaxLength(11)
            .HasColumnName("phone");
        
        builder.Property(p => p.Address)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("address");

        builder.HasMany(p => p.Appointments)
            .WithOne(p => p.Pacient);
    }
}