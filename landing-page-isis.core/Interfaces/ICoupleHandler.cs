using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;

namespace landing_page_isis.core.Interfaces;

public interface ICoupleHandler
{
    Task<PaginatedResponse<CoupleListItemDto>> GetCouples(
        int page,
        int pageSize,
        CancellationToken ct
    );
    Task<Couple?> GetCouple(Guid id);
    Task<HandlerResult> CreateCouple(Couple couple);
    Task<HandlerResult> UpdateCouple(Couple couple);
    Task<HandlerResult> DeleteCouple(Guid id);
    Task<PaginatedResponse<CoupleListItemDto>> QueryCouples(
        string query,
        int page,
        int pageSize,
        CancellationToken ct
    );
    Task<List<CoupleListItemDto>> GetAllCouples(CancellationToken ct);
    Task<Couple?> GetCoupleByPatientId(Guid patientId);
}
