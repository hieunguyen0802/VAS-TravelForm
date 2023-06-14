using src.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Threading.Tasks;
using System;
using src.Repositories.IncidentReports;
using src.Repositories.TravelDeclarations;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using src.Core.Domains;


namespace src.Web.Areas.Administration.Controllers
{
    [Area(Constants.Areas.Administration)]
    [Authorize(Policy = Constants.RoleNames.Administrator)]
    
    public class DashboardController : Controller
    {
        private readonly ITravelDeclarationRepository _travelDeclarationRepository;


        public DashboardController(ITravelDeclarationRepository travelDeclarationRepository)
        {
            _travelDeclarationRepository = travelDeclarationRepository;
         
        }

       public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> DashboardMoetTravel()
        {
            var result = await _travelDeclarationRepository.getDashboardTravelDeclarationMoet();
            return PartialView("_DashboardMoetTravelView", result);
        }
        public async Task<IActionResult> DashboardCamTravel()
        {
            var result = await _travelDeclarationRepository.getDashboardTravelDeclarationCam();
            return PartialView("_DashboardCamTravelView", result);
        }
        public async Task<IActionResult> DashboardMoetCovid()
        {
            var result = await _travelDeclarationRepository.getDashboardCovidIncidentMoet();
            return PartialView("_DashboardMoetCovidView", result);
        }
        public async Task<IActionResult> DashboardCamCovid()
        {
            var result = await _travelDeclarationRepository.getDashboardCovidIncidentCam();
            return PartialView("_DashboardCamCovidView", result);
        }

    }
}