using landing_page_isis.core.Models;

namespace landing_page_isis.core.Interfaces;

public interface ILeadHandler
{
    Task<List<Lead>> GetLeads();
    Task<Lead> GetLead(int id);
    Task<bool> CreateLead(Lead lead);
    Task<bool> UpdateLead(Lead lead);
    Task<bool> DeleteLead(int id);
}