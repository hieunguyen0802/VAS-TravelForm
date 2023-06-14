using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using src.Repositories.Category;
using src.Core;
using Microsoft.AspNetCore.Authorization;
using src.Core.Domains;
using src.Repositories.Users;

namespace src.Web.Areas.Administration.Controllers.Api
{
    [Produces("application/json")]
    [Route("api/BaseCategory")]
    [Area(Constants.Areas.Administration)]
    [Authorize(Policy = Constants.RoleNames.Administrator)]
    public class BaseCategoryController : Controller
    {
        private readonly IBaseCategoryRepository _baseCategoryRepository;

        public BaseCategoryController(IBaseCategoryRepository baseCategoryRepository)
        {
            _baseCategoryRepository = baseCategoryRepository;
        }
        [HttpGet("getScheduleMeetings")]
        public async Task<IActionResult> getScheduleMeetings()
        {
            try
            {
                var list = await _baseCategoryRepository.getAllScheduleMeetings();
                return Json(list);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        [HttpGet("getAllSourceEnquiries")]
        public async Task<IActionResult> getAllSourceEnquiries()
        {
            try
            {
                var entities = await _baseCategoryRepository.getAllSourceEnquiries();
                return Json(entities);
            }
            catch (Exception ex)
            {
                return Json(null);
            }
        }
        [HttpGet("getAllSourceEnquiriesExept")]
        public async Task<IActionResult> getAllSourceEnquiriesExept(string Id)
        {
            try
            {
                var entities = await _baseCategoryRepository.getAllSourceEnquiriesExept(Id);
                return Json(entities);
            }
            catch (Exception ex)
            {
                return Json(null);
            }
        }
        [HttpGet("getCampus")]
        public async Task<IActionResult> getCampus()
        {
            try
            {
                var list = await _baseCategoryRepository.getCampusAsync();
                return Json(list);
            }
            catch (Exception)
            {
                return null;
            }
        }
        [HttpGet("getSchoolYear")]
        public async Task<IActionResult> getSchoolYear()
        {
            try
            {
                var list = await _baseCategoryRepository.getSchoolYear();
                return Json(list);
            }
            catch (Exception)
            {
                return null;
            }
        }
        [HttpGet("getGradeByCampus")]
        public async Task<IActionResult> getGradeByCampus(string CodeCampus)
        {
            try
            {
                var entities = await _baseCategoryRepository.getGradeByCampusAsync(CodeCampus);
                return Json(entities);
            }
            catch (Exception)
            {
                return Json(null);
            }
        }
        [HttpGet("getClassByCampusAndGrade")]
        public async Task<IActionResult> getClassByCampusAndGrade(string CodeCampus, string CodeGrade)
        {
            try
            {
                var entities = await _baseCategoryRepository.getClassByGradeAndCampus(CodeCampus, CodeGrade);
                return Json(entities);
            }
            catch (Exception)
            {
                return Json(null);
            }
        }
        [HttpGet("getListStudentByPhoneNumber")]
        [AllowAnonymous]
        public async Task<IActionResult> getListStudentByPhoneNumber(string PhoneNumber)
        {
            try
            {
                var entities = await _baseCategoryRepository.getListStudentByPhoneNumber(PhoneNumber);
                return Json(entities);
            }
            catch (Exception ex)
            {
                return Json(null);
            }
        }
        [HttpGet("getAcedemicYearFromEnage")]
        public async Task<IActionResult> getAcedemicYearFromEnage()
        {
            try
            {
                var entities = await _baseCategoryRepository.getAcedemicYearFromEngage();
                return Json(entities);
            }
            catch (Exception ex)
            {
                return Json(null);
            }
        }
        [HttpGet("GetAllCampusFromEngage")]
        public async Task<IActionResult> GetAllCampusFromEngage()
        {
            try
            {
                var entities = await _baseCategoryRepository.getCampusFromEngage();
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
    }
}