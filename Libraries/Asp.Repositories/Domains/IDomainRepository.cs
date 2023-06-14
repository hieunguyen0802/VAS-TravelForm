using System.Collections.Generic;
using System.Threading.Tasks;
using src.Core.Domains;

namespace src.Repositories.Domains
{
    public interface IDomainRepository
    {
        Task<IList<Domain>> GetAllDomainsAsync();

        Task<Domain> GetDomainByIdAsync(int userId);

        Task<int> AddDomainAsync(Domain user);

        Task UpdateDomainAsync(Domain user);
    }
}