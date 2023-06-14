using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using src.Core.Domains;
using src.Repositories.Category;
using src.Repositories.Users;
using System.Collections.Generic;
using src.Web.Common;
using src.Repositories.RedZone;

namespace src.Web.Controllers
{
    public class BaseCategoryController : Controller
    {
        private readonly IBaseCategoryRepository _baseCategory;
        private readonly IUserRepository _userRepository;
        private readonly IUserSession _userSession;
        private readonly IRedZoneRepo _redZoneRepo;
        public BaseCategoryController(IBaseCategoryRepository baseCategory, IUserRepository userRepository, IUserSession userSession, IRedZoneRepo redZoneRepo)
        {
            _baseCategory = baseCategory;
            _userRepository = userRepository;
            _userSession = userSession;
            _redZoneRepo = redZoneRepo;
        }

        public async Task<IActionResult> getCampus()
        {
            try
            {
                var entities = await _baseCategory.getCampusFromEngage();
                return Json(entities);
            }
            catch (Exception ex)
            {
                return Json(null);
            }
        }
        public async Task<IActionResult> getGradeByCampus(string CampusCode)
        {
            try
            {
                var entities = await _baseCategory.getGradeByCampusCode(CampusCode);
                return Json(entities);
            }
            catch (Exception ex)
            {
                return Json(null);
            }
        }
        public async Task<IActionResult> getClassByCampusAndGrade_Engage(string CampusCode, string GradeCode)
        {
            try
            {
                var entities = await _baseCategory.getClassByCampusAndGrade_Engage(CampusCode, GradeCode);
                return Json(entities);
            }
            catch (Exception ex)
            {
                return Json(null);
            }
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> getAllCampusAndDepartment()
        {
            var entities = await _userRepository.GetUserByIdAsync(_userSession.Id);
            List<BaseCategory> groupedList = entities.HeadOfDepartments.Select(i =>
                                 new BaseCategory()
                                 {
                                     Code = i.DepartmentOrCampus,
                                     CategoryName = i.DepartmentOrCampus
                                 }
                             ).ToList();
            return Json(groupedList);
        }

        public async Task<IActionResult> getColleagueInfo()
        {
            var entities = await _userRepository.GetUsersAsync();
            List<BaseCategory> groupedList = entities.Select(i =>
                                 new BaseCategory()
                                 {
                                     Code = i.UserCode + "-" + i.FirstName + " " + i.LastName + "-" + i.Position,
                                     CategoryName = string.Format($"{i.UserCode} - {i.FirstName} {i.LastName} - {i.Position} - {i.Campus} ")
                                 }).ToList();

            return Json(groupedList);
        }

        public async Task<IActionResult> getRedZoneInfo()
        {
            var entities = await _redZoneRepo.listAllRequest();
            List<BaseCategory> groupedList = entities.Where(t => t.isActivate == true).Select(i =>
                                 new BaseCategory()
                                 {
                                     Code = i.redZoneId.ToString(),
                                     CategoryName = i.redZoneName
                                 }).ToList();

            return Json(groupedList);
        }
    }
}