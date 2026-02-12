using landing_page_isis.core.Models;

namespace landing_page_isis.core.Interfaces;

public interface IPacientHandler
{
    Task<PaginatedResponse<Pacient>> GetPacients(int page, int pageSize, CancellationToken ct);
    Task<Pacient?> GetPacient(Guid id);
    Task<HandlerResult> CreatePacient(Pacient lead);
    Task<HandlerResult> UpdatePacient(Pacient lead);
    Task<HandlerResult> DeletePacient(Guid id);
}
