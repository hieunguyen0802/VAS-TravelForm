using System.Collections.Generic;
using System.Threading.Tasks;
using src.Data;
using src.Core.Domains;
using src.Core.Enums;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;
using src.Web.Common.Models.RedZone;

namespace src.Repositories.RedZone
{
    public class RedZoneRepo : IRedZoneRepo
    {
        private readonly IDbContext _dbContext;
        
        public RedZoneRepo (IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<redZone>> listAllRequest()
        {
            return await _dbContext.RedZone.AsQueryable().ToListAsync();
        }

        public redZone GetRedZonesById(Guid? id)
        {
            var query = _dbContext.RedZone.Where(x => x.redZoneId== id);
            var result = query.FirstOrDefault();
            return result;
        }

        public async Task<IList<TravelDeclaration>> FilterTravelRouteByRedZone(Guid id)
        {
            try
            {
                var entities = await _dbContext.travel_declaration.Include(t => t.Requester).Include(t => t.travellingRoutes).ToListAsync();
                var request = GetRedZonesById(id);

                if (entities != null)
                {
                    if (request.redZoneDate != null)
                    {
                        entities = entities.Where(t => t.travellingRoutes.Any(h => h.dateTravel >= request.redZoneDate)).ToList();
                    }

                    if (request.redZoneToDate != null)
                    {
                        entities = entities.Where(t => t.travellingRoutes.Any(h => h.toDateTravel <= request.redZoneToDate)).ToList();
                    }

                    if (request.isRedZoneOnTransportation == true)
                    {
                        entities = entities.Where(h => h.travellingRoutes.Any(x => x.transportation == request.redZoneTransportation)).ToList();
                    }

                    if (request.isDomestic == isDomestic.yes)
                    {
                        entities = entities.Where(t => t.travellingRoutes.Any(h => h.TravelRouteProvinceId == request.redZoneProvinceId)
                        || t.travellingRoutes.Any(h => h.TravelRouteDistrictId == request.redZoneDistrictId)
                        || t.travellingRoutes.Any(h => h.TravelRouteWardId == request.redZoneWardId))
                        .ToList();
                    }
                    else
                    {
                        entities = entities.Where(t => t.travellingRoutes.Any(h => h.travelRouteCountryId == request.redZoneCountryId)
                        || t.travellingRoutes.Any(h => h.travelRouteCity == request.redZoneCity))
                        .ToList();
                    }

                    return entities;
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw ex;
            }
        }


        public async Task<IList<TravelDeclaration>> FilterTravelRouteByRedZoneUsingModel(RedZoneModel model)
        {
            try
            {
                var entities = await _dbContext.travel_declaration.Include(t => t.Requester).Include(t => t.travellingRoutes).ToListAsync();

                if (entities != null)
                {
                    if (model.redZoneDate != null)
                    {
                        entities = entities.Where(t => t.travellingRoutes.Any(h => h.dateTravel >= model.redZoneDate)).ToList();
                    }

                    if (model.redZoneToDate != null)
                    {
                        entities = entities.Where(t => t.travellingRoutes.Any(h => h.toDateTravel <= model.redZoneToDate)).ToList();
                    }

                    if (model.isRedZoneOnTransportation == true)
                    {
                        entities = entities.Where(h => h.travellingRoutes.Any(x => x.transportation == model.redZoneTransportation)).ToList();
                    }

                    if (model.isDomestic == isDomestic.yes)
                    {
                        entities = entities.Where(t => t.travellingRoutes.Any(h => h.TravelRouteProvinceId == model.redZoneProvinceId)
                        || t.travellingRoutes.Any(h => h.TravelRouteDistrictId == model.redZoneDistrictId)
                        || t.travellingRoutes.Any(h => h.TravelRouteWardId == model.redZoneWardId))
                        .ToList();
                    }
                    else
                    {
                        entities = entities.Where(t => t.travellingRoutes.Any(h => h.travelRouteCountryId == model.redZoneCountryId)
                        || t.travellingRoutes.Any(h => h.travelRouteCity == model.redZoneCity))
                        .ToList();
                    }

                    return entities;
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw ex;
            }
        }


        public async Task<redZone> insert (redZone model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            _dbContext.RedZone.Add(model);
            await _dbContext.SaveChangesAsync();
            return model;
        }

        public async Task<List<redZone>> filterRedZone(RedZoneModel model)
        {
            var entities = await _dbContext.RedZone.AsQueryable().ToListAsync();
            if (entities != null)
            {
                if (model.redZoneName != null)
                {
                    entities = entities.Where(t => t.redZoneName.Contains(model.redZoneName)).ToList();
                }
                if (model.redZoneDate != null)
                {
                    entities = entities.Where(t => t.redZoneDate >= model.redZoneDate.Value).ToList();
                }

                if (model.redZoneToDate != null)
                {
                    entities = entities.Where(t => t.redZoneToDate <= model.redZoneToDate.Value).ToList();
                }
                if (model.isActivate == true)
                {
                    entities = entities.Where(h => h.isActivate == true).ToList();
                } 

                if (model.isRedZoneOnTransportation == true)
                {
                    entities = entities.Where(h => h.isRedZoneOnTransportation == true).ToList();
                    if (model.redZoneTransportation != null)
                    {
                        entities = entities.Where(h => h.redZoneTransportation == model.redZoneTransportation).ToList();
                    }
                } 
                else
                {
                    if (model.isDomestic.Value == isDomestic.yes)
                    {
                        entities = entities.Where(t => t.isDomestic == model.isDomestic.Value).ToList();
                        if (model.redZoneProvinceId != null)
                        {
                            entities = entities.Where(t => t.redZoneProvinceId == model.redZoneProvinceId
                            || t.redZoneDistrictId == model.redZoneDistrictId
                            || t.redZoneWardId == model.redZoneWardId)
                            .ToList();
                        }
                    }
                    else
                    {
                        entities = entities.Where(t => t.isDomestic == model.isDomestic.Value).ToList();
                        if (model.redZoneCountryId != null)
                        {
                            entities = entities.Where(t => t.redZoneCountryId == model.redZoneCountryId 
                            || t.redZoneCity == model.redZoneCity).ToList();
                        }
                        
                    }

                }

                return entities;
            }
            return null;

        }


        public async Task updateRedZone(redZone model)
        {
            _dbContext.RedZone.Update(model);
            await _dbContext.SaveChangesAsync();
        }


        // Red Zone Follow Up

        public async Task<RedZoneFollowUp> insertFollowUp(RedZoneFollowUp model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            _dbContext.RedZoneFollowUp.Add(model);
            await _dbContext.SaveChangesAsync();
            return model;
        }

        public async Task<List<RedZoneFollowUp>> listAllFollowUp()
        {
            return await _dbContext.RedZoneFollowUp.AsQueryable().ToListAsync();
        }

        public async Task UpdateRedZoneFollowUp(RedZoneFollowUp model)
        {
            _dbContext.RedZoneFollowUp.Update(model);
            await _dbContext.SaveChangesAsync();
        }


        public RedZoneFollowUp GetRedZonesFollowUpById(Guid? id)
        {
            var query = _dbContext.RedZoneFollowUp.Where(x => x.RedZoneFollowUpId == id);
            var result = query.FirstOrDefault();
            return result;
        }


        public async Task<RedZoneFollowUp> GetRedZonesFollowUpAndIncidentById(Guid? id)
        {
            var query = await _dbContext.RedZoneFollowUp.Include(h => h.IncidentReport)
                .Include(h => h.IncidentReport.Requester)
                .Include(h => h.IncidentReport.informationContactSuspectCaseCovid)
                .Include(h => h.IncidentReport.routesOfContactingWithColleagues)
                .Where(x => x.RedZoneFollowUpId == id).FirstOrDefaultAsync();
            return query;
        }


        // Update History
        public async Task<updateHistory> insertUpdateHistory(updateHistory model)
        {
            try
            {
                if (model == null)
                    throw new ArgumentNullException(nameof(model));
                _dbContext.updateHistory.Add(model);
                await _dbContext.SaveChangesAsync();
                return model;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw ex;
            }
            
        }

        public async Task<IList<updateHistory>> GetHistoryList(Guid? id)
        {
            var query = _dbContext.updateHistory.Where(x => x.redZoneFollowUpId == id);
            var result = await query.ToListAsync();
            return  result;
        }

        public async Task<List<updateHistory>> ListAllUpdateHistories()
        {
            return await _dbContext.updateHistory.AsQueryable().ToListAsync();
        }

    }
}
