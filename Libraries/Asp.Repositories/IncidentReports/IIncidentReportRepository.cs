
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using src.Core.Domains;

namespace src.Repositories.IncidentReports
{
    public interface IIncidentReportRepository
    {
        Task<IncidentReport> insert(IncidentReport model);

        Task<IList<IncidentReport>> listAllRequestByCampus(int userId,string campus);

        Task<IList<IncidentReport>> listAllRequestByUser(int userId, string approver);

        Task<IncidentReport> getIncidentReportById(Guid? id);

        Task updateIncidentReport(IncidentReport model);

        Task<IList<IncidentReport>> getAllRequest();

   
    }
}
