using landing_page_isis.Infrastructure.Data;
using landing_page_isis.core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Extensions;

public static class DbMigrationExtensions
{
    public static async Task MigrateCpfHashes(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var patientsWithoutHash = await context.Patients
            .Where(p => p.CpfHash == null && p.Cpf != null)
            .ToListAsync();

        if (patientsWithoutHash.Any())
        {
            foreach (var patient in patientsWithoutHash)
            {
                if (!string.IsNullOrEmpty(patient.Cpf))
                {
                    patient.CpfHash = CpfHelper.ComputeHash(CpfValidator.Strip(patient.Cpf));
                }
            }
            await context.SaveChangesAsync();
        }
    }
}
