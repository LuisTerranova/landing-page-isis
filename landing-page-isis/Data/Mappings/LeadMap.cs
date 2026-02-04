using landing_page_isis.core;
using landing_page_isis.core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace landing_page_isis.Data.Mappings;

public class LeadMap : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("leads");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnName("lead_name");

        builder.Property(l => l.Phone)
            .IsRequired()
            .HasMaxLength(11)
            .HasColumnName("lead_phone");
        
        builder.Property(l => l.Email)
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnName("lead_email");
        
        builder.Property(l => l.Intent)
            .IsRequired()
            .HasMaxLength(300)
            .HasColumnName("lead_intent");
        
        builder.Property(l => l.Created)
            .HasColumnName("lead_created");
        
        builder.Property(l => l.LeadStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("lead_status");;
    }
}