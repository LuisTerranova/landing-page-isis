namespace landing_page_isis.core;

public interface IPacientHandler
{
    Task<List<Pacient>> GetPacients();
    Task<Pacient> GetPacient(int id);
    Task<bool> CreatePacient(Pacient lead);
    Task<bool> UpdatePacient(Pacient lead);
    Task<bool> DeletePacient(int id);
}
