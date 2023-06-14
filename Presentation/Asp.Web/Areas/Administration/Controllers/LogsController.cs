using System;
using System.Linq;
using System.Threading.Tasks;
using src.Core;
using src.Core.Data;
using src.Core.Extensions;
using src.Repositories.Logging;
using src.Web.Common.Models.LogViewModels;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace src.Web.Areas.Administration.Controllers
{
    [Area(Constants.Areas.Administration)]
    [Authorize(Policy = Constants.RoleNames.Administrator)]
    public class LogsController : Controller
    {
        private readonly ILogRepository _logRepository;
        private readonly IMapper _mapper;

        public LogsController(
            ILogRepository logRepository,
            IMapper mapper)
        {
            _logRepository = logRepository;
            _mapper = mapper;
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        public IActionResult List()
        {
            return View();
        }

        public async Task<IActionResult> GetLogs()
        {
            var entities = await _logRepository.GetLogs();
            var models = entities.Select(l => _mapper.Map<Core.Domains.Log, LogViewModel>(l)).OrderByDescending(t=>t.Logged).ToList();
            return Json(new { data = models });
        }
    }
}