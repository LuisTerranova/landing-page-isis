using landing_page_isis.core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace landing_page_isis.Data.Mappings;

public class AppointmentMap : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");
        builder.HasKey(a => a.Id);
        
        builder.Property(a => a.AppointmentDate)
            .IsRequired()
            .HasColumnName("appointment_date");
        
        builder.Property(a => a.AppointmentStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnName("appointment_status");
    } 
}