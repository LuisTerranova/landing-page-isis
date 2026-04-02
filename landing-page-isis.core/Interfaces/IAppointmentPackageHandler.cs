using landing_page_isis.core.Models;

namespace landing_page_isis.core.Interfaces;

public interface IAppointmentPackageHandler
{
    Task<PaginatedResponse<AppointmentPackage?>> GetPackagesByPacientId(int page, int pageSize, Guid pacientId, CancellationToken ct);
    Task<HandlerResult> CreatePackage(AppointmentPackage package);
    Task<HandlerResult> UpdatePackage(AppointmentPackage package);
    Task<HandlerResult> DeletePackage(Guid id);
    Task<List<AppointmentPackage?>> GetAllPackagesByDateRange(DateTimeOffset start, DateTimeOffset end, CancellationToken ct);
}
