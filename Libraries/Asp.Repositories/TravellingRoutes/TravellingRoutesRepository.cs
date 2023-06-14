using src.Core.Domains;
using src.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace src.Repositories.TravellingRoutes
{
    public class TravellingRoutesRepository : ITravellingRoutesRepository
    {
        private readonly IDbContext _dbContext;

        public TravellingRoutesRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task insert(TravellingRoute model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            _dbContext.travelling_routes.Add(model);
            await _dbContext.SaveChangesAsync();
        }
    }
}
