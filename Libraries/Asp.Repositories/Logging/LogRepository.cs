using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using src.Core.Data;
using src.Core.Domains;
using src.Data;
using Microsoft.EntityFrameworkCore;

namespace src.Repositories.Logging
{
    public class LogRepository : ILogRepository
    {
        private readonly IDbContext _context;

        public LogRepository(IDbContext context)
        {
            _context = context;
        }

        public async Task<IList<Log>> GetLogs()
        {
            return await _context.Logs.AsQueryable().ToListAsync();
        }
        public async Task<IList<string>> GetLevels()
        {
            var query = _context.Logs.AsQueryable()
                .Select(x => x.Level)
                .Distinct();

            return await query.ToListAsync();
        }
    }
}