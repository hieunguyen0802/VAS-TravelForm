using System.Linq;
using System.Threading.Tasks;
using src.Core;
using src.Core.Domains;
using src.Repositories.Settings;
using src.Web.Common.Models.SettingViewModels;
using src.Web.Common.Mvc.Alerts;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Web.Common.Models.ConfigViewModels;
using src.Repositories.Messages;
using Microsoft.AspNetCore.Mvc.Rendering;
using src.Repositories.Configs;
using System;
using src.Web.Common;

namespace src.Web.Areas.Administration.Controllers
{
    [Area(Constants.Areas.Administration)]
    [Authorize(Policy = Constants.RoleNames.Administrator)]
    public class SettingsController : Controller
    {
        private readonly ISettingRepository _settingRepository;
        private readonly IMapper _mapper;
        private readonly IEmailTemplateRepository _emailTemplateRepository;
        private readonly IEmailConfigRepository _emailConfigRepository;
        private readonly IDateTime _dateTime;
        private readonly IUserSession _userSession;
        public SettingsController(
            ISettingRepository settingRepository,
            IMapper mapper,
            IEmailTemplateRepository emailTemplateRepository,
            IEmailConfigRepository emailConfigRepository,
            IDateTime dateTime,
            IUserSession userSession
            )
        {
            _settingRepository = settingRepository;
            _mapper = mapper;
            _emailTemplateRepository = emailTemplateRepository;
            _emailConfigRepository = emailConfigRepository;
            _dateTime = dateTime;
            _userSession = userSession;
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        public IActionResult List()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ListSettings()
        {
            var result = _settingRepository.GetAllSettings()
                .Select(e => _mapper.Map<Setting, SettingViewModel>(e));
            return Json(result);
        }

        public IActionResult Edit(int id)
        {
            var setting = _settingRepository.GetSettingById(id);

            if (setting == null)
                return RedirectToAction("List");

            var model = _mapper.Map<Setting, SettingViewModel>(setting);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SettingViewModel model)
        {
            if (ModelState.IsValid)
            {
                var entity = _settingRepository.GetSettingById(model.Id);

                if (entity == null)
                    return RedirectToAction("List");

                entity = _mapper.Map(model, entity);
                await _settingRepository.UpdateSettingAsync(entity);
                return RedirectToAction("List").WithSuccess($"{entity.Name} setting was updated successfully.");
            }
            return View(model);
        }

        [HttpPost]
        public JsonResult ClearCache()
        {
            _settingRepository.ClearCache();
            return Json(null);
        }
        public async Task<IActionResult> EmailConfig()
        {
            ConfigsEmailViewModel viewmodel = new ConfigsEmailViewModel();
            var configs = await _emailConfigRepository.getAllConfig();
           
            if (configs != null)
            {
                var model = _mapper.Map<configs_email, ConfigsEmailViewModel>(configs);
                model.listEmails = new SelectList(await _emailTemplateRepository.GetAllEmailTemplates(), "Id", "Name");
                return View(model);

            }
            else
            {
                viewmodel.listEmails = new SelectList(await _emailTemplateRepository.GetAllEmailTemplates(), "Id", "Name");
                return View(viewmodel);
            }
           
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailConfig(ConfigsEmailViewModel model)
        {
            model.createdAt = _dateTime.Now;
            model.createdBy = _userSession.UserName;
            model.configs_email_id = Guid.NewGuid();
            try
            {
                if (ModelState.IsValid)
                {
                    await _emailConfigRepository.removeAllconfigs();
                    var mapper = _mapper.Map<ConfigsEmailViewModel, configs_email>(model);
                    await _emailConfigRepository.addEmaiConfig(mapper);
                    return RedirectToAction("EmailConfig").WithSuccess("Success!");
                }
                else
                {
                    return View("EmailConfig", model).WithError("Something went wrong. Please contact IT for support. Thanks !");
                }
            }
            catch (Exception ex)
            {

                return View("EmailConfig", model).WithError(ex.Message);
            }

        }
    }
}