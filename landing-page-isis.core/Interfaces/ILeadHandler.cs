using landing_page_isis.core.Models;

namespace landing_page_isis.core.Interfaces;

public interface ILeadHandler
{
    Task<PaginatedResponse<Lead?>> GetLeads(int page, int pageSize);
    Task<Lead?> GetLead(Guid id);
    Task<HandlerResult> CreateLead(Lead lead);
    Task<HandlerResult> ApproveLead(Guid id);
    Task<HandlerResult> DeleteLead(Guid id);
}