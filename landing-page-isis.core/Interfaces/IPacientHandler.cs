using landing_page_isis.core.Models;

namespace landing_page_isis.core.Interfaces;

public interface IPacientHandler
{
    Task<List<Pacient>> GetPacients();
    Task<Pacient> GetPacient(int id);
    Task<bool> CreatePacient(Pacient lead);
    Task<bool> UpdatePacient(Pacient lead);
    Task<bool> DeletePacient(int id);
}
