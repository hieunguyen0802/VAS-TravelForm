using System.Collections.Generic;
using System.Threading.Tasks;
using src.Core.Data;
using src.Core.Domains;

namespace src.Repositories.Logging
{
    public interface ILogRepository
    {
        Task<IList<Log>> GetLogs();

        Task<IList<string>> GetLevels();
    }
}