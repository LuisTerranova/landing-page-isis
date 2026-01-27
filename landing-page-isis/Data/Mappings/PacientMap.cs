using landing_page_isis.core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace landing_page_isis.Data.Mappings;

public class PacientMap : IEntityTypeConfiguration<Pacient>
{
    public void Configure(EntityTypeBuilder<Pacient> builder)
    {
        throw new NotImplementedException();
    }
}