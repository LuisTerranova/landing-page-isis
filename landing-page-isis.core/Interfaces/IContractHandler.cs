using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;

namespace landing_page_isis.core.Interfaces;

public interface IContractHandler
{
    Task<PaginatedResponse<ContractListItemDto>> GetContracts(
        int page,
        int pageSize,
        CancellationToken ct
    );
    Task<Contract?> GetContract(Guid id);
    Task<Contract?> GetContractByToken(string token);
    Task<HandlerResult> CreateContract(Contract contract);
    Task<HandlerResult> UpdateContract(Contract contract);
    Task<HandlerResult> AcceptContract(string token);
    Task<HandlerResult> DeleteContract(Guid id);
}
