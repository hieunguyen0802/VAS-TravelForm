using src.Core.Domains;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using src.Web.Common.Models.RedZone;


namespace src.Repositories.RedZone
{
    public interface IRedZoneRepo
    {
        Task<List<redZone>> filterRedZone(RedZoneModel model);
        Task<List<redZone>> listAllRequest();
        Task<redZone> insert(redZone model);
        redZone GetRedZonesById(Guid? id);
        Task<IList<TravelDeclaration>> FilterTravelRouteByRedZone(Guid id );
        Task<IList<TravelDeclaration>> FilterTravelRouteByRedZoneUsingModel(RedZoneModel model);

        Task updateRedZone(redZone model);

        Task UpdateRedZoneFollowUp(RedZoneFollowUp model);
        RedZoneFollowUp GetRedZonesFollowUpById(Guid? id);
        Task<List<RedZoneFollowUp>> listAllFollowUp();
        Task<RedZoneFollowUp> insertFollowUp(RedZoneFollowUp model);
        Task<RedZoneFollowUp> GetRedZonesFollowUpAndIncidentById(Guid? id);
        Task<updateHistory> insertUpdateHistory(updateHistory model);
        Task<IList<updateHistory>> GetHistoryList(Guid? id);
        Task<List<updateHistory>> ListAllUpdateHistories();
    }
}
