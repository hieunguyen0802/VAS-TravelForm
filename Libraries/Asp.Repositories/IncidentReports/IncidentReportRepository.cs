using src.Core.Domains;
using src.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using src.Core.Data;
using src.Core.Enums;
using src.Repositories.Users;

namespace src.Repositories.IncidentReports
{
    public class IncidentReportRepository : IIncidentReportRepository
    {

        public readonly IDbContext _dbContext;
        private readonly IUserRepository _userRepository;

        public IncidentReportRepository(IDbContext dbContext, IUserRepository userRepository)
        {
            _dbContext = dbContext;
            _userRepository = userRepository;
        }
        public async Task<IList<IncidentReport>> getAllRequest()
        {
            return await _dbContext.incidentReport
                .Include(r => r.routesAtRedZones)
                .Include(e => e.routesOutsizeRedZones)
                .Include(h => h.routesOfContactingWithColleagues)
                .Include(s => s.informationContactSuspectCaseCovid)
                .Include(t => t.Requester).AsQueryable().ToListAsync();

        }

        public async Task<IList<TravelDeclaration>> getAllRequestByDataFilter(SearchDataRequest filter)
        {
            var entities = await _dbContext.incidentReport.Include(t => t.Requester).ToListAsync();

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


                return (IList<TravelDeclaration>)entities;
            }
            return null;
        }



        public async Task<IncidentReport> getIncidentReportById(Guid? id)
        {
            var result = await _dbContext.incidentReport.Include(i => i.Requester).Include(i => i.routesAtRedZones).Include(i => i.routesOfContactingWithColleagues).Include(i => i.routesOutsizeRedZones).Include(t => t.informationContactSuspectCaseCovid)
                .Where(i => i.IncidentReportId == id).FirstOrDefaultAsync();
            return result;
        }

        public async Task<IncidentReport> insert(IncidentReport model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            _dbContext.incidentReport.Add(model);
            await _dbContext.SaveChangesAsync();
            return model;
        }

        public async Task<IList<IncidentReport>> listAllRequestByCampus(int userId, string campus)
        {

            var entities = await _dbContext.incidentReport.Include(t => t.Requester)
                .Include(r => r.routesAtRedZones)
                .Include(e => e.routesOutsizeRedZones)
                .Include(h => h.routesOfContactingWithColleagues)
                .Include(s => s.informationContactSuspectCaseCovid).ToListAsync();
            if (campus == "0")
            {
                var userDetails = await _userRepository.GetUserByIdAsync(userId);
                if (userDetails != null)
                {
                    if (userDetails.HeadOfDepartments.Count() > 0)
                    {
                        string[] arrCampus = userDetails.HeadOfDepartments.Select(u => u.DepartmentOrCampus.ToString()).ToArray();
                        return entities.Where(t => t.Requester.Campus.ContainsAny(arrCampus)).ToList();
                    }
                    else
                    {
                        return entities = await _dbContext.incidentReport
                .Include(t => t.Requester)
                .Include(r => r.routesAtRedZones)
                .Include(e => e.routesOutsizeRedZones)
                .Include(h => h.routesOfContactingWithColleagues)
                .Include(s => s.informationContactSuspectCaseCovid)
                .Where(t => t.RequesterId == userId || t.nameOfLineManager.Contains(userDetails.UserName) || t.ECSDEmail.Contains(userDetails.UserName) || t.informedEmail.Contains(userDetails.UserName))
                .OrderBy(t => t.request_id).ToListAsync();
                    }
                }
            }
            else
            {
                return entities
                    .Where(t => t.Requester.Campus.ContainsAny(campus)).ToList();
            }
            return null;
        }

        public async Task<IList<IncidentReport>> listAllRequestByUser(int userId, string approver)
        {
            if (userId == 0)
                throw new ArgumentNullException(nameof(userId));
            var entities = await _dbContext.incidentReport
                .Include(t => t.Requester)
                .Include(r => r.routesAtRedZones)
                .Include(e => e.routesOutsizeRedZones)
                .Include(h => h.routesOfContactingWithColleagues)
                .Include(s => s.informationContactSuspectCaseCovid)
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
                            e.status == (int)RequestStatus.Submitted && e.RequesterId != userId && e.nameOfLineManager.Split("@")[0] != approver
                            && e.ECSDEmail.Split("@")[0] == approver);
                    }
                    return entities;
                }
            }
            return entities;
        }

        public async Task updateIncidentReport(IncidentReport model)
        {
            _dbContext.incidentReport.Update(model);
            await _dbContext.SaveChangesAsync();
        }

    }
    public static class StringExtensions
    {
        public static bool ContainsAny(this string str, params string[] values)
        {
            if (!string.IsNullOrEmpty(str) || values.Length > 0)
            {
                foreach (string value in values)
                {
                    if (str.Contains(value))
                        return true;
                }
            }

            return false;
        }
    }
}
