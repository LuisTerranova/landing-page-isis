using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;

namespace landing_page_isis.core.Interfaces;

public interface ILeadHandler
{
    Task<PaginatedResponse<LeadListItemDto>> GetLeads(int page, int pageSize, CancellationToken ct);
    Task<Lead?> GetLead(Guid id);
    Task<HandlerResult> CreateLead(Lead lead);
    Task<HandlerResult> ApproveLead(Guid id);
    Task<HandlerResult> DeleteLead(Guid id);
    Task<HandlerResult> CleanLeads(CancellationToken ct);
    string GetWhatsAppUrl(Lead lead);
}
