using landing_page_isis.core;

namespace landing_page_isis;

public class PacientHandler : IPacientHandler
{
    public Task<List<Pacient>> GetPacients()
    {
        throw new NotImplementedException();
    }

    public Task<Pacient> GetPacient(int id)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CreatePacient(Pacient lead)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdatePacient(Pacient lead)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeletePacient(int id)
    {
        throw new NotImplementedException();
    }
}