using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using src.Core.Domains;
using src.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace src.Repositories.Category
{
    public class BaseCategoryRepository : IBaseCategoryRepository
    {
        private readonly AppDbContext _context;

        public BaseCategoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IList<BaseCategory>> getAllScheduleMeetings()
        {
            var query = _context.BaseCategories.FromSql("GetAllScheduleMeetings").AsQueryable();
            return await query.ToListAsync();
        }

        public async Task<IList<BaseCategory>> getAllSourceEnquiries()
        {
            var query = _context.BaseCategories.FromSql("GetAllSouceEnquiries").AsQueryable();
            return await query.ToListAsync();
        }

        public async Task<IList<BaseCategory>> getAllSourceEnquiriesExept(string Id)
        {
            var query = _context.BaseCategories.FromSql($"GetAllSouceEnquiriesExcept {Id}").AsQueryable();
            return await query.ToListAsync();
        }

        public async Task<IList<BaseCategory>> getCampusAsync()
        {
            var query = _context.BaseCategories.FromSql("GetAllCampus").AsQueryable();
            return await query.ToListAsync();
        }

        public async Task<IList<BaseCategory>> getClassByGradeAndCampus(string CodeCampus, string Grade)
        {
            var query = _context.BaseCategories.FromSql("GetClassByGradeAndCampus @Campus = {0} ,@Grade = {1}", CodeCampus, Grade).AsQueryable();
            return await query.ToListAsync();
        }

        public async Task<IList<BaseCategory>> getGradeByCampusAsync(string CodeCampus)
        {
            var query = _context.BaseCategories.FromSql($"GetAllGrade {CodeCampus}").AsQueryable();
            return await query.ToListAsync();
        }

        public async Task<IList<BaseCategory>> getSchoolYear()
        {
            var query = _context.BaseCategories.FromSql("GetSchoolYear").AsQueryable();
            return await query.ToListAsync();
        }
        public async Task<IList<BaseCategory>> getListStudentByPhoneNumber(string PhoneNumber)
        {
            var query = _context.BaseCategories.FromSql($"GetListStudentByPhoneNumber {PhoneNumber}").AsQueryable();
            return await query.ToListAsync();
        }

        public async Task<IList<BaseCategory>> getAcedemicYearFromEngage()
        {
            var query = _context.BaseCategories.FromSql("GetAcedemicYearFromEngage").AsQueryable();
            return await query.ToListAsync();
        }

        public async Task<IList<BaseCategory>> getCampusFromEngage()
        {
            var query = _context.BaseCategories.FromSql("GetAllCampusFromEngage").AsQueryable();
            return await query.ToListAsync();
        }

        public async Task<IList<BaseCategory>> getGradeByCampusCode(string CampusCode)
        {
            var query = _context.BaseCategories.FromSql($"GetGradeByCampus {CampusCode}").AsQueryable();
            return await query.ToListAsync();
        }

        public async Task<IList<BaseCategory>> getClassByCampusAndGrade_Engage(string CampusCode, string GradeCode)
        {
            var query = _context.BaseCategories.FromSql("GetClassByCampusAndGrade_Engage @CampusCode = {0} ,@gradeCode = {1}", CampusCode, GradeCode).AsQueryable();
            return await query.ToListAsync();
        }

        public async Task<IList<BaseCategory>> getContactType_Engage()
        {
            var query = _context.BaseCategories.FromSql("getContactType_Engage").AsQueryable();
            return await query.ToListAsync();
        }

        public async Task<IList<Province>> GetProvinces()
        {
            var result = _context.province.AsQueryable();
            return await result.ToListAsync();
        }

        public async Task<IList<District>> GetDistrict()
        {
            var result = _context.district.AsQueryable();
            return await result.ToListAsync();
        }

        public async Task<IList<Ward>> GetWard()
        {
            var result = _context.ward.AsQueryable();
            return await result.ToListAsync();
        }


        public async Task<IList<District>> GetDistrictsByProvinceId(string ProvinceId)
        {
            if (string.IsNullOrEmpty(ProvinceId))
                throw new ArgumentNullException(nameof(ProvinceId));
            var result = _context.district.Where(t => t.provinceId == ProvinceId).AsQueryable();
            return await result.ToListAsync();
        }

        public async Task<IList<Ward>> GetWardsByDistrictId(string DistrictId)
        {
            if (string.IsNullOrEmpty(DistrictId))
                throw new ArgumentNullException(nameof(DistrictId));
            var result = _context.ward.Where(t => t.districtId == DistrictId).AsQueryable();
            return await result.ToListAsync();
        }

        public async Task<District> GetDistrictsById(string Id)
        {
            return await _context.district.Where(t => t.districtId == Id).FirstOrDefaultAsync();
        }

        public async Task<Ward> GetWardsById(string Id)
        {
            return await _context.ward.Where(t=>t.wardId == Id).FirstOrDefaultAsync();
        }

        public async Task<IList<Country>> GetCountries()
        {
            var result = _context.country.AsQueryable();
            return await result.ToListAsync();
        }
    }
}
