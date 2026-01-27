using landing_page_isis.core;
using landing_page_isis.core.Interfaces;

namespace landing_page_isis;

public class LeadHandler : ILeadHandler
{
    public Task<List<Lead>> GetLeads()
    {
        throw new NotImplementedException();
    }

    public Task<Lead> GetLead(int id)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CreateLead(Lead lead)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateLead(Lead lead)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteLead(int id)
    {
        throw new NotImplementedException();
    }
}
