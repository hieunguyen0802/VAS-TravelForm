using src.Core.Domains;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace src.Repositories.Category
{
    public interface IBaseCategoryRepository
    {
        Task<IList<BaseCategory>> getCampusAsync();
        Task<IList<BaseCategory>> getSchoolYear();
        Task<IList<BaseCategory>> getGradeByCampusAsync(string CodeCampus);
        Task<IList<BaseCategory>> getClassByGradeAndCampus(string CodeCampus, string Grade);
        Task<IList<BaseCategory>> getAllScheduleMeetings();
        Task<IList<BaseCategory>> getAllSourceEnquiries();
        Task<IList<BaseCategory>> getAllSourceEnquiriesExept(string Id);
        Task<IList<BaseCategory>> getListStudentByPhoneNumber(string PhoneNumber);
        Task<IList<BaseCategory>> getAcedemicYearFromEngage();
        Task<IList<BaseCategory>> getCampusFromEngage();
        Task<IList<BaseCategory>> getGradeByCampusCode(string CampusCode);
        Task<IList<BaseCategory>> getClassByCampusAndGrade_Engage(string CampusCode, string GradeCode);
        Task<IList<BaseCategory>> getContactType_Engage();
        Task<IList<Province>> GetProvinces();
        Task<IList<District>> GetDistrictsByProvinceId(string ProvinceId);
        Task<IList<Ward>> GetWardsByDistrictId(string DistrictId);

        Task<IList<Country>> GetCountries();
        Task<IList<District>> GetDistrict();
        Task<IList<Ward>> GetWard();

        Task<District> GetDistrictsById(string Id);
        Task<Ward> GetWardsById(string Id);
    }
}
