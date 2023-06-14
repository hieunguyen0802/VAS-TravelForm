using System;
using OfficeOpenXml;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using src.Core;
using src.Core.Domains;
using src.Core.Enums;
using src.Emails;
using src.Repositories.Configs;
using src.Repositories.TravelDeclarations;
using src.Repositories.TravellingRoutes;
using src.Repositories.Users;
using src.Web.Common;
using src.Web.Common.Models.TravelDeclarations;
using src.Web.Common.Models.UserViewModels;
using src.Web.Common.Mvc.Alerts;
using src.Web.Extensions;
using src.Web.Helpers;
using src.Repositories.Category;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using MimeKit;
using OfficeOpenXml.Style;
using System.Drawing;
using Microsoft.AspNetCore.SignalR;
using System.Data;

namespace src.Web.Controllers
{
    [Authorize]
    public class TravelDeclarationController : Controller
    {
        private readonly IHubContext<downloadFileHub> _hubContext;
        private readonly IDateTime _dateTime;
        private readonly IMapper _mapper;
        private readonly ILogger<TravelDeclarationController> _logger;
        private readonly IUserSession _userSession;
        private readonly ITravelDeclarationRepository _travelDeclarationRepository;
        private readonly ITravellingRoutesRepository _travellingRoutesRepository;
        private readonly IUserRepository _userRepository;
        private readonly IHostingEnvironment _hostingEnvironment;
        private IAuthorizationService _authorizationService;
        private IEmailConfigRepository _emailConfigRepository;
        private readonly IBackgroundJobClient _backgroungJobClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IBaseCategoryRepository _baseCategoryRepository;
        private IEmailSender _emailSender;
        private readonly userGroupSettings _userGroupSettings;
        public TravelDeclarationController(
            IDateTime dateTime,
            IMapper mapper,
            ILogger<TravelDeclarationController> logger,
            IUserSession userSession,
            ITravelDeclarationRepository travelDeclarationRepository,
            ITravellingRoutesRepository travellingRoutesRepository,
            IUserRepository userRepository,
            IHostingEnvironment hostingEnvironment,
            IAuthorizationService authorizationService,
            IEmailConfigRepository emailConfigRepository,
            IEmailSender emailSender,
            IBackgroundJobClient backgroungJobClient,
            IHttpContextAccessor httpContextAccessor,
            IBaseCategoryRepository baseCategoryRepository,
            IOptions<userGroupSettings> userGroupSettings,
            IHubContext<downloadFileHub> hubContext
            )
        {
            _dateTime = dateTime;
            _mapper = mapper;
            _logger = logger;
            _userSession = userSession;
            _travelDeclarationRepository = travelDeclarationRepository;
            _travellingRoutesRepository = travellingRoutesRepository;
            _userRepository = userRepository;
            _hostingEnvironment = hostingEnvironment;
            _authorizationService = authorizationService;
            _emailConfigRepository = emailConfigRepository;
            _emailSender = emailSender;
            _backgroungJobClient = backgroungJobClient;
            this._httpContextAccessor = httpContextAccessor;
            _baseCategoryRepository = baseCategoryRepository;
            _userGroupSettings = userGroupSettings.Value;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                if (_userGroupSettings.HRGroup.FirstOrDefault(t => t == _userSession.UserName) != null)
                {
                    var entities = await _travelDeclarationRepository.getAllRequest();
                    var model = entities.Select(e => _mapper.Map<TravelDeclaration, TravelDeclarationListOfUser>(e)).OrderBy(t => t.departureDate).ToList();
                    return View(model);
                }
                else
                {
                    var entities = await _travelDeclarationRepository.listAllRequestByUser(_userSession.Id, _userSession.UserName.Split('@')[0]);
                    var model = entities.Select(e => _mapper.Map<TravelDeclaration, TravelDeclarationListOfUser>(e)).OrderBy(t => t.status).ToList();
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{_userSession.UserName} got error {ex} at {_dateTime.Now}");
                _logger.LogCritical(ex.Message, ex);
                return View("Commmon", "Error");
            }


        }
        [HttpGet]
        public async Task<IActionResult> Create(TravelDeclarationModel model)
        {
            try
            {
                model.dateOfStatus = _dateTime.Now;
                model.Provinces = new SelectList(await _baseCategoryRepository.GetProvinces(), "provinceId", "name");
                model.Countries = new SelectList(await _baseCategoryRepository.GetCountries(), "countryId", "name");
                model.ECSDEmailList = await GetECSDGroup(_userGroupSettings.ECSDGroup);
                model.HREmailList = await GetECSDGroup(_userGroupSettings.HRGroup);
                model.lineManagerGroup = await GetLineManagerGroup(_userGroupSettings.LineManagerGroup);
                var requester = await _userRepository.GetUserByUserNameAsync(_userSession.UserName.Trim());
                var user = _mapper.Map<User, UserViewModel>(requester);
                model.Requester = user;
                var arrayInformed = new[] { requester.informed1, requester.informed2, requester.informed3, requester.informed4, requester.informed5, requester.informed6 };
                string fullInformed = string.Join(",", arrayInformed.Where(s => !string.IsNullOrEmpty(s)));
                model.informedEmail = fullInformed;
                model.status = 0;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{_userSession.UserName} got error {ex} at {_dateTime.Now}");
                _logger.LogCritical(ex.Message, ex);
                return View("Commmon", "Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TravelDeclarationModel model, IFormFile departureTicket, IFormFile returningTicket)
        {
            if (!ModelState.IsValid)
            {
                try
                {
                    if (model.travellingRoutes == null || model.travellingRoutes.Count < 0)
                    {
                        return View(model).WithError("ERROR! TravellingRoutes not null");
                    }
                    var userInfo = await _userRepository.GetUserByUserNameAsync(_userSession.UserName);
                    userInfo.Mobile = model.Requester.Mobile;
                    await _userRepository.UpdateUserAsync(userInfo);
                    var user_mapper = _mapper.Map<User, UserViewModel>(userInfo);
                    user_mapper.Mobile = model.Requester.Mobile;

                    model.Requester = user_mapper;
                    model.createdOn = _dateTime.Now;
                    var travel_declaration = _mapper.Map<TravelDeclarationModel, TravelDeclaration>(model);
                    travel_declaration.TravelDeclarationId = Guid.NewGuid();

                    var all_request_campus = await _travelDeclarationRepository.getAllRequest();
                    var number_of_requst = all_request_campus.Count() + 1;

                    travel_declaration.request_id = string.Format("{0} - {1}", "TV", number_of_requst);
                    travel_declaration.RequesterId = user_mapper.Id;
                    travel_declaration.dateOfStatus = null;
                    travel_declaration.status = (int)RequestStatus.Submitted;
                    travel_declaration.LatestStatus = (int)RequestStatus.Submitted;
                    travel_declaration.campusTemp = user_mapper.Campus;
                    travel_declaration.positionTemp = user_mapper.Position;

                    //Upload file
                    //Departure
                    var uploads = Path.Combine(_hostingEnvironment.WebRootPath, "Files");
                    FileUploadHelper objFile = new FileUploadHelper();

                    if (departureTicket != null)
                    {
                        string strFilePathDeparture = await objFile.SaveFileAsync(departureTicket, uploads, "Departure-");
                        strFilePathDeparture = strFilePathDeparture
                         .Replace(_hostingEnvironment.WebRootPath, string.Empty)
                         .Replace("\\", "/");

                        travel_declaration.departureTicketPath = strFilePathDeparture;
                        travel_declaration.departureTicket = objFile.GetFileName(departureTicket, true, "Departure-");
                    }

                    if (returningTicket != null)
                    {
                        //Returning
                        string strFilePathReturning = await objFile.SaveFileAsync(returningTicket, uploads, "Returning-");
                        strFilePathReturning = strFilePathReturning
                         .Replace(_hostingEnvironment.WebRootPath, string.Empty)
                         .Replace("\\", "/");

                        travel_declaration.returningTicketPath = strFilePathReturning;
                        travel_declaration.returningTicket = objFile.GetFileName(returningTicket, true, "Returning-");
                    }

                    if (travel_declaration.travelFromCountryId == "VN")
                    {
                        travel_declaration.travelFromIntl = null;
                    }

                    if (travel_declaration.travelToCountryId == "VN")
                    {
                        travel_declaration.travelToIntl = null;
                    }

                    foreach (var item in travel_declaration.travellingRoutes)
                    {
                        int count = 0;
                        for (int i = 0; i < item.TravelRouteFullAddress.Length; i++)
                        {

                            if (item.TravelRouteFullAddress[i].ToString().Equals("-"))
                            {
                                count++;

                            }
                        }
                        if (count == 2)
                        {
                            item.TravelRouteProvinceId = null;
                            item.TravelRouteDistrictId = null;
                            item.TravelRouteWardId = null;
                        }
                        if (count == 3)
                        {
                            item.travelRouteCountryId = null;
                            item.travelRouteCity = null;
                        }

                    }

                    await _travelDeclarationRepository.insert(travel_declaration);
                    _logger.LogInformation($"User {_userSession.UserName} created request with id {travel_declaration.request_id} success at {_dateTime.Now}");
                    string host = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
                    _backgroungJobClient.Enqueue(() => SendEmail(travel_declaration, (int)RequestStatus.Submitted, host));

                    return RedirectToAction("Index").WithSuccess("Submitted success !");

                }
                catch (Exception ex)
                {
                    _logger.LogError($"{_userSession.UserName} got error {ex},{ex.Message} at {_dateTime.Now}");
                    _logger.LogCritical(ex.Message, ex);
                    return RedirectToAction("Index").WithError("Something went wrong. Please contact ithdelpdesk@vas.edu.vn. Thank you!!");
                }
            }
            _logger.LogError($"{_userSession.UserName} got error invalid model at {_dateTime.Now}");
            return RedirectToAction("Index").WithError("Something went wrong. Please contact ithdelpdesk@vas.edu.vn. Thank you !!");
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var entity = _travelDeclarationRepository.getTravelDeclarationById(id);
                if (entity == null)
                    return RedirectToAction("Index");

                var model = _mapper.Map<TravelDeclaration, TravelDeclarationModel>(entity);
                model.Provinces = new SelectList(await _baseCategoryRepository.GetProvinces(), "provinceId", "name");
                model.Countries = new SelectList(await _baseCategoryRepository.GetCountries(), "countryId", "name");

                var isOwner = await _authorizationService.AuthorizeAsync(User, model, Constants.Permission_Workflow.IsOwner);

                var isApprover = await _authorizationService.AuthorizeAsync(User, model, Constants.Permission_Workflow.IsApprover);

                var isHeadofDepartment = await _authorizationService.AuthorizeAsync(User, model, Constants.Permission_Workflow.IsHeadOfDepartment);

                var isECSDGroup = await _authorizationService.AuthorizeAsync(User, model, Constants.Permission_Workflow.isECSDGroup);

                if (!isOwner.Succeeded && !isApprover.Succeeded && !isHeadofDepartment.Succeeded && !isECSDGroup.Succeeded && !_userGroupSettings.HRGroup.Any(str => str.Contains(_userSession.UserName)))
                {
                    return View("AccessDenied", "Common");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{_userSession.UserName} got error {ex} at {_dateTime.Now}");
                _logger.LogCritical(ex.Message, ex);
            }
            return View("Index").WithError("Something went wrong. Please contact ithelpdesk@vas.edu.vn to support");

        }


        #region LineManager  
        [HttpPost]
        public async Task<IActionResult> approveRequest(TravelDeclaration model)
        {
            try
            {
                var entity = _travelDeclarationRepository.getTravelDeclarationById(model.TravelDeclarationId);
                if (entity == null)
                    return RedirectToAction("Index");
                if (entity.CancelStatus == RequestStatus.cancelled)
                {
                    return View("Edit", entity).WithError("The request you selected has been cancelled");
                }
                if (entity.status == (int)RequestStatus.Submitted)
                {
                    entity.status = (int)RequestStatus.lineManager_Approved;
                    entity.LatestStatus = (int)RequestStatus.lineManager_Approved;
                    entity.dateOfStatus = _dateTime.Now;
                    entity.comment = model.comment;
                    await _travelDeclarationRepository.updateTravelDeclaration(entity);
                    _logger.LogInformation($"User{_userSession.UserName} approved request with id {model.request_id} success"); ;
                    string host = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
                    _backgroungJobClient.Enqueue(() => SendEmail(entity, (int)RequestStatus.sent_request_to_ECSD, host));
                }
                return RedirectToAction("Index").WithSuccess("Thank you for your response!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"{_userSession.UserName} got error {ex} at {_dateTime.Now}");
                _logger.LogCritical(ex.Message, ex);
            }
            return View("Index").WithError("Something went wrong. Please contact ithelpdesk@vas.edu.vn to support");


        }
        [HttpPost]
        public async Task<IActionResult> rejectRequest(TravelDeclaration model)
        {
            try
            {
                var entity = _travelDeclarationRepository.getTravelDeclarationById(model.TravelDeclarationId);
                if (entity == null)
                {
                    return RedirectToAction("Index");
                }
                if (entity.CancelStatus == RequestStatus.cancelled)
                {
                    return View("Edit", entity).WithError("The request you selected has been cancelled");
                }
                if (entity.status == (int)RequestStatus.Submitted)
                {
                    entity.status = (int)RequestStatus.lineManager_Rejected;
                    entity.LatestStatus = (int)RequestStatus.lineManager_Rejected;
                    entity.comment = model.comment;
                    entity.dateOfStatus = _dateTime.Now;
                    await _travelDeclarationRepository.updateTravelDeclaration(entity);
                    _logger.LogInformation($"User{_userSession.UserName} rejected request with id {model.request_id} success"); ;
                    string host = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
                    _backgroungJobClient.Enqueue(() => SendEmail(entity, (int)RequestStatus.lineManager_Rejected, host));

                }
                return RedirectToAction("Index").WithSuccess("Thank you for your response!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"{_userSession.UserName} got error {ex} at {_dateTime.Now}");
                _logger.LogCritical(ex.Message, ex);
            }
            return View("Index").WithError("Something went wrong. Please contact ithelpdesk@vas.edu.vn to support");

        }
        #endregion

        #region supportMethod
        public async Task<IActionResult> ListAllRequestByUser()
        {
            if (_userGroupSettings.HRGroup.FirstOrDefault(t => t == _userSession.UserName) != null)
            {
                var entities = await _travelDeclarationRepository.getAllRequest();
                var model = entities.Select(e => _mapper.Map<TravelDeclaration, TravelDeclarationListOfUser>(e)).OrderBy(t => t.status).ToList();
                return Json(new { data = model });
            }
            else
            {
                var entities = await _travelDeclarationRepository.listAllRequestByUser(_userSession.Id, _userSession.UserName.Split('@')[0]);
                var model = entities.Select(e => _mapper.Map<TravelDeclaration, TravelDeclarationListOfUser>(e)).OrderBy(t => t.status).ToList();
                return Json(new { data = model });
            }
        }
        public async Task<IActionResult> ListAllRequestByCampus(string campusCode)
        {
            var entities = await _travelDeclarationRepository.listAllRequestByCampus(campusCode);
            var model = entities.Select(e => _mapper.Map<TravelDeclaration, TravelDeclarationListOfUser>(e)).ToList();
            return Json(new { data = model });
        }
        #endregion


        #region SendEmail
        public async Task SendEmail(TravelDeclaration travelDetails, int requestStatus, string currentUrl)
        {
            //for requester
            if (requestStatus == (int)(RequestStatus.Submitted))
            {

                List<MailboxAddress> ccEmail = new List<MailboxAddress>();
                if (!string.IsNullOrEmpty(travelDetails.informedEmail))
                {
                    if (travelDetails.informedEmail.IndexOf(",") < 0)
                    {
                        ccEmail.Add(MailboxAddress.Parse(travelDetails.informedEmail));
                    }
                    else
                    {
                        string[] emailSplit = travelDetails.informedEmail.Split(",");
                        foreach (var item in emailSplit)
                        {
                            ccEmail.Add(MailboxAddress.Parse(item));
                        }
                    }
                }
                ccEmail.Add(MailboxAddress.Parse(travelDetails.Requester.UserName + "@vas.edu.vn"));

                if (!string.IsNullOrEmpty(travelDetails.nameOfLineManager) && !string.IsNullOrEmpty(travelDetails.ECSDEmail))
                {
                    var emailModel = await _emailConfigRepository.getTemplatesByRequestStatus((int)(RequestStatus.Submitted));
                    string body = emailModel.forSubmitted.Body;
                    body = TravelDeclarationSendEmail.ReplaceMessageTemplateTokens(travelDetails, currentUrl, body);

                    await _emailSender.SendEmailWithVASCredential(travelDetails.nameOfLineManager, emailModel.forSubmitted.Subject, body, ccEmail);
                }

                if (string.IsNullOrEmpty(travelDetails.nameOfLineManager) && !string.IsNullOrEmpty(travelDetails.ECSDEmail))
                {
                    var emailModel = await _emailConfigRepository.getTemplatesByRequestStatus((int)(RequestStatus.sent_request_to_ECSD));
                    string body = emailModel.forRequestToECSD.Body;
                    body = TravelDeclarationSendEmail.ReplaceMessageTemplateTokens(travelDetails, currentUrl, body);

                    await _emailSender.SendEmailWithVASCredential(travelDetails.ECSDEmail, emailModel.forRequestToECSD.Subject, body, ccEmail);
                }
            }
            //for line manager - approve
            if (requestStatus == (int)(RequestStatus.lineManager_Approved))
            {
                var emailModel = await _emailConfigRepository.getTemplatesByRequestStatus((int)(RequestStatus.lineManager_Approved));
                string body = emailModel.forApproved.Body;

                body = TravelDeclarationSendEmail.ReplaceMessageTemplateTokens(travelDetails, currentUrl, body);
                List<MailboxAddress> ccEmail = new List<MailboxAddress>();
                if (!string.IsNullOrEmpty(travelDetails.informedEmail))
                {
                    if (travelDetails.informedEmail.IndexOf(",") < 0)
                    {
                        ccEmail.Add(MailboxAddress.Parse(travelDetails.informedEmail));
                    }
                    else
                    {
                        string[] emailSplit = travelDetails.informedEmail.Split(",");
                        foreach (var item in emailSplit)
                        {
                            ccEmail.Add(MailboxAddress.Parse(item));
                        }
                    }
                }
                ccEmail.Add(MailboxAddress.Parse(travelDetails.Requester.UserName + "@vas.edu.vn"));
                if (!string.IsNullOrEmpty(travelDetails.ECSDEmail))
                {
                    await _emailSender.SendEmailWithVASCredential(travelDetails.ECSDEmail, emailModel.forApproved.Subject, body, ccEmail);
                }

            }
            //for line manager - reject
            if (requestStatus == (int)(RequestStatus.lineManager_Rejected))
            {
                var emailModel = await _emailConfigRepository.getTemplatesByRequestStatus((int)(RequestStatus.lineManager_Rejected));
                string body = emailModel.forRejected.Body;

                body = TravelDeclarationSendEmail.ReplaceMessageTemplateTokens(travelDetails, currentUrl, body);
                List<MailboxAddress> ccEmail = new List<MailboxAddress>();
                if (!string.IsNullOrEmpty(travelDetails.informedEmail))
                {
                    string[] emailSplit = travelDetails.informedEmail.Split(",");
                    foreach (var item in emailSplit)
                    {
                        ccEmail.Add(MailboxAddress.Parse(item));
                    }
                }
                if (!string.IsNullOrEmpty(travelDetails.nameOfLineManager))
                {
                    ccEmail.Add(MailboxAddress.Parse(travelDetails.nameOfLineManager));
                }

                await _emailSender.SendEmailWithVASCredential(travelDetails.Requester.UserName + "@vas.edu.vn", emailModel.forRejected.Subject, body, ccEmail);
            }

            //for ecsd - approve
            if (requestStatus == (int)(RequestStatus.sent_request_to_ECSD))
            {
                var emailModel = await _emailConfigRepository.getTemplatesByRequestStatus((int)(RequestStatus.sent_request_to_ECSD));
                string body = emailModel.forRequestToECSD.Body;

                body = TravelDeclarationSendEmail.ReplaceMessageTemplateTokens(travelDetails, currentUrl, body);
                List<MailboxAddress> ccEmail = new List<MailboxAddress>();
                if (!string.IsNullOrEmpty(travelDetails.informedEmail))
                {
                    string[] emailSplit = travelDetails.informedEmail.Split(",");
                    foreach (var item in emailSplit)
                    {
                        ccEmail.Add(MailboxAddress.Parse(item));
                    }
                }
                ccEmail.Add(MailboxAddress.Parse(travelDetails.Requester.UserName + "@vas.edu.vn"));

                await _emailSender.SendEmailWithVASCredential(travelDetails.ECSDEmail, emailModel.forRequestToECSD.Subject, body, ccEmail);
            }

            //for ecsd - approve
            if (requestStatus == (int)(RequestStatus.ecsd_Approved))
            {
                var emailModel = await _emailConfigRepository.getTemplatesByRequestStatus((int)(RequestStatus.ecsd_Approved));
                string body = emailModel.forECSDApproved.Body;

                body = TravelDeclarationSendEmail.ReplaceMessageTemplateTokens(travelDetails, currentUrl, body);
                List<MailboxAddress> ccEmail = new List<MailboxAddress>();
                if (!string.IsNullOrEmpty(travelDetails.informedEmail))
                {
                    string[] emailSplit = travelDetails.informedEmail.Split(",");
                    foreach (var item in emailSplit)
                    {
                        ccEmail.Add(MailboxAddress.Parse(item));
                    }
                }

                if (!string.IsNullOrEmpty(travelDetails.nameOfLineManager))
                {
                    ccEmail.Add(MailboxAddress.Parse(travelDetails.nameOfLineManager));
                }
                if (!string.IsNullOrEmpty(travelDetails.ECSDEmail))
                {
                    ccEmail.Add(MailboxAddress.Parse(travelDetails.ECSDEmail));
                }

                await _emailSender.SendEmailWithVASCredential(travelDetails.Requester.UserName + "@vas.edu.vn", emailModel.forECSDApproved.Subject, body, ccEmail);
            }
            if (requestStatus == (int)(RequestStatus.ecsd_Rejected))
            {
                var emailModel = await _emailConfigRepository.getTemplatesByRequestStatus((int)(RequestStatus.ecsd_Rejected));
                string body = emailModel.forECSDRejected.Body;

                body = TravelDeclarationSendEmail.ReplaceMessageTemplateTokens(travelDetails, currentUrl, body);
                List<MailboxAddress> ccEmail = new List<MailboxAddress>();
                if (!string.IsNullOrEmpty(travelDetails.informedEmail))
                {
                    string[] emailSplit = travelDetails.informedEmail.Split(",");
                    foreach (var item in emailSplit)
                    {
                        ccEmail.Add(MailboxAddress.Parse(item));
                    }
                }

                if (!string.IsNullOrEmpty(travelDetails.nameOfLineManager))
                {
                    ccEmail.Add(MailboxAddress.Parse(travelDetails.nameOfLineManager));
                }
                if (!string.IsNullOrEmpty(travelDetails.ECSDEmail))
                {
                    ccEmail.Add(MailboxAddress.Parse(travelDetails.ECSDEmail));
                }

                await _emailSender.SendEmailWithVASCredential(travelDetails.Requester.UserName + "@vas.edu.vn", emailModel.forECSDRejected.Subject, body, ccEmail);
            }

            //cancel email template
            if (requestStatus == (int)(RequestStatus.cancelled))
            {
                try
                {
                    var emailModel = await _emailConfigRepository.getTemplatesByRequestStatus((int)(RequestStatus.cancelled));
                    string body = emailModel.forCancelled.Body;

                    body = TravelDeclarationSendEmail.ReplaceMessageTemplateTokens(travelDetails, currentUrl, body);
                    List<MailboxAddress> ccEmail = new List<MailboxAddress>();
                    if (!string.IsNullOrEmpty(travelDetails.informedEmail))
                    {
                        string[] emailSplit = travelDetails.informedEmail.Split(",");
                        foreach (var item in emailSplit)
                        {
                            ccEmail.Add(MailboxAddress.Parse(item));
                        }
                    }


                    if (!string.IsNullOrEmpty(travelDetails.nameOfLineManager))
                    {
                        ccEmail.Add(MailboxAddress.Parse(travelDetails.nameOfLineManager));
                    }
                    if (!string.IsNullOrEmpty(travelDetails.ECSDEmail))
                    {
                        ccEmail.Add(MailboxAddress.Parse(travelDetails.ECSDEmail));
                    }


                    await _emailSender.SendEmailWithVASCredential(travelDetails.Requester.UserName + "@vas.edu.vn", emailModel.forCancelled.Subject, body, ccEmail);
                }
                catch (Exception ex)
                {

                    throw;
                }

            }

        }

        #endregion

        #region getInfo
        public async Task<IActionResult> getDistrictByProvinceId(string provinceId)
        {
            try
            {
                var result = await _baseCategoryRepository.GetDistrictsByProvinceId(provinceId);
                return Json(result.OrderBy(t => t.name));
            }
            catch (Exception)
            {
                return Json(null);
            }

        }
        public async Task<IActionResult> getWardByDistrictId(string DistrictId)
        {
            try
            {
                var result = await _baseCategoryRepository.GetWardsByDistrictId(DistrictId);
                return Json(result.OrderBy(t => t.name));
            }
            catch (Exception)
            {
                return Json(null);
            }

        }
        private async Task<List<SelectListItem>> GetECSDGroup(List<string> group)
        {
            return (await _userRepository.GetUsersGroupSetting(group)).
                Select(c => new SelectListItem() { Text = string.Format($"{c.UserName}@vas.edu.vn - {c.Position} - {c.Campus}"), Value = string.Format($"{c.UserName}@vas.edu.vn") }).ToList();
        }

        private async Task<List<SelectListItem>> GetLineManagerGroup(List<string> group)
        {
            return (await _userRepository.GetUsersGroupSetting(group)).
                Select(c => new SelectListItem() { Text = string.Format($"{c.UserName}@vas.edu.vn - {c.Position} - {c.Campus}"), Value = string.Format($"{c.UserName}@vas.edu.vn") }).ToList();
        }

        #endregion


        #region actionMultipleRequest
        public async Task<IActionResult> actionMultipleRequest(Guid[] listRequest, int actionStatus)
        {

            try
            {
                if (listRequest.Length <= 0)
                {
                    return Json(new { success = false, message = "ERROR! Please contact to IThelpdesk !" });
                }

                string ids = string.Join(",", listRequest.Select(p => p.ToString()).ToArray());
                List<string> userCode = new List<string>();
                foreach (Guid Id in listRequest)
                {
                    var entity = _travelDeclarationRepository.getTravelDeclarationById(Id);
                    if (entity == null)
                        return RedirectToAction("Index");

                    if (entity.nameOfLineManager != null && entity.nameOfLineManager.Split("@")[0] == _userSession.UserName)
                    {
                        if (actionStatus == (int)RequestStatus.cancelled)
                        {
                            var model = _mapper.Map<TravelDeclaration, TravelDeclarationModel>(entity);
                            return View("Edit", entity).WithError("The request you selected has been cancelled");
                        }

                        else if (entity.status == (int)RequestStatus.lineManager_Approved || actionStatus == (int)RequestStatus.lineManager_Rejected)
                        {
                            return Json(new { success = false, message = "You've taken action before !!" });
                        }

                    }


                    if (entity.ECSDEmail != null && entity.ECSDEmail.Split("@")[0] == _userSession.UserName)
                    {
                        if (actionStatus == (int)RequestStatus.cancelled)
                        {
                            var model = _mapper.Map<TravelDeclaration, TravelDeclarationModel>(entity);
                            return View("Edit", entity).WithError("The request you selected has been cancelled");
                        }
                        else if (entity.ECSDVerifyStatus != null && (entity.ECSDVerifyStatus == RequestStatus.ecsd_Approved || entity.ECSDVerifyStatus == RequestStatus.ecsd_Rejected))
                        {
                            return Json(new { success = false, message = "You've taken action before !!" });
                        }
                    }

                    if (actionStatus == (int)RequestStatus.lineManager_Approved || entity.status == (int)RequestStatus.lineManager_Rejected)
                    {
                        if (entity.nameOfLineManager != null && entity.nameOfLineManager.Split("@")[0] == _userSession.UserName)
                        {
                            entity.status = actionStatus;
                            entity.LatestStatus = actionStatus;
                            entity.dateOfStatus = _dateTime.Now;
                            await _travelDeclarationRepository.updateTravelDeclaration(entity);
                            string host = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
                            _backgroungJobClient.Enqueue(() => SendEmail(entity, actionStatus, host));
                        }
                        else
                        {
                            return Json(new { success = false, message = "You did not have permission to do that !!" });
                        }
                    }

                    if (actionStatus == (int)RequestStatus.ecsd_Approved || entity.status == (int)RequestStatus.ecsd_Rejected)
                    {
                        if (!string.IsNullOrEmpty(entity.nameOfLineManager) && entity.status == (int)RequestStatus.Submitted)
                        {
                            return RedirectToAction("Index").WithError("ERROR!. Please waiting for line manager approve first !!!");
                        }

                        if (entity.ECSDEmail != null && entity.ECSDEmail.Split("@")[0] == _userSession.UserName)
                        {
                            entity.status = actionStatus;
                            entity.LatestStatus = actionStatus;
                            entity.dateOfStatus = _dateTime.Now;
                            entity.ECSDVerifyStatus = (RequestStatus)actionStatus;
                            entity.ECSDCommentDate = _dateTime.Now;
                            await _travelDeclarationRepository.updateTravelDeclaration(entity);
                            string host = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
                            _backgroungJobClient.Enqueue(() => SendEmail(entity, actionStatus, host));
                        }
                        else
                        {
                            return Json(new { success = false, message = "You did not have permission to do that !!" });
                        }
                    }
                }
                return Json(new { success = true, message = "Success !" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "ERROR! Please contact to IThelpdesk !" });
            }

        }

        #endregion


        #region checkPermission 
        public IActionResult checkPermissionForApproveRequest(Guid requestId)
        {
            if (_userSession.UserName != null)
            {
                var requestEntity = _travelDeclarationRepository.getTravelDeclarationById(requestId);
                if (requestEntity != null)
                {
                    if (!string.IsNullOrEmpty(requestEntity.nameOfLineManager))
                    {
                        if (requestEntity.nameOfLineManager.Split('@')[0] == _userSession.UserName && requestEntity.ECSDVerifyStatus == null)
                        {
                            if (requestEntity.status != (int)RequestStatus.Submitted)
                            {
                                return Json(new { success = false, message = "You've taken action before  !" });
                            }
                            return Json(new { success = true, message = "SUCCESS !" });
                        }
                    }
                    if (!string.IsNullOrEmpty(requestEntity.ECSDEmail))
                    {
                        if (requestEntity.ECSDEmail.Split('@')[0] == _userSession.UserName)
                        {
                            if (requestEntity.ECSDVerifyStatus != null)
                            {
                                return Json(new { success = false, message = "You've taken action before  !" });
                            }
                            return Json(new { success = true, message = "SUCCESS !" });
                        }
                        else if (_userGroupSettings.HRGroup.FirstOrDefault(t => t == _userSession.UserName) != null && requestEntity.status == (int)RequestStatus.ecsd_Approved)
                        {
                            return Json(new { success = true, message = "SUCCESS !" });
                        }
                    }
                    else
                    {
                        return Json(new { success = false, message = "You did not have permission to approve this request !" });
                    }

                }
                else
                    return Json(new { success = false, message = "You did not have permission to approve this request !" });
            }

            return Json(new { success = false, message = "You did not have permission to approve this request !" });
        }
        public IActionResult getTravelRouteDetails(Guid id)
        {

            var data = _travelDeclarationRepository.getTravelDeclarationById(id);
            if (data == null)
                return Json(new { succes = false, message = "ERROR! Record not found !" });
            return Json(data.travellingRoutes);
        }

        #endregion


        #region ECSD Action Controller
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ECSDApproval(TravelDeclaration model)
        {
            try
            {
                var entity = _travelDeclarationRepository.getTravelDeclarationById(model.TravelDeclarationId);
                if (entity == null)
                    return RedirectToAction("Index");

                if (entity.CancelStatus == RequestStatus.cancelled)
                {
                    return View("Edit", entity).WithError("The request you selected has been cancelled");
                }

                if (!string.IsNullOrEmpty(entity.nameOfLineManager) && entity.status == (int)RequestStatus.Submitted)
                {
                    return RedirectToAction("Index").WithError("ERROR!. Please waiting for line manager approve first !!!");
                }

                entity.ECSDVerifyStatus = RequestStatus.ecsd_Approved;
                entity.LatestStatus = (int)RequestStatus.ecsd_Approved;
                entity.ECSDCommentDate = _dateTime.Now;
                entity.ECSDComment = model.ECSDComment;
                await _travelDeclarationRepository.updateTravelDeclaration(entity);
                _logger.LogInformation($"User {_userSession.UserName} has been approved with request {model.TravelDeclarationId} success !");
                string host = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
                _backgroungJobClient.Enqueue(() => SendEmail(entity, (int)RequestStatus.ecsd_Approved, host));

                return RedirectToAction("Index").WithSuccess("Thank you for your response!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"{_userSession.UserName} got error {ex} at {_dateTime.Now}");
                _logger.LogCritical(ex.Message, ex);
            }
            return View("Index").WithError("Something went wrong. Please contact ithelpdesk@vas.edu.vn to support");

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ECSDReject(TravelDeclaration model)
        {
            try
            {
                var entity = _travelDeclarationRepository.getTravelDeclarationById(model.TravelDeclarationId);
                if (entity == null)
                {
                    return RedirectToAction("Index");
                }
                if (entity.CancelStatus == RequestStatus.cancelled)
                {
                    return View("Edit", entity).WithError("The request you selected has been cancelled");

                }

                if (!string.IsNullOrEmpty(entity.nameOfLineManager) && entity.status == (int)RequestStatus.Submitted)
                {
                    return RedirectToAction("Index").WithError("ERROR!. Please waiting for line manager approve first !!!");
                }
                entity.ECSDVerifyStatus = RequestStatus.ecsd_Rejected;
                entity.LatestStatus = (int)RequestStatus.ecsd_Rejected;
                entity.ECSDCommentDate = _dateTime.Now;
                entity.ECSDComment = model.ECSDComment;
                await _travelDeclarationRepository.updateTravelDeclaration(entity);
                _logger.LogInformation($"User {_userSession.UserName} has been rejected with request {model.TravelDeclarationId} success !");
                string host = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
                _backgroungJobClient.Enqueue(() => SendEmail(entity, (int)RequestStatus.ecsd_Rejected, host));

                return RedirectToAction("Index").WithSuccess("Thank you for your response!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"{_userSession.UserName} got error {ex} at {_dateTime.Now}");
                _logger.LogCritical(ex.Message, ex);
            }
            return View("Index").WithError("Something went wrong. Please contact ithelpdesk@vas.edu.vn to support");
        }
        #endregion



        //Cancel
        #region Cancel

        [HttpPost]
        public async Task<IActionResult> CancelRequest(TravelDeclaration model)
        {
            try
            {
                var entity = _travelDeclarationRepository.getTravelDeclarationById(model.TravelDeclarationId);
                if (entity == null)
                    return RedirectToAction("Index");

                entity.CancelStatus = RequestStatus.cancelled;
                entity.LatestStatus = (int)RequestStatus.cancelled;
                entity.CancelDate = _dateTime.Now;
                entity.CancelComment = model.CancelComment;
                await _travelDeclarationRepository.updateTravelDeclaration(entity);
                _logger.LogInformation($"User {_userSession.UserName} has been cancelled with request {model.TravelDeclarationId} success !");
                string host = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
                _backgroungJobClient.Enqueue(() => SendEmail(entity, (int)RequestStatus.cancelled, host));
                return RedirectToAction("Index").WithSuccess("Cancelled Successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"{_userSession.UserName} got error {ex} at {_dateTime.Now}");
                _logger.LogCritical(ex.Message, ex);
            }
            return View("Index").WithError("Something went wrong. Please contact ithelpdesk@vas.edu.vn to support");
        }

        #endregion
        //Clone
        #region clone
        [HttpGet]
        public async Task<IActionResult> CloneRequest(Guid id)
        {
            var entity = _travelDeclarationRepository.getTravelDeclarationById(id);
            if (entity == null)
                return RedirectToAction("Index");

            var model = _mapper.Map<TravelDeclaration, TravelDeclarationModel>(entity);
            model.Provinces = new SelectList(await _baseCategoryRepository.GetProvinces(), "provinceId", "name");
            model.Countries = new SelectList(await _baseCategoryRepository.GetCountries(), "countryId", "name");

            return View("Clone", model);

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clone(TravelDeclarationModel model, IFormFile departureTicket, IFormFile returningTicket)
        {
            if (!ModelState.IsValid)
            {
                try
                {
                    if (model.travellingRoutes == null || model.travellingRoutes.Count < 0)
                    {
                        return View(model).WithError("ERROR! TravellingRoutes not null");
                    }
                    var userInfo = await _userRepository.GetUserByUserNameAsync(_userSession.UserName);
                    userInfo.Mobile = model.Requester.Mobile;
                    await _userRepository.UpdateUserAsync(userInfo);
                    var user_mapper = _mapper.Map<User, UserViewModel>(userInfo);
                    user_mapper.Mobile = model.Requester.Mobile;

                    model.Requester = user_mapper;
                    model.createdOn = _dateTime.Now;
                    var travel_declaration = _mapper.Map<TravelDeclarationModel, TravelDeclaration>(model);

                    travel_declaration.TravelDeclarationId = Guid.NewGuid();

                    var allCloneRequest = await _travelDeclarationRepository.getRequestIdById(model.request_id);
                    var cloneNumber = allCloneRequest.Count();
                    if (cloneNumber > 1)
                    {
                        travel_declaration.request_id = string.Format("{0}.{1}", model.request_id, cloneNumber);
                    }
                    else
                    {
                        travel_declaration.request_id = string.Format("{0}.{1}", model.request_id, 1);
                    }

                    travel_declaration.RequesterId = user_mapper.Id;
                    travel_declaration.dateOfStatus = null;
                    travel_declaration.status = (int)RequestStatus.Submitted;
                    travel_declaration.LatestStatus = (int)RequestStatus.Submitted;

                    //Upload file
                    //Departure
                    var uploads = Path.Combine(_hostingEnvironment.WebRootPath, "Files");
                    FileUploadHelper objFile = new FileUploadHelper();

                    if (departureTicket != null)
                    {
                        string strFilePathDeparture = await objFile.SaveFileAsync(departureTicket, uploads, "Departure-");
                        strFilePathDeparture = strFilePathDeparture
                         .Replace(_hostingEnvironment.WebRootPath, string.Empty)
                         .Replace("\\", "/");

                        travel_declaration.departureTicketPath = strFilePathDeparture;
                        travel_declaration.departureTicket = objFile.GetFileName(departureTicket, true, "Departure-");
                    }

                    if (returningTicket != null)
                    {
                        //Returning
                        string strFilePathReturning = await objFile.SaveFileAsync(returningTicket, uploads, "Returning-");
                        strFilePathReturning = strFilePathReturning
                         .Replace(_hostingEnvironment.WebRootPath, string.Empty)
                         .Replace("\\", "/");

                        travel_declaration.returningTicketPath = strFilePathReturning;
                        travel_declaration.returningTicket = objFile.GetFileName(returningTicket, true, "Returning-");
                    }

                    if (travel_declaration.travelFromCountryId == "VN")
                    {
                        travel_declaration.travelFromIntl = null;
                    }

                    if (travel_declaration.travelToCountryId == "VN")
                    {
                        travel_declaration.travelToIntl = null;
                    }

                    foreach (var item in travel_declaration.travellingRoutes)
                    {
                        int count = 0;
                        for (int i = 0; i < item.TravelRouteFullAddress.Length; i++)
                        {

                            if (item.TravelRouteFullAddress[i].ToString().Equals("-"))
                            {
                                count++;
                            }
                        }
                        if (count == 2)
                        {
                            item.TravelRouteProvinceId = null;
                            item.TravelRouteDistrictId = null;
                            item.TravelRouteWardId = null;
                        }
                        if (count == 3)
                        {
                            item.travelRouteCountryId = null;
                            item.travelRouteCity = null;
                        }

                    }

                    await _travelDeclarationRepository.insert(travel_declaration);
                    _logger.LogInformation($"User{_userSession.UserName} clonned request with id {model.request_id} success"); ;
                    string host = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
                    _backgroungJobClient.Enqueue(() => SendEmail(travel_declaration, (int)RequestStatus.Submitted, host));
                    return RedirectToAction("Index").WithSuccess("Submitted success !");

                }
                catch (Exception ex)
                {
                    _logger.LogError($"{_userSession.UserName} got error {ex.Message} at {_dateTime.Now}");
                    _logger.LogCritical(ex.Message, ex);
                    return RedirectToAction("Index").WithError("Something went wrong. Please contact ithdelpdesk@vas.edu.vn. Thank you!!!");
                }
            }
            return RedirectToAction("Index").WithError("Something went wrong. Please contact ithdelpdesk@vas.edu.vn. Thank you !!");
        }


        #endregion  


        #region driverMethod 
        private async Task<string> getProvinceName(string provinceId)
        {
            var entity = await _baseCategoryRepository.GetProvinces();
            if (entity != null)
            {
                var result = entity.Where(t => t.provinceId == provinceId).FirstOrDefault();
                return result != null ? result.name : "";
            }
            return "";
        }


        private async Task<string> getDistrictName(string districtId)
        {
            var entity = await _baseCategoryRepository.GetDistrictsById(districtId);
            if (entity != null)
            {
                return entity.name;
            }
            return "";
        }

        private async Task<string> getWardName(string wardId)
        {
            var entity = await _baseCategoryRepository.GetWardsById(wardId);
            if (entity != null)
            {
                return entity.name;
            }
            return "";
        }
        private async Task<string> getCountryName(string countryId)
        {
            var entity = await _baseCategoryRepository.GetCountries();
            if (entity != null)
            {
                var result = entity.Where(t => t.countryId == countryId).FirstOrDefault();
                return result != null ? result.name : "";
            }
            return "";
        }

        #endregion


        #region Export Filtered Excel


        public async Task<IActionResult> ExportExcel(int request_status)
        {
            
            IList<TravelDeclaration> listAllRequest;
            try
            {
                if (_userGroupSettings.HRGroup.FirstOrDefault(t => t == _userSession.UserName) != null)
                {
                    listAllRequest = await _travelDeclarationRepository.getAllRequest();
                    if (request_status != 0)
                    {
                        listAllRequest = listAllRequest.Where(t => t.status == request_status || t.ECSDVerifyStatus == (RequestStatus)request_status).ToList();
                    }

                }
                else
                {
                    listAllRequest = await _travelDeclarationRepository.listAllRequestByUser(_userSession.Id, _userSession.UserName.Split('@')[0]);
                    if (request_status != 0)
                    {
                        listAllRequest = listAllRequest.Where(t => t.status == request_status || t.ECSDVerifyStatus == (RequestStatus)request_status).ToList();
                    }
                }

                var stream = new MemoryStream();

                using (var package = new ExcelPackage(stream))
                {

                    var workSheet = package.Workbook.Worksheets.Add("Travel Declaration");


                    #region template excel
                    //// Header
                    workSheet.Cells[1, 1].Value = "TRAVEL DECLARATION REPORT";
                    workSheet.Cells[1, 1, 2, 20].Merge = true; //Merge columns start and end range
                    workSheet.Cells[1, 1, 2, 20].Style.Font.Bold = true; //Font should be bold
                    workSheet.Cells[1, 1, 2, 20].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[1, 1, 2, 20].Style.Font.Size = 16;
                    workSheet.Cells[1, 1, 2, 20].Style.Font.Color.SetColor(Color.Red);


                    //Summary
                    workSheet.Cells[3, 1].Value = "THÔNG TIN CÁ NHÂN KÊ KHAI / GENERAL INFORMATION ";
                    workSheet.Cells[3, 1, 3, 16].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 1, 3, 16].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 1, 3, 16].Style.Font.Size = 16;
                    workSheet.Cells[3, 1, 3, 16].Style.Font.Bold = true;
                    workSheet.Cells[3, 1, 3, 16].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 1, 3, 16].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[3, 1, 3, 16].Style.Fill.BackgroundColor.SetColor(Color.DeepSkyBlue);

                    //Details   
                    workSheet.Cells[3, 17].Value = "CHI TIẾT / DETAIL";
                    workSheet.Cells[3, 17, 3, 30].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 17, 3, 30].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 17, 3, 30].Style.Font.Bold = true;
                    workSheet.Cells[3, 17, 3, 30].Style.Font.Size = 16;
                    workSheet.Cells[3, 17, 3, 30].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 17, 3, 30].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[3, 17, 3, 30].Style.Fill.BackgroundColor.SetColor(Color.DeepSkyBlue);

                    //No
                    workSheet.Cells[4, 1].Value = "No";
                    workSheet.Cells[4, 1, 7, 1].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 1, 7, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 1, 7, 1].Style.Font.Size = 11;
                    workSheet.Cells[4, 1, 7, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 1, 7, 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(1).Width = 5;
                    workSheet.Column(1).Style.WrapText = true;

                    //RequestID
                    workSheet.Cells[4, 2].Value = "Request ID";
                    workSheet.Cells[4, 2, 7, 2].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 2, 7, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 2, 7, 2].Style.Font.Size = 11;
                    workSheet.Cells[4, 2, 7, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 2, 7, 2].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(2).Width = 10;
                    workSheet.Column(2).Style.WrapText = true;

                    //status
                    workSheet.Cells[4, 3].Value = "Trạng thái / Status";
                    workSheet.Cells[4, 3, 7, 3].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 3, 7, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 3, 7, 3].Style.Font.Size = 11;
                    workSheet.Cells[4, 3, 7, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 3, 7, 3].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(3).Width = 10;
                    workSheet.Column(3).Style.WrapText = true;

                    //Submit status date 
                    workSheet.Cells[4, 4].Value = "Ngày gửi / Submitted Date";
                    workSheet.Cells[4, 4, 7, 4].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 4, 7, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 4, 7, 4].Style.Font.Size = 11;
                    workSheet.Cells[4, 4, 7, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 4, 7, 4].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(4).Width = 15;
                    workSheet.Column(4).Style.WrapText = true;

                    //Line Manager status date
                    workSheet.Cells[4, 5].Value = "Ngày xác nhận / Verified or Rejected Date";
                    workSheet.Cells[4, 5, 7, 5].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 5, 7, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 5, 7, 5].Style.Font.Size = 11;
                    workSheet.Cells[4, 5, 7, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 5, 7, 5].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(5).Width = 15;
                    workSheet.Column(5).Style.WrapText = true;

                    //ECSD status date
                    workSheet.Cells[4, 6].Value = "Ngày duyệt / Approval or Rejected Date";
                    workSheet.Cells[4, 6, 7, 6].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 6, 7, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 6, 7, 6].Style.Font.Size = 11;
                    workSheet.Cells[4, 6, 7, 6].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 6, 7, 6].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(6).Width = 15;
                    workSheet.Column(6).Style.WrapText = true;

                    //Cancel status
                    workSheet.Cells[4, 7].Value = "Ngày hủy / Cancelled Date";
                    workSheet.Cells[4, 7, 7, 7].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 7, 7, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 7, 7, 7].Style.Font.Size = 11;
                    workSheet.Cells[4, 7, 7, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 7, 7, 7].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(7).Width = 15;
                    workSheet.Column(7).Style.WrapText = true;

                    //CAM/MOET
                    workSheet.Cells[4, 8].Value = "MOET/CAM";
                    workSheet.Cells[4, 8, 7, 8].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 8, 7, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 8, 7, 8].Style.Font.Size = 11;
                    workSheet.Cells[4, 8, 7, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 8, 7, 8].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(8).Width = 10;
                    workSheet.Column(8).Style.WrapText = true;


                    //Type of list
                    workSheet.Cells[4, 9].Value = "Nhóm / Type of list";
                    workSheet.Cells[4, 9, 7, 9].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 9, 7, 9].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 9, 7, 9].Style.Font.Size = 11;
                    workSheet.Cells[4, 9, 7, 9].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 9, 7, 9].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(9).Width = 10;
                    workSheet.Column(9).Style.WrapText = true;


                    //Employee ID
                    workSheet.Cells[4, 10].Value = "Mã Nhân viên / EME code";
                    workSheet.Cells[4, 10, 7, 10].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 10, 7, 10].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 10, 7, 10].Style.Font.Size = 11;
                    workSheet.Cells[4, 10, 7, 10].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 10, 7, 10].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(10).Width = 10;
                    workSheet.Column(10).Style.WrapText = true;


                    //Patient/Suspect Name
                    workSheet.Cells[4, 11].Value = "Họ Tên / Full Name";
                    workSheet.Cells[4, 11, 7, 11].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 11, 7, 11].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 11, 7, 11].Style.Font.Size = 11;
                    workSheet.Cells[4, 11, 7, 11].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 11, 7, 11].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(11).Width = 10;
                    workSheet.Column(11).Style.WrapText = true;


                    //Position header
                    workSheet.Cells[4, 12].Value = "Chức Danh / Position";
                    workSheet.Cells[4, 12, 5, 13].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 12, 5, 13].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 12, 5, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 12, 5, 13].Style.Font.Size = 11;
                    workSheet.Cells[4, 12, 5, 13].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 12, 5, 13].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(13).Width = 15;
                    workSheet.Column(13).Style.WrapText = true;


                    //Position code
                    workSheet.Cells[6, 12].Value = "Mã Chức danh / Position code";
                    workSheet.Cells[6, 12, 7, 12].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 12, 7, 12].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 12, 7, 12].Style.Font.Size = 11;
                    workSheet.Cells[6, 12, 7, 12].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 12, 7, 12].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);



                    //Title
                    workSheet.Cells[6, 13].Value = "Chức danh / Title";
                    workSheet.Cells[6, 13, 7, 13].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 13, 7, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 13, 7, 13].Style.Font.Size = 11;
                    workSheet.Cells[6, 13, 7, 13].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 13, 7, 13].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);

                    //Dept./Campuses
                    workSheet.Cells[4, 14].Value = "Cơ sở - PB / Campuses - Dept.";
                    workSheet.Cells[4, 14, 7, 14].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 14, 7, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 14, 7, 14].Style.Font.Size = 11;
                    workSheet.Cells[4, 14, 7, 14].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 14, 7, 14].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(14).Width = 15;
                    workSheet.Column(14).Style.WrapText = true;


                    //Current Address/ Địa chỉ hiện tại
                    workSheet.Cells[4, 15].Value = "Địa chỉ hiện tại / Current Address";
                    workSheet.Cells[4, 15, 7, 15].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 15, 7, 15].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 15, 7, 15].Style.Font.Size = 11;
                    workSheet.Cells[4, 15, 7, 15].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 15, 7, 15].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(15).Width = 20;


                    //Số ĐTDĐ / Phone number
                    workSheet.Cells[4, 16].Value = "Số ĐTDĐ / Phone number";
                    workSheet.Cells[4, 16, 7, 16].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 16, 7, 16].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 16, 7, 16].Style.Font.Size = 11;
                    workSheet.Cells[4, 16, 7, 16].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 16, 7, 16].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(16).Width = 15;



                    //2.

                    //KHAI BÁO DU LỊCH / TRAVEL DECLARATION
                    workSheet.Cells[4, 17].Value = "KHAI BÁO DU LỊCH / TRAVEL DECLARATION";
                    workSheet.Cells[4, 17, 4, 30].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 17, 4, 30].Style.Font.Bold = true;
                    workSheet.Cells[4, 17, 4, 30].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 17, 4, 30].Style.Font.Size = 11;
                    workSheet.Cells[4, 17, 4, 30].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 17, 4, 30].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[4, 17, 4, 30].Style.Fill.BackgroundColor.SetColor(Color.SkyBlue);
                    workSheet.Column(17).Width = 20;


                    // THỜI GIAN VÀ ĐỊA ĐIỂM DU LỊCH /TRAVEL TIME & LOCATION 
                    workSheet.Cells[5, 17].Value = "THỜI GIAN VÀ ĐỊA ĐIỂM DU LỊCH / TRAVEL TIME & LOCATION";
                    workSheet.Cells[5, 17, 5, 24].Merge = true; //Merge columns start and end range
                    workSheet.Cells[5, 17, 5, 24].Style.Font.Bold = true;
                    workSheet.Cells[5, 17, 5, 24].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[5, 17, 5, 24].Style.Font.Size = 11;
                    workSheet.Cells[5, 17, 5, 24].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[5, 17, 5, 24].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[5, 17, 5, 24].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);

                    // THỜI GIAN /TRAVEL TIME
                    workSheet.Cells[5, 25].Value = "LỘ TRÌNH DI CHUYỂN / TRAVELLING ROUTES";
                    workSheet.Cells[5, 25, 5, 29].Merge = true; //Merge columns start and end range
                    workSheet.Cells[5, 25, 5, 29].Style.Font.Bold = true;
                    workSheet.Cells[5, 25, 5, 29].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[5, 25, 5, 29].Style.Font.Size = 11;
                    workSheet.Cells[5, 25, 5, 29].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[5, 25, 5, 29].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[5, 25, 5, 29].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);


                    // Từ /From 
                    workSheet.Cells[6, 17].Value = "Từ /From";
                    workSheet.Cells[6, 17, 7, 17].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 17, 7, 17].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 17, 7, 17].Style.Font.Size = 11;
                    workSheet.Cells[6, 17, 7, 17].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 17, 7, 17].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(17).Width = 20;


                    // Đến / To
                    workSheet.Cells[6, 18].Value = "Đến / To";
                    workSheet.Cells[6, 18, 7, 18].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 18, 7, 18].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 18, 7, 18].Style.Font.Size = 11;
                    workSheet.Cells[6, 18, 7, 18].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 18, 7, 18].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(18).Width = 20;

                    // Ngày Đi/Departure date - headeer
                    workSheet.Cells[6, 19].Value = "Ngày Đi/Departure date";
                    workSheet.Cells[6, 19, 6, 21].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 19, 6, 21].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 19, 6, 21].Style.Font.Size = 11;
                    workSheet.Cells[6, 19, 6, 21].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 19, 6, 21].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(21).Width = 15;

                    // Ngày về/Returning date - Header
                    workSheet.Cells[6, 22].Value = "Ngày về/Returning date";
                    workSheet.Cells[6, 22, 6, 24].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 22, 6, 24].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 22, 6, 24].Style.Font.Size = 11;
                    workSheet.Cells[6, 22, 6, 24].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 22, 6, 24].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(22).Width = 15;

                    // Ngày Đi/Departure date - child 
                    workSheet.Cells[7, 19].Value = "Ngày đi/Departure date";
                    workSheet.Cells[7, 19].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[7, 19].Style.Font.Size = 11;
                    workSheet.Cells[7, 19].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[7, 19].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(19).Width = 15;


                    // Phương tiện/Means of transportation
                    workSheet.Cells[7, 20].Value = "Phương tiện/Means of transportation";
                    workSheet.Cells[7, 20].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[7, 20].Style.Font.Size = 11;
                    workSheet.Cells[7, 20].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[7, 20].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(20).Width = 20;


                    // Đính kèm vé/Enclosed ticket
                    workSheet.Cells[7, 21].Value = "Đính kèm vé/Enclosed ticket";
                    workSheet.Cells[7, 21].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[7, 21].Style.Font.Size = 11;
                    workSheet.Cells[7, 21].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[7, 21].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(21).Width = 5;


                    // Ngày về/Returning date
                    workSheet.Cells[7, 22].Value = "Ngày về/Returning date";
                    workSheet.Cells[7, 22].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[7, 22].Style.Font.Size = 11;
                    workSheet.Cells[7, 22].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[7, 22].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(22).Width = 15;


                    // Phương tiện/Means of transportation
                    workSheet.Cells[7, 23].Value = "Phương tiện/Means of transportation";
                    workSheet.Cells[7, 23].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[7, 23].Style.Font.Size = 11;
                    workSheet.Cells[7, 23].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[7, 23].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(23).Width = 20;

                    // Đính kèm vé/Enclosed ticket
                    workSheet.Cells[7, 24].Value = "Đính kèm vé/Enclosed ticket";
                    workSheet.Cells[7, 24].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[7, 24].Style.Font.Size = 11;
                    workSheet.Cells[7, 24].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[7, 24].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(24).Width = 5;

                    // Từ (Ngày & giờ) / From (Date & Time)
                    workSheet.Cells[6, 25].Value = "Từ (Ngày & giờ) / From (Date & Time)";
                    workSheet.Cells[6, 25, 7, 25].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 25, 7, 25].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 25, 7, 25].Style.Font.Size = 11;
                    workSheet.Cells[6, 25, 7, 25].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 25, 7, 25].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(25).Width = 15;


                    //Đến (Ngày & giờ) / To(Date & Time)
                    workSheet.Cells[6, 26].Value = "Đến (Ngày & giờ) / To(Date & Time)";
                    workSheet.Cells[6, 26, 7, 26].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 26, 7, 26].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 26, 7, 26].Style.Font.Size = 11;
                    workSheet.Cells[6, 26, 7, 26].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 26, 7, 26].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(26).Width = 15;


                    // Địa điểm / location
                    workSheet.Cells[6, 27].Value = "Địa điểm / Location";
                    workSheet.Cells[6, 27, 7, 27].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 27, 7, 27].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 27, 7, 27].Style.Font.Size = 11;
                    workSheet.Cells[6, 27, 7, 27].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 27, 7, 27].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(27).Width = 20;


                    // Phương tiện / Means of transportation
                    workSheet.Cells[6, 28].Value = "Phương tiện / Means of transportation";
                    workSheet.Cells[6, 28, 7, 28].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 28, 7, 28].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 28, 7, 28].Style.Font.Size = 11;
                    workSheet.Cells[6, 28, 7, 28].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 28, 7, 28].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(28).Width = 20;

                    //Note
                    workSheet.Cells[6, 29].Value = "Ghi chú / Notes";
                    workSheet.Cells[6, 29, 7, 29].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 29, 7, 29].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 29, 7, 29].Style.Font.Size = 11;
                    workSheet.Cells[6, 29, 7, 29].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 29, 7, 29].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Cells[6, 29, 7, 29].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Cells[6, 29, 7, 29].AutoFitColumns();
                    workSheet.Column(29).Width = 20;

                    //Back To Work Date
                    workSheet.Cells[5, 30].Value = "Ngày trở lại làm việc / Back to work date";
                    workSheet.Cells[5, 30, 7, 30].Merge = true; //Merge columns start and end range
                    workSheet.Cells[5, 30, 7, 30].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[5, 30, 7, 30].Style.Font.Size = 11;
                    workSheet.Cells[5, 30, 7, 30].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[5, 30, 7, 30].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Cells[5, 29, 7, 30].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Cells[6, 30, 7, 30].AutoFitColumns();
                    workSheet.Column(30).Width = 15;

                    #endregion

                    int rowData = 8;
                    var orderNumber = 1;

                    foreach (var item in listAllRequest.OrderBy(t => t.departureDate))
                    {
                        var listTravelRoutes = item.travellingRoutes.Count();

                        workSheet.Cells[rowData, 1, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 1].Value = orderNumber;
                        workSheet.Cells[rowData, 1, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 1].Merge = true;

                        workSheet.Cells[rowData, 2, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 2].Value = item.request_id;
                        workSheet.Cells[rowData, 2, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 2].Merge = true;
                        workSheet.Cells[rowData, 2, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 2, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        if (item.CancelStatus != null)
                        {
                            workSheet.Cells[rowData, 3, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 3].Value = EnumExtensions.GetDisplayName((RequestStatus)item.CancelStatus);
                        }
                        else if (item.ECSDVerifyStatus != null)
                        {
                            workSheet.Cells[rowData, 3, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 3].Value = EnumExtensions.GetDisplayName((RequestStatus)item.ECSDVerifyStatus);
                        }
                        else
                        {
                            workSheet.Cells[rowData, 3, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 3].Value = EnumExtensions.GetDisplayName((RequestStatus)item.status);
                        }
                        workSheet.Cells[rowData, 3, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 3].Merge = true;
                        workSheet.Cells[rowData, 3, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 3, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 4, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 4].Value = item.createdOn != null ? item.createdOn.Value.ToString("dd/MM/yyyy") : "";
                        workSheet.Cells[rowData, 4, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 4].Merge = true;
                        workSheet.Cells[rowData, 4, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 4, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 5, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 5].Value = item.dateOfStatus != null ? item.dateOfStatus.Value.ToString("dd/MM/yyyy") : "";
                        workSheet.Cells[rowData, 5, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 5].Merge = true;
                        workSheet.Cells[rowData, 5, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 5, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 6, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 6].Value = item.ECSDCommentDate != null ? item.ECSDCommentDate.Value.ToString("dd/MM/yyyy") : "";
                        workSheet.Cells[rowData, 6, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 6].Merge = true;
                        workSheet.Cells[rowData, 6, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 6, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 7, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 7].Value = item.CancelDate != null ? item.CancelDate.Value.ToString("dd/MM/yyyy") : "";
                        workSheet.Cells[rowData, 7, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 7].Merge = true;
                        workSheet.Cells[rowData, 7, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 7, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 8, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 8].Value = item.Requester.UserCode.Substring(0, 3).Contains("FEM") || item.Requester.UserCode.Substring(0, 3).Contains("VEM") ? "CAM" : "MOET";
                        workSheet.Cells[rowData, 8, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 8].Merge = true;
                        workSheet.Cells[rowData, 8, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 8, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 9, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 9].Merge = true;

                        workSheet.Cells[rowData, 10, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 10].Value = item.Requester.UserCode;
                        workSheet.Cells[rowData, 10, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 10].Merge = true;
                        workSheet.Cells[rowData, 10, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 10, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 10].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 11, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 11].Value = item.Requester.FirstName + " " + item.Requester.LastName;
                        workSheet.Cells[rowData, 11, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 11].Merge = true;
                        workSheet.Cells[rowData, 11, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 11].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 11, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 11].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 12, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 12].Merge = true;

                        workSheet.Cells[rowData, 13, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 13].Merge = true;
                        workSheet.Cells[rowData, 13, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 13].Value = item.Requester.Position;
                        workSheet.Cells[rowData, 13, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 13, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 13].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 14, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 14].Value = item.Requester.Campus;
                        workSheet.Cells[rowData, 14, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 14].Merge = true;
                        workSheet.Cells[rowData, 14, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 14].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 14, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 15, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 15].Value = item.travelHomeAddress + " " + "-" + " " + await getWardName(item.travelWardId) + " " + "-" + " " + await getDistrictName(item.travelDistrictId) + " " + "-" + " " + await getProvinceName(item.travelProvinceId);
                        workSheet.Cells[rowData, 15, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 15].Merge = true;
                        workSheet.Cells[rowData, 15, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 15].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 15, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 15].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 16, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 16].Value = item.Requester.Mobile;
                        workSheet.Cells[rowData, 16, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 16].Merge = true;
                        workSheet.Cells[rowData, 16, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 16].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 16, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 16].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        var fromPlace = "";
                        var toPlace = "";
                        if (item.travelFromCountryId != null)
                        {
                            if (item.travelFromCountryId == "VN")
                            {
                                fromPlace = await getProvinceName(item.travelFrom) + "-" + await getCountryName(item.travelFromCountryId);
                            }
                            else
                            {
                                fromPlace = item.travelFromIntl + "-" + await getCountryName(item.travelFromCountryId);
                            }
                        }
                        else
                        {
                            fromPlace = "";
                        }


                        if (item.travelToCountryId != null)
                        {
                            if (item.travelToCountryId == "VN")
                            {
                                toPlace = await getProvinceName(item.travelTo) + "-" + await getCountryName(item.travelToCountryId);
                            }
                            else
                            {
                                toPlace = item.travelToIntl + "-" + await getCountryName(item.travelToCountryId);
                            }
                        }
                        else
                        {
                            toPlace = "";
                        }

                        workSheet.Cells[rowData, 17, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 17].Value = fromPlace;
                        workSheet.Cells[rowData, 17, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 17].Merge = true;
                        workSheet.Cells[rowData, 17, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 17].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 17, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 17].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 18, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 18].Value = toPlace;
                        workSheet.Cells[rowData, 18, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 18].Merge = true;
                        workSheet.Cells[rowData, 18, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 18].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 18, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 18].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 19, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 19].Value = item.departureDate != null ? item.departureDate.ToString("dd/MM/yyyy") : "";
                        workSheet.Cells[rowData, 19, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 19].Merge = true;
                        workSheet.Cells[rowData, 19, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 19].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 19, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 19].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 20].Value = item.departureTransportation;
                        workSheet.Cells[rowData, 20, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 20].Merge = true;
                        workSheet.Cells[rowData, 20, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 20].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 20, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 20].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 21, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 21].Value = item.departureTicket != null ? "X" : "";
                        workSheet.Cells[rowData, 21, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 21].Merge = true;
                        workSheet.Cells[rowData, 21, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 21].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 21, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 21].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 22, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 22].Value = item.returningDate != null ? item.returningDate.ToString("dd/MM/yyyy") : "";
                        workSheet.Cells[rowData, 22, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 22].Merge = true;
                        workSheet.Cells[rowData, 22, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 22].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 22, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 22].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 23, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 23].Value = item.returningTransportaion;
                        workSheet.Cells[rowData, 23, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 23].Merge = true;
                        workSheet.Cells[rowData, 23, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 23].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 23, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 23].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 24, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 24].Value = item.returningTicket != null ? "X" : "";
                        workSheet.Cells[rowData, 24, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 24].Merge = true;
                        workSheet.Cells[rowData, 24, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 24].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 24, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 24].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 30, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 30].Value = item.backToWorkDate != null ? item.backToWorkDate.ToString("dd/MM/yyyy") : "";
                        workSheet.Cells[rowData, 30, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 30].Merge = true;
                        workSheet.Cells[rowData, 30, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 30].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 30, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 30].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        foreach (var itemRoute in item.travellingRoutes)
                        {
                            var lastItem = item.travellingRoutes.LastOrDefault().TravellingRouteId;
                            workSheet.Cells[rowData, 25].Value = itemRoute.dateTravel != null ? itemRoute.dateTravel.ToString("dd/MM/yyyy") : "";
                            workSheet.Cells[rowData, 26].Value = itemRoute.toDateTravel != null ? itemRoute.toDateTravel.Value.ToString("dd/MM/yyyy") : "";
                            workSheet.Cells[rowData, 27].Value = itemRoute.TravelRouteFullAddress;
                            workSheet.Cells[rowData, 28].Value = itemRoute.transportation;
                            workSheet.Cells[rowData, 29].Value = itemRoute.travelRoutesNotes;
                            rowData++;
                        }

                        orderNumber++;
                    }

                    workSheet.Cells[workSheet.Dimension.Address].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    workSheet.Cells[workSheet.Dimension.Address].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    workSheet.Cells[workSheet.Dimension.Address].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    workSheet.Cells[workSheet.Dimension.Address].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                    package.Save();
                }
                stream.Position = 0;

                string excelName = $"TravelDeclaration-Report-{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.xlsx";
                return File(stream, "application/octet-stream", excelName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{_userSession.UserName} got error {ex} at {_dateTime.Now}");
                _logger.LogCritical(ex.Message, ex);
                return View("Commmon", "Error");
            }

            #endregion
        }
    }
}