using src.Core.Domains;
using src.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using src.Core.Data;
namespace src.Repositories.TravelDeclarations
{
    public interface ITravelDeclarationRepository
    {
        Task<TravelDeclaration> insert(TravelDeclaration model);

        Task<IList<TravelDeclaration>> listAllRequestByCampus(string campus);

        Task<IList<TravelDeclaration>> listAllRequestByUser(int userId,string approver);

        TravelDeclaration getTravelDeclarationById(Guid? id);

        Task<IList<TravelDeclaration>> getRequestIdById(string id);


        Task updateTravelDeclaration(TravelDeclaration model);

        Task<IList<TravelDeclaration>> getAllRequest();

        Task<IList<TravelDeclaration>> getAllRequestByDataFilter(SearchDataRequest filter);

        Task<List<TravelDeclarationDashboardModel>> getDashboardTravelDeclarationMoet();
        Task<List<TravelDeclarationDashboardModel>> getDashboardTravelDeclarationCam();
        Task<List<TravelDeclarationDashboardModel>> getDashboardCovidIncidentMoet();
        Task<List<TravelDeclarationDashboardModel>> getDashboardCovidIncidentCam();

    }
}
