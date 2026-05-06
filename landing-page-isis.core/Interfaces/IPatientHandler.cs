using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;

namespace landing_page_isis.core.Interfaces;

public interface IPatientHandler
{
    Task<PaginatedResponse<PatientListItemDto>> GetPatients(
        int page,
        int pageSize,
        CancellationToken ct
    );
    Task<Patient?> GetPatient(Guid id);
    Task<HandlerResult> CreatePatient(Patient lead);
    Task<HandlerResult> UpdatePatient(Patient lead);
    Task<HandlerResult> DeletePatient(Guid id);
    Task<PaginatedResponse<PatientListItemDto>> QueryPatients(
        string query,
        int page,
        int pageSize,
        CancellationToken ct
    );
}
