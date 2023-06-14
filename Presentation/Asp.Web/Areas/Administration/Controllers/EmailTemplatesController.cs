using System.Linq;
using System.Net;
using System.Threading.Tasks;
using src.Core;
using src.Core.Domains;
using src.Repositories.Messages;
using src.Web.Common;
using src.Web.Common.Models.EmailTemplateViewModels;
using src.Web.Common.Mvc.Alerts;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.IO;
using System.Drawing;
using src.Web.Extensions;
using src.Repositories.Category;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace src.Web.Areas.Administration.Controllers
{
    [Area(Constants.Areas.Administration)]
    [Authorize(Policy = Constants.RoleNames.Administrator)]
    public class EmailTemplatesController : Controller
    {
        private readonly IDateTime _dateTime;
        private readonly IEmailTemplateRepository _emailTemplateRepository;
        private readonly IBaseCategoryRepository _baseCategoryRepository;
        private readonly IMapper _mapper;
        private readonly IUserSession _userSession;

        public EmailTemplatesController(
            IDateTime dateTime,
            IEmailTemplateRepository emailTemplateRepository,
            IBaseCategoryRepository baseCategoryRepository,
            IMapper mapper,
            IUserSession userSession)
        {
            _dateTime = dateTime;
            _emailTemplateRepository = emailTemplateRepository;
            _baseCategoryRepository = baseCategoryRepository;
            _mapper = mapper;
            _userSession = userSession;
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        public IActionResult List()
        {
            return View("List");
        }
        public async Task<IActionResult> ListEmailTemplates()
        {
            var entities = await _emailTemplateRepository.GetAllEmailTemplates();
            var models = entities.Select(e => _mapper.Map<EmailTemplate, EmailTemplateViewModel>(e)).ToList();
            return Json(new { data = models });
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var emailTemplate = await _emailTemplateRepository.GetEmailTemplateById(id);

            if (emailTemplate == null)
                return RedirectToAction("List");

            var model = _mapper.Map<EmailTemplate, EmailTemplateViewModel>(emailTemplate);
            return View(model);
        }
        public async Task<IActionResult> Create()
        {
            var model = new EmailTemplateViewModel();
            return View(model);
        }
        

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EmailTemplateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var emailTemplate = await _emailTemplateRepository.GetEmailTemplateById(model.Id);

                if (emailTemplate == null)
                    return RedirectToAction("List");

                model.CreatedBy = emailTemplate.CreatedBy;
                model.ModifiedBy = emailTemplate.ModifiedBy;
                emailTemplate = _mapper.Map(model, emailTemplate);
                emailTemplate.Body = WebUtility.HtmlDecode(model.Body);
                emailTemplate.Instruction = WebUtility.HtmlDecode(model.Instruction);
                emailTemplate.ModifiedBy = _userSession.UserName;
                emailTemplate.Campus = model.Campus;
                emailTemplate.ModifiedOn = _dateTime.Now;
               
                await _emailTemplateRepository.UpdateEmailTemplate(emailTemplate);
                return RedirectToAction("List").WithSuccess($"{emailTemplate.Name} template was updated successfully.");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmailTemplateViewModel model)
        {
            if (ModelState.IsValid)
            {

                model.Body = WebUtility.HtmlDecode(model.Body);
                model.Instruction = WebUtility.HtmlDecode(model.Instruction);
                var dateTimeNow = _dateTime.Now;
                var username = _userSession.UserName;
                var emailTemplate = _mapper.Map<EmailTemplateViewModel, EmailTemplate>(model);
                emailTemplate.CreatedBy = username;
                emailTemplate.CreatedOn = _dateTime.Now;
                emailTemplate.ModifiedBy = username;
                emailTemplate.ModifiedOn = _dateTime.Now;
                await _emailTemplateRepository.InsertEmailTemplate(emailTemplate);
                return RedirectToAction("List").WithSuccess($"{emailTemplate.Name} template was created successfully.");
            }
            return View(model);
        }
        [HttpPost]
        public ActionResult UploadImage(IFormFile upload, string CKEditorFuncNum, string CKEditor, string langCode)
        {
            if (upload.Length <= 0) return null;
            if (!upload.IsImage())
            {
                var NotImageMessage = "please choose a picture";
                dynamic NotImage = JsonConvert.DeserializeObject("{ 'uploaded': 0, 'error': { 'message': \"" + NotImageMessage + "\"}}");
                return Json(NotImage);
            }

            var fileName = Guid.NewGuid() + Path.GetExtension(upload.FileName).ToLower();

            Image image = Image.FromStream(upload.OpenReadStream());
            int width = image.Width;
            int height = image.Height;
            if ((width > 750) || (height > 500))
            {
                var DimensionErrorMessage = "Custom Message for error";
                dynamic stuff = JsonConvert.DeserializeObject("{ 'uploaded': 0, 'error': { 'message': \"" + DimensionErrorMessage + "\"}}");
                return Json(stuff);
            }

            if (upload.Length > 500 * 1024)
            {
                var LengthErrorMessage = "Custom Message for error";
                dynamic stuff = JsonConvert.DeserializeObject("{ 'uploaded': 0, 'error': { 'message': \"" + LengthErrorMessage + "\"}}");
                return Json(stuff);
            }

            var path = Path.Combine(
                Directory.GetCurrentDirectory(), "wwwroot/images/CKEditorImages",
                fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                upload.CopyTo(stream);

            }

            var url = $"{"/images/CKEditorImages/"}{fileName}";
            var successMessage = "image is uploaded successfully";
            dynamic success = JsonConvert.DeserializeObject("{ 'uploaded': 1,'fileName': \"" + fileName + "\",'url': \"" + url + "\", 'error': { 'message': \"" + successMessage + "\"}}");
            return Json(success);
        }
    }
}