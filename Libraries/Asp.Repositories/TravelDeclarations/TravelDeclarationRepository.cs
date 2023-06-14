using src.Core.Domains;
using src.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using src.Core.Data;
using src.Core.Enums;



namespace src.Repositories.TravelDeclarations
{
    public class TravelDeclarationRepository : ITravelDeclarationRepository
    {
        private readonly IDbContext _dbContext;

        public TravelDeclarationRepository(IDbContext dbcontext)
        {
            _dbContext = dbcontext;
        }

        public async Task<IList<TravelDeclaration>> getAllRequest()
        {
            return await _dbContext.travel_declaration.Include(t => t.Requester).Include(t => t.travellingRoutes).AsQueryable().ToListAsync();
        }

        public async Task<IList<TravelDeclaration>> getAllRequestByDataFilter(SearchDataRequest filter)
        {
            var entities = await _dbContext.travel_declaration.Include(t => t.Requester).Include(t => t.travellingRoutes).ToListAsync();

            if (entities != null)
            {
                if (filter.from != null)
                {
                    entities = entities.Where(t => t.departureDate >= filter.from.Value).ToList();
                }

                if (filter.to != null)
                {
                    entities = entities.Where(t => t.departureDate <= filter.to.Value).ToList();
                }

                if (filter.travelLocation != null)
                {
                    entities = entities.Where(t => t.travelTo == filter.travelLocation
                    || t.travellingRoutes.Any(x => x.TravelRouteProvinceId == filter.cityOfTravelRoutes)
                    || t.travellingRoutes.Any(x => x.TravelRouteDistrictId == filter.districtOfTravelRoutes)
                    || t.travellingRoutes.Any(x => x.TravelRouteWardId == filter.wardOfTravelRoutes))
                    .ToList();
                }
                return entities;
            }
            return null;
        }

        public Task<List<TravelDeclarationDashboardModel>> getDashboardTravelDeclarationMoet()
        {
            return _dbContext.Dashboards.FromSql("TdDashboardMoet").AsQueryable().ToListAsync();
        }

        public Task<List<TravelDeclarationDashboardModel>> getDashboardTravelDeclarationCam()
        {
            return _dbContext.Dashboards.FromSql("TdDashboardCam").AsQueryable().ToListAsync();
        }
        public Task<List<TravelDeclarationDashboardModel>> getDashboardCovidIncidentMoet()
        {
            return _dbContext.Dashboards.FromSql("CirDashboardMoet").AsQueryable().ToListAsync();
        }
        public Task<List<TravelDeclarationDashboardModel>> getDashboardCovidIncidentCam()
        {
            return _dbContext.Dashboards.FromSql("CirDashboardCam").AsQueryable().ToListAsync();
        }



        public async Task<IList<TravelDeclaration>> getRequestIdById(string id)
        {
            return await _dbContext.travel_declaration.Where(t => t.request_id.Contains(id)).AsQueryable().ToListAsync();
        }


        public TravelDeclaration getTravelDeclarationById(Guid? id)
        {
            var query = _dbContext.travel_declaration.Include(t => t.Requester).Include(t => t.travellingRoutes)
              .Where(x => x.TravelDeclarationId == id);
            var result = query.FirstOrDefault();
            return result;
        }

        public async Task<TravelDeclaration> insert(TravelDeclaration model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            _dbContext.travel_declaration.Add(model);
            await _dbContext.SaveChangesAsync();
            return model;
        }

        public async Task<IList<TravelDeclaration>> listAllRequestByCampus(string campus)
        {
            var entity = _dbContext.travel_declaration.ToListAsync();
            if (campus == "0")
            {
                return await _dbContext.travel_declaration.Include(t => t.Requester).ToListAsync();
            }
            else
            {
                return await _dbContext.travel_declaration.Include(t => t.Requester).Where(t => t.Requester.Campus.Contains(campus)).ToListAsync();
            }
        }

        public async Task<IList<TravelDeclaration>> listAllRequestByUser(int userId, string approver)
        {
            if (userId == 0)
                throw new ArgumentNullException(nameof(userId));
            var entities = await _dbContext.travel_declaration
                .Include(t => t.Requester)
                .Include(x => x.travellingRoutes)
                .Where(t => t.RequesterId == userId || t.nameOfLineManager.Contains(approver) || t.ECSDEmail.Contains(approver) || t.informedEmail.Contains(approver))
                .OrderBy(t => t.request_id).ToListAsync();
            if (entities != null)
            {
                if (entities.Any(e => e.ECSDEmail != null && e.ECSDEmail.Split("@")[0] == approver))
                {

                    if (entities.Any(l => !string.IsNullOrEmpty(l.nameOfLineManager)))
                    {
                        entities.RemoveAll(e =>
                            !string.IsNullOrEmpty(e.nameOfLineManager) &&
                            e.status == (int)RequestStatus.Submitted && e.CancelStatus == null && e.RequesterId != userId && e.nameOfLineManager.Split("@")[0] != approver
                            && e.ECSDEmail.Split("@")[0] == approver);
                    }
                    return entities;
                }
            }
            return entities;


        }

        public async Task updateTravelDeclaration(TravelDeclaration model)
        {
            _dbContext.travel_declaration.Update(model);
            await _dbContext.SaveChangesAsync();
        }


    }
}
