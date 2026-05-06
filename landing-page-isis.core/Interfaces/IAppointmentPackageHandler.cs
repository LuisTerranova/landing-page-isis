using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;

namespace landing_page_isis.core.Interfaces;

public interface IAppointmentPackageHandler
{
    Task<PaginatedResponse<AppointmentPackageListItemDto>> GetPackagesByPatientId(
        int page,
        int pageSize,
        Guid patientId,
        CancellationToken ct
    );
    Task<HandlerResult> CreatePackage(AppointmentPackage package);
    Task<HandlerResult> UpdatePackage(AppointmentPackage package);
    Task<HandlerResult> DeletePackage(Guid id);
    Task<List<AppointmentPackageListItemDto>> GetAllPackagesByDateRange(
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct
    );
}
