using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;

namespace landing_page_isis.core.Interfaces;

public interface IPatientHandler
{
    Task<PaginatedResponse<PatientListItemDto>> GetPatients(
        int page,
        int pageSize,
        CancellationToken ct,
        PatientStatusFilter filter = PatientStatusFilter.Active
    );
    Task<Patient?> GetPatient(Guid id);
    Task<HandlerResult> CreatePatient(Patient lead);
    Task<HandlerResult> UpdatePatient(Patient lead);
    Task<HandlerResult> DeletePatient(Guid id);
    Task<PaginatedResponse<PatientListItemDto>> QueryPatients(
        string query,
        int page,
        int pageSize,
        CancellationToken ct,
        bool excludeWithActiveContract = false,
        bool includeDeactivated = false
    );
    Task<Dictionary<Guid, string?>> GetPatientEmailMap(IEnumerable<Guid> ids, CancellationToken ct);

    /// <summary>
    /// Activates or deactivates a patient. Deactivation is blocked when the patient still has
    /// a couple link, a future scheduled appointment, or an active package with remaining sessions.
    /// </summary>
    Task<HandlerResult> SetPatientActive(Guid id, bool active, CancellationToken ct);

    /// <summary>
    /// Returns active patients with a birthday between today and today + <paramref name="daysAhead"/>,
    /// ordered by the number of days remaining and then by name.
    /// </summary>
    Task<IReadOnlyList<BirthdayListItemDto>> GetUpcomingBirthdays(
        int daysAhead,
        CancellationToken ct
    );
}
