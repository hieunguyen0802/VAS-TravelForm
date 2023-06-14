using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using src.Core;
using src.Core.Domains;
using src.Core.Enums;
using src.Emails;
using src.Repositories.Category;
using src.Repositories.Configs;
using src.Repositories.IncidentReports;
using src.Repositories.RedZone;
using src.Repositories.TravelDeclarations;
using src.Repositories.Users;
using src.Web.Common;
using src.Web.Common.Models.IncidentReportViewModel;
using src.Web.Common.Models.UserViewModels;
using src.Web.Common.Mvc.Alerts;
using src.Web.Extensions;
using src.Web.Helpers;

namespace src.Web.Controllers
{
    [Authorize]
    public class CovidIncidentController : Controller
    {
        private readonly IIncidentReportRepository _incidentReportRepository;
        private readonly ITravelDeclarationRepository _travelDeclarationRepository;
        private readonly IBaseCategoryRepository _baseCategoryRepository;
        private readonly IUserSession _userSession;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly userGroupSettings _userGroupSettings;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IDateTime _dateTime;
        private IAuthorizationService _authorizationService;
        private readonly IBackgroundJobClient _backgroungJobClient;
        private IEmailSender _emailSender;
        private IEmailConfigRepository _emailConfigRepository;
        private readonly ILogger<CovidIncidentController> _logger;
        private readonly IRedZoneRepo _redZoneRepo;

        public CovidIncidentController(
            IBaseCategoryRepository baseCategoryRepository,
            IUserSession userSession,
            IUserRepository userRepository,
            IMapper mapper,
            IIncidentReportRepository incidentReportRepository,
            IOptions<userGroupSettings> userGroupSettings,
            IHostingEnvironment hostingEnvironment,
            IAuthorizationService authorizationService,
            IDateTime dateTime,
            IEmailConfigRepository emailConfigRepository,
            IBackgroundJobClient backgroungJobClient,
            ILogger<CovidIncidentController> logger,
            IEmailSender emailSender,
            ITravelDeclarationRepository travelDeclarationRepository,
            IRedZoneRepo redZoneRepo

            )
        {
            _baseCategoryRepository = baseCategoryRepository;
            _userSession = userSession;
            _userRepository = userRepository;
            _mapper = mapper;
            _userGroupSettings = userGroupSettings.Value;
            _incidentReportRepository = incidentReportRepository;
            _hostingEnvironment = hostingEnvironment;
            _dateTime = dateTime;
            _authorizationService = authorizationService;
            _backgroungJobClient = backgroungJobClient;
            _emailSender = emailSender;
            _emailConfigRepository = emailConfigRepository;
            _logger = logger;
            _travelDeclarationRepository = travelDeclarationRepository;
            _redZoneRepo = redZoneRepo;

        }

        public async Task<IActionResult> Index()
        {
            /*try
            {*/
                if (_userGroupSettings.HRGroup.FirstOrDefault(t => t == _userSession.UserName) != null)
                {
                    var entities = await _incidentReportRepository.getAllRequest();
                    var model = entities.Select(e => _mapper.Map<IncidentReport, IncidentReportListDto>(e)).OrderBy(t => t.status).ToList();
                    return View(model);
                }
                else
                {
                    var entities = await _incidentReportRepository.listAllRequestByUser(_userSession.Id, _userSession.UserName.Split('@')[0]);
                    var model = entities.Select(e => _mapper.Map<IncidentReport, IncidentReportListDto>(e)).OrderBy(t => t.status).ToList();
                    return View(model);
                }
           /* }
            catch (Exception ex)
            {
                _logger.LogError($"{_userSession.UserName} got error {ex} at {_dateTime.Now}");
                _logger.LogCritical(ex.Message, ex);
                return View("Commmon", "Error");
            }*/

        }
        [HttpGet]
        public async Task<IActionResult> Create(IncidentReportViewModel model)
        {
            try
            {
                model.Provinces = new SelectList(await _baseCategoryRepository.GetProvinces(), "provinceId", "name");
                model.ECSDEmailList = await GetECSDGroup(_userGroupSettings.ECSDGroup);
                model.lineManagerGroup = await GetLineManagerGroup(_userGroupSettings.LineManagerGroup);
                var requester = await _userRepository.GetUserByUserNameAsync(_userSession.UserName.Trim());
                var user = _mapper.Map<User, UserViewModel>(requester);
                model.Requester = user;
                var arrayInformed = new[] { requester.informed1, requester.informed2, requester.informed3, requester.informed4, requester.informed5, requester.informed6 };
                string fullInformed = string.Join(",", arrayInformed.Where(s => !string.IsNullOrEmpty(s)));
                model.informedEmail = fullInformed;
                return View(model);
            }
            catch (Exception ex)
            {

                _logger.LogError($"{_userSession.UserName} got error {ex} at {_dateTime.Now}");
                _logger.LogCritical(ex.Message, ex);
                return View("Commmon", "Error");
            }

        }

        [HttpGet]
        public async Task<IActionResult> CreateByRedZone(Guid travelId, Guid redZoneId)
        {
            try
            {
                IncidentReportViewModel model = new IncidentReportViewModel();
                var travel = _travelDeclarationRepository.getTravelDeclarationById(travelId);
                model.travelId = travelId;
                model.travelRequestId = travel.request_id;
                model.redZoneId = redZoneId;
                model.Provinces = new SelectList(await _baseCategoryRepository.GetProvinces(), "provinceId", "name");
                model.ECSDEmailList = await GetECSDGroup(_userGroupSettings.ECSDGroup);
                model.lineManagerGroup = await GetLineManagerGroup(_userGroupSettings.LineManagerGroup);
                var requester = await _userRepository.GetUserByUserNameAsync(_userSession.UserName.Trim());
                var user = _mapper.Map<User, UserViewModel>(requester);
                model.Requester = user;
                var arrayInformed = new[] { requester.informed1, requester.informed2, requester.informed3, requester.informed4, requester.informed5, requester.informed6 };
                string fullInformed = string.Join(",", arrayInformed.Where(s => !string.IsNullOrEmpty(s)));
                model.informedEmail = fullInformed;
                return View("Create", model);

            }
            catch (Exception ex)
            {

                _logger.LogError($"{_userSession.UserName} got error {ex} at {_dateTime.Now}");
                _logger.LogCritical(ex.Message, ex);
                throw ex;
                
            }

        }


        [HttpPost]
        public async Task<IActionResult> SaveRequest(IncidentReportViewModel model, IFormFile departureTicket, IFormFile returningTicket, IFormFile testResultName)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    model.Provinces = new SelectList(await _baseCategoryRepository.GetProvinces(), "provinceId", "name");
                    var userInfo = await _userRepository.GetUserByUserNameAsync(_userSession.UserName);
                    

                    userInfo.Mobile = model.Requester.Mobile;
                    await _userRepository.UpdateUserAsync(userInfo);
                    var user_mapper = _mapper.Map<User, UserViewModel>(userInfo);
                    user_mapper.Mobile = model.Requester.Mobile;

                    var incident_report = _mapper.Map<IncidentReportViewModel, IncidentReport>(model);
                    incident_report.IncidentReportId = Guid.NewGuid();
                    incident_report.travelId = model.travelId;
                    incident_report.redZoneId = model.redZoneId;

                    var all_request_campus = await _incidentReportRepository.getAllRequest();
                    var number_of_requst = all_request_campus.Count() + 1;

                    incident_report.request_id = string.Format("{0} - {1}", "CIR", number_of_requst);
                    incident_report.RequesterId = user_mapper.Id;
                    incident_report.status = (int)RequestStatus.Submitted;
                    incident_report.createdOn = _dateTime.Now;
                    incident_report.campusTemp = user_mapper.Campus;
                    incident_report.positionTemp = user_mapper.Position;

                    if (model.travelId != Guid.Empty)
                    {
                        var travel = _travelDeclarationRepository.getTravelDeclarationById(model.travelId);
                        travel.incidentId = incident_report.IncidentReportId;
                        await _travelDeclarationRepository.updateTravelDeclaration(travel);
                    }


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

                        incident_report.departureTicketPath = strFilePathDeparture;
                        incident_report.departureTicket = objFile.GetFileName(departureTicket, true, "Departure-");
                    }

                    if (returningTicket != null)
                    {
                        //Returning
                        string strFilePathReturning = await objFile.SaveFileAsync(returningTicket, uploads, "Returning-");
                        strFilePathReturning = strFilePathReturning
                         .Replace(_hostingEnvironment.WebRootPath, string.Empty)
                         .Replace("\\", "/");

                        incident_report.returningTicketPath = strFilePathReturning;
                        incident_report.returningTicket = objFile.GetFileName(returningTicket, true, "Returning-");
                    }
                    if (testResultName != null)
                    {
                        string strFilePathTestResult = await objFile.SaveFileAsync(testResultName, uploads, "TestResult-");
                        strFilePathTestResult = strFilePathTestResult
                         .Replace(_hostingEnvironment.WebRootPath, string.Empty)
                         .Replace("\\", "/");

                        incident_report.testResultPath = strFilePathTestResult;
                        incident_report.testResultName = objFile.GetFileName(departureTicket, true, "TestResult-");
                    }

                    if (incident_report.backToWorkStatus == workSatus.backToWorkAlready)
                    {
                        incident_report.estimatedDateBackToWork = null;
                    }
                    if (incident_report.backToWorkStatus == workSatus.notYetBackToWork)
                    {
                        incident_report.dateBackToWork = null;
                    }

                    await _incidentReportRepository.insert(incident_report);

                    _logger.LogInformation($"{_userSession.UserName} has been created incident report with id: {incident_report.IncidentReportId} at {_dateTime.Now}");


                    string host = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
                   _backgroungJobClient.Enqueue(() => SendEmail(incident_report, (int)RequestStatus.Submitted, host));

                    return RedirectToAction("Index").WithSuccess("Submitted success !");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{_userSession.UserName} got error {ex} at {_dateTime.Now}");
                _logger.LogCritical(ex.Message, ex);
            }
            _logger.LogError($"{_userSession.UserName} got error invalid model at {_dateTime.Now}");
            return RedirectToAction("Index").WithError("Something went wrong. Please contact ithelpdesk@vas.edu.vn to support");

        }
        
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var entity = await _incidentReportRepository.getIncidentReportById(id);
                if (entity == null)
                    return RedirectToAction("Index");

                var model = _mapper.Map<IncidentReport, IncidentReportViewModel>(entity);
                model.ECSDEmailList = await GetECSDGroup(_userGroupSettings.ECSDGroup);
                model.lineManagerGroup = await GetECSDGroup(_userGroupSettings.LineManagerGroup);
                model.Provinces = new SelectList(await _baseCategoryRepository.GetProvinces(), "provinceId", "name");
                //var arrayInformed = new[] { model.Requester.informed1, model.Requester.informed2, model.Requester.informed3, model.Requester.informed5, model.Requester.informed6 };
                //string fullInformed = string.Join(",", arrayInformed.Where(s => !string.IsNullOrEmpty(s)));
                //model.informedEmail = fullInformed;


                var isOwner = await _authorizationService.AuthorizeAsync(User, model, Constants.Permission_Workflow_Covid.IsOwner);

                var isApprover = await _authorizationService.AuthorizeAsync(User, model, Constants.Permission_Workflow_Covid.IsApprover);

                var isHeadofDepartment = await _authorizationService.AuthorizeAsync(User, model, Constants.Permission_Workflow_Covid.IsHeadOfDepartment);

                var isECSDGroup = await _authorizationService.AuthorizeAsync(User, model, Constants.Permission_Workflow_Covid.isECSDGroup);

                if (!isECSDGroup.Succeeded && !isOwner.Succeeded && !isApprover.Succeeded && !isHeadofDepartment.Succeeded && !_userGroupSettings.HRGroup.Any(str => str.Contains(_userSession.UserName)))
                {
                    _logger.LogInformation($"AccessDenied {_userSession.UserName} ");
                    return View("AccessDenied", "Common");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{_userSession.UserName} got error {ex.Message} at {_dateTime.Now}");
                _logger.LogCritical(ex.Message, ex);
            }
            return View("Index").WithError("Something went wrong. Please contact ithelpdesk@vas.edu.vn to support");
        }

        #region LineManager Action - Verify or Reject
        [HttpPost]
        public async Task<IActionResult> approveRequest(IncidentReport model)
        {
            try
            {
                var entity = await _incidentReportRepository.getIncidentReportById(model.IncidentReportId);
                if (entity == null)
                    return RedirectToAction("Index");
                if (entity.status == (int)RequestStatus.Submitted)
                {
                    entity.status = (int)RequestStatus.lineManager_Approved;
                    entity.dateOfStatus = _dateTime.Now;
                    entity.comment = model.comment;
                    await _incidentReportRepository.updateIncidentReport(entity);

                    string host = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";

                    _backgroungJobClient.Enqueue(() => SendEmail(entity, (int)RequestStatus.lineManager_Approved, host));

                }
                return RedirectToAction("Index").WithSuccess("Thank you for your response!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"{_userSession.UserName} got error {ex.Message} at {_dateTime.Now}");
                _logger.LogCritical(ex.Message, ex);
            }
            return RedirectToAction("Index").WithError("Something went wrong. Please contact ithelpdesk@vas.edu.vn to support");
        }
        [HttpPost]
        public async Task<IActionResult> rejectRequest(IncidentReport model)
        {
            try
            {
                var entity = await _incidentReportRepository.getIncidentReportById(model.IncidentReportId);
                if (entity == null)
                    return RedirectToAction("Index");
                if (entity.status == (int)RequestStatus.Submitted)
                {
                    entity.status = (int)RequestStatus.lineManager_Rejected;
                    entity.comment = model.comment;
                    entity.dateOfStatus = _dateTime.Now;
                    await _incidentReportRepository.updateIncidentReport(entity);

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

        #region ECSD Action - Approve or Reject
                [HttpPost]
                [ValidateAntiForgeryToken]
                public async Task<IActionResult> ECSDApproval(IncidentReport model)
                {
                    try
                    {
                        var entity = await _incidentReportRepository.getIncidentReportById(model.IncidentReportId);
                        if (entity == null)
                            return RedirectToAction("Index");
                        if (!string.IsNullOrEmpty(entity.nameOfLineManager) && entity.status == (int)RequestStatus.Submitted)
                        {
                            return RedirectToAction("Index").WithError("ERROR!. Please waiting for line manager approve first !!!");
                        }

                        entity.ECSDVerifyStatus = RequestStatus.ecsd_Approved;
                        entity.ECSDCommentDate = _dateTime.Now;
                        entity.ECSDComment = model.ECSDComment;
                        await _incidentReportRepository.updateIncidentReport(entity);
                        string host = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
                        _backgroungJobClient.Enqueue(() => SendEmail(entity, (int)RequestStatus.ecsd_Approved, host));

                        return RedirectToAction("Index").WithSuccess("Thank you for your response!");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"{_userSession.UserName} got error {ex.Message} at {_dateTime.Now}");
                        _logger.LogCritical(ex.Message, ex);
                    }
                    return View("Index").WithError("Something went wrong. Please contact ithelpdesk@vas.edu.vn to support");
                }
                [HttpPost]
                [ValidateAntiForgeryToken]
                public async Task<IActionResult> ECSDReject(IncidentReport model)
                {
                    try
                    {
                        var entity = await _incidentReportRepository.getIncidentReportById(model.IncidentReportId);
                        if (entity == null)
                            return RedirectToAction("Index");

                        if (!string.IsNullOrEmpty(entity.nameOfLineManager) && entity.status == (int)RequestStatus.Submitted)
                        {
                            return RedirectToAction("Index").WithError("ERROR!. Please waiting for line manager approve first !!!");
                        }
                        entity.ECSDVerifyStatus = RequestStatus.ecsd_Rejected;
                        entity.ECSDCommentDate = _dateTime.Now;
                        entity.ECSDComment = model.ECSDComment;
                        await _incidentReportRepository.updateIncidentReport(entity);
                        string host = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
                        _backgroungJobClient.Enqueue(() => SendEmail(entity, (int)RequestStatus.ecsd_Rejected, host));
                        return RedirectToAction("Index").WithSuccess("Thank you for your response!");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"{_userSession.UserName} got error {ex.Message} at {_dateTime.Now}");
                        _logger.LogCritical(ex.Message, ex);
                    }
                    return View("Index").WithError("Something went wrong. Please contact ithelpdesk@vas.edu.vn to support");

                } 
                #endregion

        
        #region sendEmail
        public async Task SendEmail(IncidentReport model, int requestStatus, string currentUrl)
        {
            try
            {
                //for requester
                if (requestStatus == (int)(RequestStatus.Submitted))
                {

                    List<MailboxAddress> ccEmail = new List<MailboxAddress>();
                    if (!string.IsNullOrEmpty(model.informedEmail))
                    {
                        if (model.informedEmail.IndexOf(",") < 0)
                        {
                            ccEmail.Add(MailboxAddress.Parse(model.informedEmail));
                        }
                        else
                        {
                            string[] emailSplit = model.informedEmail.Split(",");
                            foreach (var item in emailSplit)
                            {
                                ccEmail.Add(MailboxAddress.Parse(item));
                            }
                        }
                    }
                    ccEmail.Add(MailboxAddress.Parse(model.Requester.UserName + "@vas.edu.vn"));

                    if (!string.IsNullOrEmpty(model.nameOfLineManager) && !string.IsNullOrEmpty(model.ECSDEmail))
                    {
                        var emailModel = await _emailConfigRepository.getTemplatesByRequestStatus((int)(RequestStatus.Submitted));
                        string body = emailModel.forSubmitted.Body;
                        body = TravelDeclarationSendEmail.ReplaceMessageTemplateTokensForCovid(model, currentUrl, body);

                        await _emailSender.SendEmailWithVASCredential(model.nameOfLineManager, emailModel.forSubmitted.Subject, body, ccEmail);
                    }

                    if (string.IsNullOrEmpty(model.nameOfLineManager) && !string.IsNullOrEmpty(model.ECSDEmail))
                    {
                        var emailModel = await _emailConfigRepository.getTemplatesByRequestStatus((int)(RequestStatus.sent_request_to_ECSD));
                        string body = emailModel.forRequestToECSD.Body;
                        body = TravelDeclarationSendEmail.ReplaceMessageTemplateTokensForCovid(model, currentUrl, body);

                        await _emailSender.SendEmailWithVASCredential(model.ECSDEmail, emailModel.forRequestToECSD.Subject, body, ccEmail);
                    }
                }
                //for line manager - approve
                if (requestStatus == (int)(RequestStatus.lineManager_Approved))
                {
                    var emailModel = await _emailConfigRepository.getTemplatesByRequestStatus((int)(RequestStatus.lineManager_Approved));
                    string body = emailModel.forApproved.Body;

                    body = TravelDeclarationSendEmail.ReplaceMessageTemplateTokensForCovid(model, currentUrl, body);
                    List<MailboxAddress> ccEmail = new List<MailboxAddress>();
                    if (!string.IsNullOrEmpty(model.informedEmail))
                    {
                        if (model.informedEmail.IndexOf(",") < 0)
                        {
                            ccEmail.Add(MailboxAddress.Parse(model.informedEmail));
                        }
                        else
                        {
                            string[] emailSplit = model.informedEmail.Split(",");
                            foreach (var item in emailSplit)
                            {
                                ccEmail.Add(MailboxAddress.Parse(item));
                            }
                        }
                    }
                    ccEmail.Add(MailboxAddress.Parse(model.Requester.UserName + "@vas.edu.vn"));
                    if (!string.IsNullOrEmpty(model.ECSDEmail))
                    {
                        await _emailSender.SendEmailWithVASCredential(model.ECSDEmail, emailModel.forApproved.Subject, body, ccEmail);
                    }

                }
                //for line manager - reject
                if (requestStatus == (int)(RequestStatus.lineManager_Rejected))
                {
                    var emailModel = await _emailConfigRepository.getTemplatesByRequestStatus((int)(RequestStatus.lineManager_Rejected));
                    string body = emailModel.forRejected.Body;

                    body = TravelDeclarationSendEmail.ReplaceMessageTemplateTokensForCovid(model, currentUrl, body);
                    List<MailboxAddress> ccEmail = new List<MailboxAddress>();
                    if (!string.IsNullOrEmpty(model.informedEmail))
                    {
                        string[] emailSplit = model.informedEmail.Split(",");
                        foreach (var item in emailSplit)
                        {
                            ccEmail.Add(MailboxAddress.Parse(item));
                        }
                    }
                    if (!string.IsNullOrEmpty(model.nameOfLineManager))
                    {
                        ccEmail.Add(MailboxAddress.Parse(model.nameOfLineManager));
                    }

                    await _emailSender.SendEmailWithVASCredential(model.Requester.UserName + "@vas.edu.vn", emailModel.forRejected.Subject, body, ccEmail);
                }

                //for ecsd - approve
                if (requestStatus == (int)(RequestStatus.sent_request_to_ECSD))
                {
                    var emailModel = await _emailConfigRepository.getTemplatesByRequestStatus((int)(RequestStatus.sent_request_to_ECSD));
                    string body = emailModel.forRequestToECSD.Body;

                    body = TravelDeclarationSendEmail.ReplaceMessageTemplateTokensForCovid(model, currentUrl, body);
                    List<MailboxAddress> ccEmail = new List<MailboxAddress>();
                    if (!string.IsNullOrEmpty(model.informedEmail))
                    {
                        string[] emailSplit = model.informedEmail.Split(",");
                        foreach (var item in emailSplit)
                        {
                            ccEmail.Add(MailboxAddress.Parse(item));
                        }
                    }
                    ccEmail.Add(MailboxAddress.Parse(model.Requester.UserName + "@vas.edu.vn"));

                    await _emailSender.SendEmailWithVASCredential(model.ECSDEmail, emailModel.forRequestToECSD.Subject, body, ccEmail);
                }

                //for ecsd - approve
                if (requestStatus == (int)(RequestStatus.ecsd_Approved))
                {
                    var emailModel = await _emailConfigRepository.getTemplatesByRequestStatus((int)(RequestStatus.ecsd_Approved));
                    string body = emailModel.forECSDApproved.Body;

                    body = TravelDeclarationSendEmail.ReplaceMessageTemplateTokensForCovid(model, currentUrl, body);
                    List<MailboxAddress> ccEmail = new List<MailboxAddress>();
                    if (!string.IsNullOrEmpty(model.informedEmail))
                    {
                        string[] emailSplit = model.informedEmail.Split(",");
                        foreach (var item in emailSplit)
                        {
                            ccEmail.Add(MailboxAddress.Parse(item));
                        }
                    }

                    if (!string.IsNullOrEmpty(model.nameOfLineManager))
                    {
                        ccEmail.Add(MailboxAddress.Parse(model.nameOfLineManager));
                    }
                    if (!string.IsNullOrEmpty(model.ECSDEmail))
                    {
                        ccEmail.Add(MailboxAddress.Parse(model.ECSDEmail));
                    }

                    await _emailSender.SendEmailWithVASCredential(model.Requester.UserName + "@vas.edu.vn", emailModel.forECSDApproved.Subject, body, ccEmail);
                }
                if (requestStatus == (int)(RequestStatus.ecsd_Rejected))
                {
                    var emailModel = await _emailConfigRepository.getTemplatesByRequestStatus((int)(RequestStatus.ecsd_Rejected));
                    string body = emailModel.forECSDRejected.Body;

                    body = TravelDeclarationSendEmail.ReplaceMessageTemplateTokensForCovid(model, currentUrl, body);
                    List<MailboxAddress> ccEmail = new List<MailboxAddress>();
                    if (!string.IsNullOrEmpty(model.informedEmail))
                    {
                        string[] emailSplit = model.informedEmail.Split(",");
                        foreach (var item in emailSplit)
                        {
                            ccEmail.Add(MailboxAddress.Parse(item));
                        }
                    }

                    if (!string.IsNullOrEmpty(model.nameOfLineManager))
                    {
                        ccEmail.Add(MailboxAddress.Parse(model.nameOfLineManager));
                    }
                    if (!string.IsNullOrEmpty(model.ECSDEmail))
                    {
                        ccEmail.Add(MailboxAddress.Parse(model.ECSDEmail));
                    }

                    await _emailSender.SendEmailWithVASCredential(model.Requester.UserName + "@vas.edu.vn", emailModel.forECSDRejected.Subject, body, ccEmail);
                }



            }
            catch (Exception ex)
            {

                throw ex;
            }
           
        }
        #endregion

        #region action many requests
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
                    var entity = await _incidentReportRepository.getIncidentReportById(Id);
                    if (entity == null)
                        return RedirectToAction("Index");
                    if (entity.status == (int)RequestStatus.Submitted)
                    {
                        if (entity.nameOfLineManager != null && entity.nameOfLineManager.Split("@")[0] == _userSession.UserName || entity.ECSDEmail != null && entity.ECSDEmail.Split("@")[0] == _userSession.UserName)
                        {
                            entity.status = actionStatus;
                            if (actionStatus == (int)RequestStatus.ecsd_Approved || actionStatus == (int)RequestStatus.ecsd_Rejected)
                            {
                                entity.ECSDVerifyStatus = (RequestStatus)actionStatus;
                            }
                            entity.dateOfStatus = _dateTime.Now;
                            await _incidentReportRepository.updateIncidentReport(entity);
                            string host = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";

                            _backgroungJobClient.Enqueue(() => SendEmail(entity, actionStatus, host));
                        }
                    }
                }
                return Json(new { success = true, message = "Success !" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"{_userSession.UserName} got error {ex.Message} at {_dateTime.Now}");
                _logger.LogCritical(ex.Message, ex);
                return Json(new { success = false, message = "ERROR! Please contact to IThelpdesk !" });
            }

        }
        #endregion

        #region checkPermission
        public async Task<IActionResult> checkPermissionForApproveRequest(Guid requestId)
        {
            if (_userSession.UserName != null)
            {
                var requestEntity = await _incidentReportRepository.getIncidentReportById(requestId);
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

        #endregion

        #region driver method   
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

        private async Task<string> getProvinceName(string provinceId)
        {
            var entity = await _baseCategoryRepository.GetProvinces();
            if (entity != null)
            {
                return entity.Where(t => t.provinceId == provinceId).FirstOrDefault()?.name;
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
        
        #region support Method
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
        public async Task<IActionResult> ListAllRequestByUser()
        {
            try
            {
                if (_userGroupSettings.HRGroup.FirstOrDefault(t => t == _userSession.UserName) != null)
                {
                    var entities = await _incidentReportRepository.getAllRequest();
                    var model = entities.Select(e => _mapper.Map<IncidentReport, IncidentReportListDto>(e)).OrderBy(t => t.status).ToList();
                    return View(model);
                }
                else
                {
                    var entities = await _incidentReportRepository.listAllRequestByUser(_userSession.Id, _userSession.UserName.Split('@')[0]);
                    var model = entities.Select(e => _mapper.Map<IncidentReport, IncidentReportListDto>(e)).OrderBy(t => t.status).ToList();
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{_userSession.UserName} got error {ex} at {_dateTime.Now}");
                _logger.LogCritical(ex.Message, ex);
            }
            return View("Index").WithError("Something went wrong. Please contact ithelpdesk@vas.edu.vn to support");

        }
        public async Task<IActionResult> ListAllRequestByCampus(string campusCode)
        {
            var entities = await _incidentReportRepository.listAllRequestByCampus(_userSession.Id, campusCode);
            var model = entities.Select(e => _mapper.Map<IncidentReport, IncidentReportListDto>(e)).ToList();
            return Json(new { data = model });
        } 
        #endregion

        #region Export Excel    
        public async Task<IActionResult> ExportExcel(string campus)
        {
            IList<IncidentReport> listAllRequest = new List<IncidentReport>();
            try
            {
                if (_userGroupSettings.HRGroup.FirstOrDefault(t => t == _userSession.UserName) != null)
                {
                    listAllRequest = await _incidentReportRepository.getAllRequest();
                }
                else
                {
                    var userDetails = await _userRepository.GetUserByIdAsync(_userSession.Id);
                    if (userDetails != null)
                    {
                        if (userDetails.HeadOfDepartments != null)
                        {
                            listAllRequest = await _incidentReportRepository.listAllRequestByCampus(_userSession.Id, campus);
                        }
                    }
                    else
                    {
                        listAllRequest = await _incidentReportRepository.listAllRequestByUser(_userSession.Id, _userSession.UserName.Split('@')[0]);
                    }
                }



                var stream = new MemoryStream();
                using (var package = new ExcelPackage(stream))
                {
                    //name the sheet "Sheet1"
                    var workSheet = package.Workbook.Worksheets.Add("Covid Incident");
                    workSheet.Cells.Style.Font.Name = "Cambria";


                    //// Header
                    workSheet.Cells[1, 1].Value = "COVID-19 FOLLOW LIST";
                    workSheet.Cells[1, 1, 2, 20].Merge = true; //Merge columns start and end range
                    workSheet.Cells[1, 1, 2, 20].Style.Font.Bold = true; //Font should be bold
                    workSheet.Cells[1, 1, 2, 20].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[1, 1, 2, 20].Style.Font.Size = 16;
                    workSheet.Cells[1, 1, 2, 20].Style.Font.Color.SetColor(Color.Red);

                    //Summary
                    workSheet.Cells[3, 1].Value = "THÔNG TIN CÁ NHÂN KÊ KHAI / GENERAL INFORMATION";
                    workSheet.Cells[3, 1, 3, 24].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 1, 3, 24].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[3, 1, 3, 24].Style.Font.Size = 14;
                    workSheet.Cells[3, 1, 3, 24].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 1, 3, 24].Style.Font.Bold = true;
                    workSheet.Cells[3, 1, 3, 24].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[3, 1, 3, 24].Style.Fill.BackgroundColor.SetColor(Color.DeepSkyBlue);

                    //Covid19 incident report   
                    workSheet.Cells[3, 25].Value = "Bản tường trình COVID - 19 / COVID - 19 INCIDENT REPORT";
                    workSheet.Cells[3, 25, 3, 73].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 25, 3, 73].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[3, 25, 3, 73].Style.Font.Size = 14;
                    workSheet.Cells[3, 25, 3, 73].Style.Font.Bold = true;
                    workSheet.Cells[3, 25, 3, 73].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 25, 3, 73].Style.Fill.BackgroundColor.SetColor(Color.DeepSkyBlue);


                    //No
                    workSheet.Cells[4, 1].Value = "No";
                    workSheet.Cells[4, 1, 7, 1].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 1, 7, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 1, 7, 1].Style.Font.Size = 11;
                    workSheet.Cells[4, 1, 7, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 1, 7, 1].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(1).Width = 5;

                    //RequestID
                    workSheet.Cells[4, 2].Value = "Request ID";
                    workSheet.Cells[4, 2, 7, 2].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 2, 7, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 2, 7, 2].Style.Font.Size = 11;
                    workSheet.Cells[4, 2, 7, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 2, 7, 2].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(2).Width = 10;

                    //Status
                    workSheet.Cells[4, 3].Value = "Trạng thái/ Status";
                    workSheet.Cells[4, 3, 7, 3].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 3, 7, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 3, 7, 3].Style.Font.Size = 11;
                    workSheet.Cells[4, 3, 7, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 3, 7, 3].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(3).Width = 15;

                    //submitted date
                    workSheet.Cells[4, 4].Value = "Ngày gửi/ Submitted Date";
                    workSheet.Cells[4, 4, 7, 4].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 4, 7, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 4, 7, 4].Style.Font.Size = 11;
                    workSheet.Cells[4, 4, 7, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 4, 7, 4].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(4).Width = 15;

                    // verified date
                    workSheet.Cells[4, 5].Value = "Ngày xác nhận/ Verified or Rejected Date";
                    workSheet.Cells[4, 5, 7, 5].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 5, 7, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 5, 7, 5].Style.Font.Size = 11;
                    workSheet.Cells[4, 5, 7, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 5, 7, 5].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(5).Width = 15;

                    //approve date
                    workSheet.Cells[4, 6].Value = "Ngày duyệt/ Approval or Rejected Date";
                    workSheet.Cells[4, 6, 7, 6].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 6, 7, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 6, 7, 6].Style.Font.Size = 11;
                    workSheet.Cells[4, 6, 7, 6].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 6, 7, 6].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(6).Width = 15;

                    //cancelled
                    workSheet.Cells[4, 7].Value = "Ngày hủy/ Cancelled";
                    workSheet.Cells[4, 7, 7, 7].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 7, 7, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 7, 7, 7].Style.Font.Size = 11;
                    workSheet.Cells[4, 7, 7, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 7, 7, 7].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(7).Width = 15;

                    //CAM/MOET
                    workSheet.Cells[4, 8].Value = "MOET/CAM";
                    workSheet.Cells[4, 8, 7, 8].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 8, 7, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 8, 7, 8].Style.Font.Size = 11;
                    workSheet.Cells[4, 8, 7, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 8, 7, 8].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(8).Width = 10;

                    //Type of list
                    workSheet.Cells[4, 9].Value = "Nhóm/ Type of list";
                    workSheet.Cells[4, 9, 7, 9].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 9, 7, 9].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 9, 7, 9].Style.Font.Size = 11;
                    workSheet.Cells[4, 9, 7, 9].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 9, 7, 9].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(9).Width = 8;

                    //Employee ID
                    workSheet.Cells[4, 10].Value = "Mã nhân viên/ EME Code";
                    workSheet.Cells[4, 10, 7, 10].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 10, 7, 10].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 10, 7, 10].Style.Font.Size = 11;
                    workSheet.Cells[4, 10, 7, 10].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 10, 7, 10].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(10).Width = 10;

                    //Patient/Suspect Name
                    workSheet.Cells[4, 11].Value = "Họ Tên/ Fullname";
                    workSheet.Cells[4, 11, 7, 11].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 11, 7, 11].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 11, 7, 11].Style.Font.Size = 11;
                    workSheet.Cells[4, 11, 7, 11].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 11, 7, 11].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(11).Width = 20;


                    /*//Position header
                    workSheet.Cells[4, 12].Value = "Chức Danh/ Position";
                    workSheet.Cells[4, 12, 5, 12].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 12, 5, 12].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 12, 5, 12].Style.Font.Size = 11;
                    workSheet.Cells[4, 12, 5, 12].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 12, 5, 12].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);*/

                    //Position
                    workSheet.Cells[4, 12].Value = "Chức Danh/ Position";
                    workSheet.Cells[4, 12, 5, 13].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 12, 5, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 12, 5, 13].Style.Font.Size = 11;
                    workSheet.Cells[4, 12, 5, 13].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 12, 5, 13].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);

                    //Position code
                    workSheet.Cells[6, 12].Value = "Mã Chức danh/ Position code";
                    workSheet.Cells[6, 12, 7, 12].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 12, 7, 12].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 12, 7, 12].Style.Font.Size = 11;
                    workSheet.Cells[6, 12, 7, 12].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 12, 7, 12].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);

                    //Title
                    workSheet.Cells[6, 13].Value = "Chức Danh/ Title";
                    workSheet.Cells[6, 13, 7, 13].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 13, 7, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 13, 7, 13].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 13, 7, 13].Style.Font.Size = 11;
                    workSheet.Cells[6, 13, 7, 13].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 13, 7, 13].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(13).Width = 15;

                    //Dept./Campuses
                    workSheet.Cells[4, 14].Value = "Cơ sở - PB/ Campuses - Dept.";
                    workSheet.Cells[4, 14, 7, 14].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 14, 7, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 14, 7, 14].Style.Font.Size = 11;
                    workSheet.Cells[4, 14, 7, 14].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 14, 7, 14].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(14).Width = 15;

                    //Địa chỉ / Current Address
                    workSheet.Cells[4, 15].Value = "Địa chỉ / Current Address";
                    workSheet.Cells[4, 15, 7, 15].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 15, 7, 15].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 15, 7, 15].Style.Font.Size = 11;
                    workSheet.Cells[4, 15, 7, 15].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 15, 7, 15].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(15).Width = 25;

                    //Số ĐTDĐ / Phone No.
                    workSheet.Cells[4, 16].Value = "Số ĐTDĐ / Phone Number";
                    workSheet.Cells[4, 16, 7, 16].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 16, 7, 16].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 16, 7, 16].Style.Font.Size = 11;
                    workSheet.Cells[4, 16, 7, 16].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 16, 7, 16].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(16).Width = 15;

                    //Thông tin liên hệ khẩn cấp/ Emergency Contact
                    workSheet.Cells[4, 17].Value = "Thông tin liên hệ khẩn cấp/ Emergency Contact";
                    workSheet.Cells[4, 17, 5, 20].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 17, 5, 20].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 17, 5, 20].Style.Font.Size = 11;
                    workSheet.Cells[4, 17, 5, 20].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 17, 5, 20].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);


                    //Họ tên/ Full name
                    workSheet.Cells[6, 17].Value = "Họ tên/ Full name";
                    workSheet.Cells[6, 17, 7, 17].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 17, 7, 17].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 17, 7, 17].Style.Font.Size = 11;
                    workSheet.Cells[6, 17, 7, 17].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 17, 7, 17].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(17).Width = 25;

                    //Số ĐTDĐ / Phone No.
                    workSheet.Cells[6, 18].Value = "Số ĐTDĐ / Phone Number";
                    workSheet.Cells[6, 18, 7, 18].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 18, 7, 18].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 18, 7, 18].Style.Font.Size = 11;
                    workSheet.Cells[6, 18, 7, 18].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 18, 7, 18].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(18).Width = 15;

                    //Mối quan hệ với nhân viên/ Relationship with staff
                    workSheet.Cells[6, 19].Value = "Mối quan hệ với nhân viên/ Relationship with staff";
                    workSheet.Cells[6, 19, 7, 19].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 19, 7, 19].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 19, 7, 19].Style.Font.Size = 11;
                    workSheet.Cells[6, 19, 7, 19].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 19, 7, 19].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(19).Width = 20;

                    //Địa chỉ liên hệ/ Contact Address
                    workSheet.Cells[6, 20].Value = "Địa chỉ liên hệ/ Contact Address";
                    workSheet.Cells[6, 20, 7, 20].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 20, 7, 20].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 20, 7, 20].Style.Font.Size = 11;
                    workSheet.Cells[6, 20, 7, 20].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 20, 7, 20].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(20).Width = 25;

                    //Địa điểm vùng dịch đã đến/Red zone
                    workSheet.Cells[4, 21].Value = "Địa điểm vùng dịch đã đến/Red zone";
                    workSheet.Cells[4, 21, 5, 24].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 21, 5, 24].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 21, 5, 24].Style.Font.Size = 11;
                    workSheet.Cells[4, 21, 5, 24].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 21, 5, 24].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);

                    //Tỉnh - Thành phố/ Province - City
                    workSheet.Cells[6, 21].Value = "Tỉnh - Thành phố/ Province - City";
                    workSheet.Cells[6, 21, 7, 21].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 21, 7, 21].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 21, 7, 21].Style.Font.Size = 11;
                    workSheet.Cells[6, 21, 7, 21].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 21, 7, 21].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(21).Width = 15;

                    //Quận - Huyện/ District
                    workSheet.Cells[6, 22].Value = "Tỉnh - Thành phố/ Province - City";
                    workSheet.Cells[6, 22, 7, 22].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 22, 7, 22].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 22, 7, 22].Style.Font.Size = 11;
                    workSheet.Cells[6, 22, 7, 22].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 22, 7, 22].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(22).Width = 15;

                    //Phường - Xã/ Ward
                    workSheet.Cells[6, 23].Value = "Phường - Xã/ Ward";
                    workSheet.Cells[6, 23, 7, 23].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 23, 7, 23].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 23, 7, 23].Style.Font.Size = 11;
                    workSheet.Cells[6, 23, 7, 23].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 23, 7, 23].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(23).Width = 15;

                    //Địa chỉ/ Address
                    workSheet.Cells[6, 24].Value = "Địa chỉ/ Address";
                    workSheet.Cells[6, 24, 7, 24].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 24, 7, 24].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 24, 7, 24].Style.Font.Size = 11;
                    workSheet.Cells[6, 24, 7, 24].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 24, 7, 24].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    workSheet.Column(24).Width = 25;


                    /* End of Summary */

                    /* Start of COVID-19 Incident Report */

                    /*start of 2. */
                    //THỜI GIAN VÀ ĐỊA ĐIỂM (DÀNH CHO NGƯỜI ĐI VỀ TỪ VÙNG DỊCH) / TIME & LOCATION (FOR THOSE TRAVELLING FROM RED ZONE)
                    workSheet.Cells[4, 25].Value = "2. THỜI GIAN VÀ ĐỊA ĐIỂM (DÀNH CHO NGƯỜI ĐI VỀ TỪ VÙNG DỊCH) / TIME & LOCATION (FOR THOSE TRAVELLING FROM RED ZONE)";
                    workSheet.Cells[4, 25, 4, 30].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 25, 4, 30].Style.Font.Bold = true;
                    workSheet.Cells[4, 25, 4, 30].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[4, 25, 4, 30].Style.Font.Size = 11;
                    workSheet.Cells[4, 25, 4, 30].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 25, 4, 30].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[4, 25, 4, 30].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);

                    //Ngày đi - Header
                    workSheet.Cells[5, 25].Value = "Ngày đi / Departure date";
                    workSheet.Cells[5, 25, 5, 27].Merge = true; //Merge columns start and end range
                    workSheet.Cells[5, 25, 5, 27].Style.Font.Bold = true;
                    workSheet.Cells[5, 25, 5, 27].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[5, 25, 5, 27].Style.Font.Size = 11;
                    workSheet.Cells[5, 25, 5, 27].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[5, 25, 5, 27].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[5, 25, 5, 27].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);


                    // Ngày Về - Header  
                    workSheet.Cells[5, 28].Value = "Ngày về / Returning date";
                    workSheet.Cells[5, 28, 5, 30].Merge = true; //Merge columns start and end range
                    workSheet.Cells[5, 28, 5, 30].Style.Font.Bold = true;
                    workSheet.Cells[5, 28, 5, 30].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[5, 28, 5, 30].Style.Font.Size = 11;
                    workSheet.Cells[5, 28, 5, 30].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[5, 28, 5, 30].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[5, 28, 5, 30].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);


                    //Ngày đi child
                    workSheet.Cells[6, 25].Value = "Ngày đi / Departure date";
                    workSheet.Cells[6, 25, 7, 25].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 25, 7, 25].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 25, 7, 25].Style.Font.Size = 11;
                    workSheet.Cells[6, 25, 7, 25].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 25, 7, 25].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(25).Width = 15;


                    //Phương tiện - ngày đi child
                    workSheet.Cells[6, 26].Value = "Phương tiện / Means of transportation ";
                    workSheet.Cells[6, 26, 7, 26].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 26, 7, 26].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 26, 7, 26].Style.Font.Size = 11;
                    workSheet.Cells[6, 26, 7, 26].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 26, 7, 26].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(26).Width = 15;

                    //Đính kèm vé - ngày đi child
                    workSheet.Cells[6, 27].Value = "Đính kèm vé / Enclosed ticket ";
                    workSheet.Cells[6, 27, 7, 27].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 27, 7, 27].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 27, 7, 27].Style.Font.Size = 11;
                    workSheet.Cells[6, 27, 7, 27].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 27, 7, 27].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(27).Width = 5;

                    //Ngày về - child
                    workSheet.Cells[6, 28].Value = "Ngày về/ Returning date";
                    workSheet.Cells[6, 28, 7, 28].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 28, 7, 28].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 28, 7, 28].Style.Font.Size = 11;
                    workSheet.Cells[6, 28, 7, 28].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 28, 7, 28].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(28).Width = 15;

                    //Phương tiện - ngày về child
                    workSheet.Cells[6, 29].Value = "Phương tiện/ Means of transportation";
                    workSheet.Cells[6, 29, 7, 29].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 29, 7, 29].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 29, 7, 29].Style.Font.Size = 11;
                    workSheet.Cells[6, 29, 7, 29].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 29, 7, 29].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(29).Width = 15;

                    //Đính kèm vé
                    workSheet.Cells[6, 30].Value = "Đính kèm vé/ Enclosed ticket";
                    workSheet.Cells[6, 30, 7, 30].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 30, 7, 30].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 30, 7, 30].Style.Font.Size = 11;
                    workSheet.Cells[6, 30, 7, 30].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 30, 7, 30].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(30).Width = 5;


                    /* End of 2. */


                    /* Start of 3. */

                    // Lộ trình di chuyển / travelling routes 
                    workSheet.Cells[4, 31].Value = "3. LỘ TRÌNH DI CHUYỂN / TRAVELLING ROUTES";
                    workSheet.Cells[4, 31, 4, 54].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 31, 4, 54].Style.Font.Bold = true;
                    workSheet.Cells[4, 31, 4, 54].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 31, 4, 54].Style.Font.Size = 11;
                    workSheet.Cells[4, 31, 4, 54].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 31, 4, 54].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[4, 31, 4, 54].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);

                    // DÀNH CHO NGƯỜI VỀ TỪ VÙNG DỊCH / FOR THOSE RETURNING FROM RED ZONE  
                    workSheet.Cells[5, 31].Value = "3.1 DÀNH CHO NGƯỜI VỀ TỪ VÙNG DỊCH / FOR THOSE RETURNING FROM RED ZONE";
                    workSheet.Cells[5, 31, 5, 40].Merge = true; //Merge columns start and end range
                    workSheet.Cells[5, 31, 5, 40].Style.Font.Bold = true;
                    workSheet.Cells[5, 31, 5, 40].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[5, 31, 5, 40].Style.Font.Size = 11;
                    workSheet.Cells[5, 31, 5, 40].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[5, 31, 5, 40].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[5, 31, 5, 40].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);


                    // LỘ TRÌNH TẠI VÙNG DỊCH / ROUTES AT RED ZONE 
                    workSheet.Cells[6, 31].Value = "3.1.1. LỘ TRÌNH TẠI VÙNG DỊCH / ROUTES AT RED ZONE";
                    workSheet.Cells[6, 31, 6, 35].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 31, 6, 35].Style.Font.Bold = false;
                    workSheet.Cells[6, 31, 6, 35].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 31, 6, 35].Style.Font.Size = 11;
                    workSheet.Cells[6, 31, 6, 35].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 31, 6, 35].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[6, 31, 6, 35].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);

                    // Từ Ngày & giờ đi
                    workSheet.Cells[7, 31].Value = "Ngày & Giờ / Date & Time";
                    workSheet.Cells[7, 31].Merge = true; //Merge columns start and end range
                    workSheet.Cells[7, 31].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[7, 31].Style.Font.Size = 11;
                    workSheet.Cells[7, 31].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[7, 31].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(31).Width = 15;

                    // Đến Ngày & giờ đi
                    workSheet.Cells[7, 32].Value = "Đến (Ngày & Giờ)/ To (Date & Time)";
                    workSheet.Cells[7, 32].Merge = true; //Merge columns start and end range
                    workSheet.Cells[7, 32].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[7, 32].Style.Font.Size = 11;
                    workSheet.Cells[7, 32].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[7, 32].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(32).Width = 15;

                    //Địa điểm / Location 
                    workSheet.Cells[7, 33].Value = "Địa điểm / location";
                    workSheet.Cells[7, 33].Merge = true; //Merge columns start and end range
                    workSheet.Cells[7, 33].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[7, 33].Style.Font.Size = 11;
                    workSheet.Cells[7, 33].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[7, 33].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(33).Width = 30;

                    //Phương tiện
                    workSheet.Cells[7, 34].Value = "Phương tiện / Means of transportation";
                    workSheet.Cells[7, 34].Merge = true; //Merge columns start and end range
                    workSheet.Cells[7, 34].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[7, 34].Style.Font.Size = 11;
                    workSheet.Cells[7, 34].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[7, 34].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(34).Width = 15;

                    //Notes
                    workSheet.Cells[7, 35].Value = "Ghi chú/ Notes";
                    workSheet.Cells[7, 35].Merge = true; //Merge columns start and end range
                    workSheet.Cells[7, 35].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[7, 35].Style.Font.Size = 11;
                    workSheet.Cells[7, 35].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[7, 35].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(35).Width = 10;


                    // LỘ TRÌNH SAU KHI RA KHỎI VÙNG DỊCH / ROUTES OUTSIDE AT RED ZONE 
                    workSheet.Cells[6, 36].Value = "3.1.2. LỘ TRÌNH SAU KHI RA KHỎI VÙNG DỊCH / ROUTES OUTSIDE AT RED ZONE ";
                    workSheet.Cells[6, 36, 6, 40].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 36, 6, 40].Style.Font.Bold = false;
                    workSheet.Cells[6, 36, 6, 40].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 36, 6, 40].Style.Font.Size = 11;
                    workSheet.Cells[6, 36, 6, 40].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 36, 6, 40].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[6, 36, 6, 40].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);

                    // Từ Ngày & giờ đi
                    workSheet.Cells[7, 36].Value = "Ngày & Giờ / Date & Time";
                    workSheet.Cells[7, 36].Merge = true; //Merge columns start and end range
                    workSheet.Cells[7, 36].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[7, 36].Style.Font.Size = 11;
                    workSheet.Cells[7, 36].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[7, 36].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(36).Width = 15;

                    // Đến Ngày & giờ đi
                    workSheet.Cells[7, 37].Value = "Đến (Ngày & Giờ)/ To (Date & Time)";
                    workSheet.Cells[7, 37].Merge = true; //Merge columns start and end range
                    workSheet.Cells[7, 37].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[7, 37].Style.Font.Size = 11;
                    workSheet.Cells[7, 37].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[7, 37].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(37).Width = 15;

                    //Địa điểm / Location 
                    workSheet.Cells[7, 38].Value = "Địa điểm / location (House number, Street, Ward, District, City/ Province";
                    workSheet.Cells[7, 38].Merge = true; //Merge columns start and end range
                    workSheet.Cells[7, 38].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[7, 38].Style.Font.Size = 11;
                    workSheet.Cells[7, 38].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[7, 38].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(38).Width = 30;

                    //Phương tiện
                    workSheet.Cells[7, 39].Value = "Phương tiện / Means of transportation";
                    workSheet.Cells[7, 39].Merge = true; //Merge columns start and end range
                    workSheet.Cells[7, 39].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[7, 39].Style.Font.Size = 11;
                    workSheet.Cells[7, 39].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[7, 39].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(39).Width = 15;

                    //Notes
                    workSheet.Cells[7, 40].Value = "Ghi chú/ Notes";
                    workSheet.Cells[7, 40].Merge = true; //Merge columns start and end range
                    workSheet.Cells[7, 40].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[7, 40].Style.Font.Size = 11;
                    workSheet.Cells[7, 40].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[7, 40].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(40).Width = 10;


                    // 3.2 DÀNH CHO NGƯỜI TIẾP XÚC VỚI NGƯỜI NGHI NHIỄM / FOR THOSE CONTACTING WITH A SUSPECT CASE OF COVID-19
                    workSheet.Cells[5, 41].Value = "3.2 DÀNH CHO NGƯỜI TIẾP XÚC VỚI NGƯỜI NGHI NHIỄM / FOR THOSE CONTACTING WITH A SUSPECT CASE OF COVID-19";
                    workSheet.Cells[5, 41, 5, 47].Merge = true; //Merge columns start and end range
                    workSheet.Cells[5, 41, 5, 47].Style.Font.Bold = true;
                    workSheet.Cells[5, 41, 5, 47].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[5, 41, 5, 47].Style.Font.Size = 11;
                    workSheet.Cells[5, 41, 5, 47].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[5, 41, 5, 47].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[5, 41, 5, 47].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);

                    // Từ Ngày & giờ đi
                    workSheet.Cells[6, 41].Value = "Ngày & Giờ / Date & Time";
                    workSheet.Cells[6, 41, 7 ,41].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 41, 7, 41].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 41, 7, 41].Style.Font.Size = 11;
                    workSheet.Cells[6, 41, 7, 41].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 41, 7, 41].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(41).Width = 15;

                    // Đến Ngày & giờ đi
                    workSheet.Cells[6, 42].Value = "Đến (Ngày & Giờ)/ To (Date & Time)";
                    workSheet.Cells[6, 42, 7, 42].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 42, 7, 42].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 42, 7, 42].Style.Font.Size = 11;
                    workSheet.Cells[6, 42, 7, 42].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 42, 7, 42].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(42).Width = 15;

                    // Đối tượng 
                    workSheet.Cells[6, 43].Value = "Đối tượng nghi nhiễm/ Suspect Case";
                    workSheet.Cells[6, 43, 7, 43].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 43, 7, 43].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 43, 7, 43].Style.Font.Size = 11;
                    workSheet.Cells[6, 43, 7, 43].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 43, 7, 43].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(43).Width = 15;

                    // Địa điểm / Location (House number, Street, Ward, District, City/Province)
                    workSheet.Cells[6, 44].Value = "Địa điểm / Location (House number, Street, Ward, District, City/Province)";
                    workSheet.Cells[6, 44, 7, 44].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 44, 7, 44].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 44, 7, 44].Style.Font.Size = 11;
                    workSheet.Cells[6, 44, 7, 44].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 44, 7, 44].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(44).Width = 30;

                    // Mối quan hệ 
                    workSheet.Cells[6, 45].Value = "Mối quan hệ với nhân viên / Relationship with staff";
                    workSheet.Cells[6, 45, 7, 45].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 45, 7, 45].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 45, 7, 45].Style.Font.Size = 11;
                    workSheet.Cells[6, 45, 7, 45].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 45, 7, 45].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(45).Width = 30;


                    // tình huống
                    workSheet.Cells[6, 46].Value = "Tình huống tiếp xúc / Contact situation";
                    workSheet.Cells[6, 46, 7, 46].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 46, 7, 46].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 46, 7, 46].Style.Font.Size = 11;
                    workSheet.Cells[6, 46, 7, 46].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 46, 7, 46].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(46).Width = 30;

                    // Ghi chú
                    workSheet.Cells[6, 47].Value = "Ghi chú/ Notes";
                    workSheet.Cells[6, 47, 7, 47].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 47, 7, 47].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 47, 7, 47].Style.Font.Size = 11;
                    workSheet.Cells[6, 47, 7, 47].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 47, 7, 47].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(47).Width = 10;

                    // 3.3 Lộ trình tiếp xúc với đồng nghiệp/ Routes of contacting with colleagues 
                    workSheet.Cells[5, 48].Value = "3.3. LỘ TRÌNH TIẾP XÚC VỚI ĐỒNG NGHIỆP/ ROUTES OF CONTACTING WITH COLLEAGUES";
                    workSheet.Cells[5, 48, 5, 54].Merge = true; //Merge columns start and end range
                    workSheet.Cells[5, 48, 5, 54].Style.Font.Bold = false;
                    workSheet.Cells[5, 48, 5, 54].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[5, 48, 5, 54].Style.Font.Size = 11;
                    workSheet.Cells[5, 48, 5, 54].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[5, 48, 5, 54].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[5, 48, 5, 54].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);

                    // Từ Ngày & giờ đi
                    workSheet.Cells[6, 48].Value = "Ngày & Giờ / Date & Time";
                    workSheet.Cells[6, 48, 7, 48].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 48, 7, 48].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 48, 7, 48].Style.Font.Size = 11;
                    workSheet.Cells[6, 48, 7, 48].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 48, 7, 48].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(48).Width = 15;

                    // Đến Ngày & giờ đi
                    workSheet.Cells[6, 49].Value = "Đến (Ngày & Giờ)/ To (Date & Time)";
                    workSheet.Cells[6, 49, 7, 49].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 49, 7, 49].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 49, 7, 49].Style.Font.Size = 11;
                    workSheet.Cells[6, 49, 7, 49].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 49, 7, 49].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(49).Width = 15;

                    //Cơ sở của đồng nghiệp
                    workSheet.Cells[6, 50].Value = "Cơ sở của đồng nghiệp / Colleague's Campus";
                    workSheet.Cells[6, 50, 7, 50].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 50, 7, 50].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 50, 7, 50].Style.Font.Size = 11;
                    workSheet.Cells[6, 50, 7, 50].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 50, 7, 50].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(50).Width = 15;

                    // Họ tên đồng nghiệp & chức danh
                    workSheet.Cells[6, 51].Value = " Mã NV & Họ tên đồng nghiệp & chức danh / Colleague's Name - Position - EME Code";
                    workSheet.Cells[6, 51, 7, 51].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 51, 7, 51].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 51, 7, 51].Style.Font.Size = 11;
                    workSheet.Cells[6, 51, 7, 51].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 51, 7, 51].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(51).Width = 25;

                    // địa điểm tiếp xúc
                    workSheet.Cells[6, 52].Value = "Địa điểm tiếp xúc / Contact Location";
                    workSheet.Cells[6, 52, 7, 52].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 52, 7, 52].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 52, 7, 52].Style.Font.Size = 11;
                    workSheet.Cells[6, 52, 7, 52].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 52, 7, 52].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(52).Width = 30;

                    // tình huống
                    workSheet.Cells[6, 53].Value = "Tình huống tiếp xúc  / Contact situation";
                    workSheet.Cells[6, 53, 7, 53].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 53, 7, 53].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 53, 7, 53].Style.Font.Size = 11;
                    workSheet.Cells[6, 53, 7, 53].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 53, 7, 53].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(53).Width = 30;

                    // Ghi chú
                    workSheet.Cells[6, 54].Value = "Ghi chú/ Notes";
                    workSheet.Cells[6, 54, 7, 54].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 54, 7, 54].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 54, 7, 54].Style.Font.Size = 11;
                    workSheet.Cells[6, 54, 7, 54].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 54, 7, 54].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(54).Width = 10;


                    /* End of 3. */

                    /* Start of 4. */

                    //THÔNG TIN KHAI BÁO VỚI CƠ QUAN Y TẾ / DECLARATION TO MEDICAL CENTER 
                    workSheet.Cells[4, 55].Value = "4. THÔNG TIN KHAI BÁO VỚI CƠ QUAN Y TẾ / DECLARATION TO MEDICAL CENTER";
                    workSheet.Cells[4, 55, 4, 67].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 55, 4, 67].Style.Font.Bold = true;
                    workSheet.Cells[4, 55, 4, 67].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 55, 4, 67].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 55, 4, 67].Style.Font.Size = 11;
                    workSheet.Cells[4, 55, 4, 67].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 55, 4, 67].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[4, 55, 4, 67].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);

                    // Thời gian liên hệ / Contact time 
                    workSheet.Cells[5, 55].Value = "4.1 Thời gian liên hệ / Contact time";
                    workSheet.Cells[5, 55, 7, 55].Merge = true; //Merge columns start and end range
                    workSheet.Cells[5, 55, 7, 55].Style.Font.Bold = true;
                    workSheet.Cells[5, 55, 7, 55].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[5, 55, 7, 55].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[5, 55, 7, 55].Style.Font.Size = 11;
                    workSheet.Cells[5, 55, 7, 55].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[5, 55, 7, 55].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[5, 55, 7, 55].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);
                    workSheet.Column(55).Width = 15;

                    // Tên người tiếp nhận / information received by
                    workSheet.Cells[5, 56].Value = "4.2 Tên người tiếp nhận / information received by";
                    workSheet.Cells[5, 56, 7, 56].Merge = true; //Merge columns start and end range
                    workSheet.Cells[5, 56, 7, 56].Style.Font.Bold = true;
                    workSheet.Cells[5, 56, 7, 56].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[5, 56, 7, 56].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[5, 56, 7, 56].Style.Font.Size = 11;
                    workSheet.Cells[5, 56, 7, 56].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[5, 56, 7, 56].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[5, 56, 7, 56].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);
                    workSheet.Column(56).Width = 20;

                    // Số điện thoại/ Tên cơ sở y tế & địa chỉ đã liên hệ tư vấn / Contact Number/Name & address of Medical Center: 
                    workSheet.Cells[5, 57].Value = "4.3 Số điện thoại/ Tên cơ sở y tế & địa chỉ đã liên hệ tư vấn / Contact Number/Name & address of Medical Center:";
                    workSheet.Cells[5, 57, 7, 57].Merge = true; //Merge columns start and end range
                    workSheet.Cells[5, 57, 7, 57].Style.Font.Bold = true;
                    workSheet.Cells[5, 57, 7, 57].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[5, 57, 7, 57].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[5, 57, 7, 57].Style.Font.Size = 11;
                    workSheet.Cells[5, 57, 7, 57].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[5, 57, 7, 57].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[5, 57, 7, 57].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);
                    workSheet.Column(57).Width = 15;

                    // Hướng dẫn từ Cán bộ y tế / Guidance from Medical Center:
                    workSheet.Cells[5, 58].Value = "4.4 Hướng dẫn từ Cán bộ y tế / Guidance from Medical Center:";
                    workSheet.Cells[5, 58, 7, 58].Merge = true; //Merge columns start and end range
                    workSheet.Cells[5, 58, 7, 58].Style.Font.Bold = true;
                    workSheet.Cells[5, 58, 7, 58].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[5, 58, 7, 58].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[5, 58, 7, 58].Style.Font.Size = 11;
                    workSheet.Cells[5, 58, 7, 58].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[5, 58, 7, 58].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[5, 58, 7, 58].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);
                    workSheet.Column(58).Width = 20;

                    // Loại F được xác định bởi cơ quan y tế / F type confirmed by the Medical Center:
                    workSheet.Cells[5, 59].Value = "4.5 Loại F được xác định bởi cơ quan y tế / F type confirmed by the Medical Center:";
                    workSheet.Cells[5, 59, 7, 59].Merge = true; //Merge columns start and end range
                    workSheet.Cells[5, 59, 7, 59].Style.Font.Bold = true;
                    workSheet.Cells[5, 59, 7, 59].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[5, 59, 7, 59].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[5, 59, 7, 59].Style.Font.Size = 11;
                    workSheet.Cells[5, 59, 7, 59].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[5, 59, 7, 59].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[5, 59, 7, 59].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);
                    workSheet.Column(59).Width = 10;

                    //Thuộc đối tượng phải xét nghiệm Covid/ Confirmation to have a test:
                    workSheet.Cells[5, 60].Value = "4.6 Thuộc đối tượng phải xét nghiệm Covid/ Confirmation to have a test:";
                    workSheet.Cells[5, 60, 7, 60].Merge = true; //Merge columns start and end range
                    workSheet.Cells[5, 60, 7, 60].Style.Font.Bold = true;
                    workSheet.Cells[5, 60, 7, 60].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[5, 60, 7, 60].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[5, 60, 7, 60].Style.Font.Size = 11;
                    workSheet.Cells[5, 60, 7, 60].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[5, 60, 7, 60].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[5, 60, 7, 60].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);
                    workSheet.Column(60).Width = 10;

                    //Tình trạng xét nghiệm (nếu thuộc đối tượng XN) Testing status(for those who need to have a test)
                    workSheet.Cells[5, 61].Value = "4.7 Tình trạng xét nghiệm (nếu thuộc đối tượng XN) Testing status(for those who need to have a test)";
                    workSheet.Cells[5, 61, 5, 64].Merge = true; //Merge columns start and end range
                    workSheet.Cells[5, 61, 5, 64].Style.Font.Bold = true;
                    workSheet.Cells[5, 61, 5, 64].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[5, 61, 5, 64].Style.Font.Size = 11;
                    workSheet.Cells[5, 61, 5, 64].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[5, 61, 5, 64].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[5, 61, 5, 64].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);

                    // Đã xét nghiệm / Tested
                    workSheet.Cells[6, 61].Value = "Đã xét nghiệm/ Tested";
                    workSheet.Cells[6, 61, 7, 61].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 61, 7, 61].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 61, 7, 61].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 61, 7, 61].Style.Font.Size = 11;
                    workSheet.Cells[6, 61, 7, 61].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 61, 7, 61].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(61).Width = 10;

                    // Đã có lịch nhưng chưa đến ngày hẹn/ Ghi rõ ngày hẹn XN/ Scheduled but not yet time/Appointment date
                    workSheet.Cells[6, 62].Value = "Đã có lịch nhưng chưa đến ngày hẹn/ Ghi rõ ngày hẹn XN/ Scheduled but not yet time/Appointment date";
                    workSheet.Cells[6, 62, 7, 62].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 62, 7, 62].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 62, 7, 62].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 62, 7, 62].Style.Font.Size = 11;
                    workSheet.Cells[6, 62, 7, 62].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 62, 7, 62].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(62).Width = 10;

                    // Chưa xét nghiệm do chưa nằm trong dánh sách ưu tiên / Not yet tested because not in the priority list
                    workSheet.Cells[6, 63].Value = "Chưa xét nghiệm do chưa nằm trong dánh sách ưu tiên / Not yet tested because not in the priority list";
                    workSheet.Cells[6, 63, 7, 63].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 63, 7, 63].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 63, 7, 63].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 63, 7, 63].Style.Font.Size = 11;
                    workSheet.Cells[6, 63, 7, 63].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 63, 7, 63].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(63).Width = 10;

                    // Khác/ Others
                    workSheet.Cells[6, 64].Value = "Khác/ Others";
                    workSheet.Cells[6, 64, 7, 64].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 64, 7, 64].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 64, 7, 64].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 64, 7, 64].Style.Font.Size = 11;
                    workSheet.Cells[6, 64, 7, 64].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 64, 7, 64].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(64).Width = 10;

                    //Kết quả xét nghiệm/ Test result
                    workSheet.Cells[5, 65].Value = "4.8 Kết quả xét nghiệm/ Test result";
                    workSheet.Cells[5, 65, 5, 67].Merge = true; //Merge columns start and end range
                    workSheet.Cells[5, 65, 5, 67].Style.Font.Bold = true;
                    workSheet.Cells[5, 65, 5, 67].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[5, 65, 5, 67].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[5, 65, 5, 67].Style.Font.Size = 11;
                    workSheet.Cells[5, 65, 5, 67].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[5, 65, 5, 67].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[5, 65, 5, 67].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);

                    // Đã có/ Yes
                    workSheet.Cells[6, 65].Value = "Đã có/ Yes";
                    workSheet.Cells[6, 65, 7, 65].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 65, 7, 65].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 65, 7, 65].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 65, 7, 65].Style.Font.Size = 11;
                    workSheet.Cells[6, 65, 7, 65].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 65, 7, 65].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(65).Width = 10;

                    // Đang chờ kết quả/ Waiting for the result
                    workSheet.Cells[6, 66].Value = "Đang chờ kết quả/ Waiting for the result";
                    workSheet.Cells[6, 66, 7, 66].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 66, 7, 66].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 66, 7, 66].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 66, 7, 66].Style.Font.Size = 11;
                    workSheet.Cells[6, 66, 7, 66].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 66, 7, 66].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(66).Width = 10;

                    // Đính kèm kết quả/ Enclosed test result
                    workSheet.Cells[6, 67].Value = "Đính kèm kết quả/ Enclosed test result";
                    workSheet.Cells[6, 67, 7, 67].Merge = true; //Merge columns start and end range
                    workSheet.Cells[6, 67, 7, 67].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[6, 67, 7, 67].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[6, 67, 7, 67].Style.Font.Size = 11;
                    workSheet.Cells[6, 67, 7, 67].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[6, 67, 7, 67].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                    workSheet.Column(67).Width = 10;

                    /*End of 4. */


                    /* Start of 5. */
                    // TÌNH TRẠNG SỨC KHỎE HIỆN TẠI
                    workSheet.Cells[4, 68].Value = "5. TÌNH TRẠNG SỨC KHỎE HIỆN TẠI / CURRENT HEALTH SITUATION";
                    workSheet.Cells[4, 68, 7, 68].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 68, 7, 68].Style.Font.Bold = true;
                    workSheet.Cells[4, 68, 7, 68].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[4, 68, 7, 68].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 68, 7, 68].Style.Font.Size = 11;
                    workSheet.Cells[4, 68, 7, 68].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 68, 7, 68].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[4, 68, 7, 68].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(68).Width = 25;

                    /* End of 5 */


                    /* Start of 6. */
                    // 6. NGÀY TRỞ LẠI LÀM VIỆC THEO THỰC TẾ / DATE BACK TO WORK
                    workSheet.Cells[4, 69].Value = "6. NGÀY TRỞ LẠI LÀM VIỆC THEO THỰC TẾ / DATE BACK TO WORK";
                    workSheet.Cells[4, 69, 4, 72].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 69, 4, 72].Style.Font.Bold = true;
                    workSheet.Cells[4, 69, 4, 72].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[4, 69, 4, 72].Style.Font.Size = 11;
                    workSheet.Cells[4, 69, 4, 72].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 69, 4, 72].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[4, 69, 4, 72].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);

                    //   Đã trở lại/ Back to work
                    workSheet.Cells[5, 69].Value = "Đã trở lại/ Back to work";
                    workSheet.Cells[5, 69, 7, 69].Merge = true; //Merge columns start and end range
                    workSheet.Cells[5, 69, 7, 69].Style.Font.Bold = true;
                    workSheet.Cells[5, 69, 7, 69].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[5, 69, 7, 69].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[5, 69, 7, 69].Style.Font.Size = 11;
                    workSheet.Cells[5, 69, 7, 69].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[5, 69, 7, 69].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[5, 69, 7, 69].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);
                    workSheet.Column(69).Width = 10;


                    // Chưa trở lại/ Not yet back to work
                    workSheet.Cells[5, 70].Value = "Chưa trở lại/ Not yet back to work";
                    workSheet.Cells[5, 70, 7, 70].Merge = true; //Merge columns start and end range
                    workSheet.Cells[5, 70, 7, 70].Style.Font.Bold = true;
                    workSheet.Cells[5, 70, 7, 70].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[5, 70, 7, 70].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[5, 70, 7, 70].Style.Font.Size = 11;
                    workSheet.Cells[5, 70, 7, 70].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[5, 70, 7, 70].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[5, 70, 7, 70].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);
                    workSheet.Column(70).Width = 10;


                    //Ngày trở lại làm việc / Date back to work
                    workSheet.Cells[5, 71].Value = "Ngày trở lại làm việc / Date back to work";
                    workSheet.Cells[5, 71, 7, 71].Merge = true; //Merge columns start and end range
                    workSheet.Cells[5, 71, 7, 71].Style.Font.Bold = true;
                    workSheet.Cells[5, 71, 7, 71].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[5, 71, 7, 71].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[5, 71, 7, 71].Style.Font.Size = 11;
                    workSheet.Cells[5, 71, 7, 71].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[5, 71, 7, 71].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[5, 71, 7, 71].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);
                    workSheet.Column(71).Width = 15;


                    // Ngày dự kiến trở lại làm việc / Estimated date back to work
                    workSheet.Cells[5, 72].Value = "Ngày dự kiến trở lại làm việc / Estimated date back to work";
                    workSheet.Cells[5, 72, 7, 72].Merge = true; //Merge columns start and end range
                    workSheet.Cells[5, 72, 7, 72].Style.Font.Bold = true;
                    workSheet.Cells[5, 72, 7, 72].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[5, 72, 7, 72].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[5, 72, 7, 72].Style.Font.Size = 11;
                    workSheet.Cells[5, 72, 7, 72].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[5, 72, 7, 72].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[5, 72, 7, 72].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);
                    workSheet.Column(72).Width = 15;


                    /* End of 6. */

                    //Comment
                    workSheet.Cells[4, 73].Value = "Note";
                    workSheet.Cells[4, 73, 7, 73].Merge = true; //Merge columns start and end range
                    workSheet.Cells[4, 73, 7, 73].Style.Font.Bold = true;
                    workSheet.Cells[4, 73, 7, 73].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 73, 7, 73].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[4, 73, 7, 73].Style.Font.Size = 11;
                    workSheet.Cells[4, 73, 7, 73].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[4, 73, 7, 73].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[4, 73, 7, 73].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(73).Width = 25;

                    //Following list
                    workSheet.Cells[8, 1].Value = "1. FOLLOWING LIST";
                    workSheet.Cells[8, 1, 8, 73].Merge = true; //Merge columns start and end range
                    workSheet.Cells[8, 1, 8, 73].Style.Font.Bold = true;
                    workSheet.Cells[8, 1, 8, 73].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[8, 1, 8, 73].Style.Font.Size = 11;
                    workSheet.Cells[8, 1, 8, 73].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[8, 1, 8, 73].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[8, 1, 8, 73].Style.Fill.BackgroundColor.SetColor(Color.Red);

                    /*//Unfollowing list
                    workSheet.Cells[13, 1].Value = "2. UNFOLLOWING LIST";
                    workSheet.Cells[13, 1, 13, 54].Merge = true; //Merge columns start and end range
                    workSheet.Cells[13, 1, 13, 54].Style.Font.Bold = true;
                    workSheet.Cells[13, 1, 13, 54].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[13, 1, 13, 54].Style.Font.Size = 11;
                    workSheet.Cells[13, 1, 13, 54].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[13, 1, 13, 54].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[13, 1, 13, 54].Style.Fill.BackgroundColor.SetColor(Color.YellowGreen);

                    //Unrelated list
                    workSheet.Cells[17, 1].Value = "3. UNRELATED LIST";
                    workSheet.Cells[17, 1, 17, 54].Merge = true; //Merge columns start and end range
                    workSheet.Cells[17, 1, 17, 54].Style.Font.Bold = true;
                    workSheet.Cells[17, 1, 17, 54].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alignment is center
                    workSheet.Cells[17, 1, 17, 54].Style.Font.Size = 11;
                    workSheet.Cells[17, 1, 17, 54].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[17, 1, 17, 54].Style.Font.Color.SetColor(Color.White);
                    workSheet.Cells[17, 1, 17, 54].Style.Fill.BackgroundColor.SetColor(Color.Cyan);
        */
                    int rowData = 9;
                    var orderNumber = 1;
                    if (listAllRequest == null)
                    {
                        return null;
                    }
                    else if (listAllRequest != null)
                    {
                        listAllRequest = listAllRequest.OrderBy(t => t.departureDate).ToList();
                    }

                    foreach (var item in listAllRequest)
                    {

                        var routeAtRedZones = item.routesAtRedZones.Count();
                        var routeOfContactingWithColleagues = item.routesOfContactingWithColleagues.Count();
                        var routeOutsizedRedZones = item.routesOutsizeRedZones.Count();
                        var infoContactSuspect = item.informationContactSuspectCaseCovid.Count();


                        var listIncidentReport = Math.Max(Math.Max(Math.Max(routeAtRedZones, routeOfContactingWithColleagues), routeOutsizedRedZones), infoContactSuspect);



                        // GENERAL INFO 

                        workSheet.Cells[rowData, 1, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 1].Value = orderNumber;
                        workSheet.Cells[rowData, 1, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 1].Merge = true;


                        workSheet.Cells[rowData, 2, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 2].Value = item.request_id;
                        workSheet.Cells[rowData, 2, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 2].Merge = true;
                        workSheet.Cells[rowData, 2, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 2, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                       /* if (item.CancelStatus != null)
                        {
                            workSheet.Cells[rowData, 3, listTravelRoutes > 1 ? (listTravelRoutes + rowData) - 1 : rowData, 3].Value = EnumExtensions.GetDisplayName((RequestStatus)item.CancelStatus);
                        }
                        else */
                        if (item.ECSDVerifyStatus != null)
                        {
                            workSheet.Cells[rowData, 3, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 3].Value = EnumExtensions.GetDisplayName((RequestStatus)item.ECSDVerifyStatus);
                        }
                        else
                        {
                            workSheet.Cells[rowData, 3, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 3].Value = EnumExtensions.GetDisplayName((RequestStatus)item.status);
                        }
                        workSheet.Cells[rowData, 3, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 3].Merge = true;
                        workSheet.Cells[rowData, 3, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 3, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 4, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 4].Value = item.createdOn != null ? item.createdOn.Value.ToString("dd/MM/yyyy") : "";
                        workSheet.Cells[rowData, 4, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 4].Merge = true;
                        workSheet.Cells[rowData, 4, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 4, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 5, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 5].Value = item.dateOfStatus != null ? item.dateOfStatus.Value.ToString("dd/MM/yyyy") : "";
                        workSheet.Cells[rowData, 5, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 5].Merge = true;
                        workSheet.Cells[rowData, 5, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 5, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 6, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 6].Value = item.ECSDCommentDate != null ? item.ECSDCommentDate.Value.ToString("dd/MM/yyyy") : "";
                        workSheet.Cells[rowData, 6, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 6].Merge = true;
                        workSheet.Cells[rowData, 6, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 6, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 7, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 7].Value = "";
                        workSheet.Cells[rowData, 7, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 7].Merge = true;
                        workSheet.Cells[rowData, 7, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 7, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 8, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 8].Value = item.Requester.UserCode.Substring(0, 3).Contains("FEM") || item.Requester.UserCode.Substring(0, 3).Contains("VEM") ? "CAM" : "MOET";
                        workSheet.Cells[rowData, 8, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 8].Merge = true;
                        workSheet.Cells[rowData, 8, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 8, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 9, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 9].Merge = true;


                        workSheet.Cells[rowData, 10, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 10].Value = item.Requester.UserCode;
                        workSheet.Cells[rowData, 10, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 10].Merge = true;
                        workSheet.Cells[rowData, 10, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 10, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 10].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 11, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 11].Value = item.Requester.FirstName + " " + item.Requester.LastName;
                        workSheet.Cells[rowData, 11, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 11].Merge = true;
                        workSheet.Cells[rowData, 11, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 11].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 11, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 11].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 12, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 12].Merge = true;

                        workSheet.Cells[rowData, 13, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 13].Merge = true;
                        workSheet.Cells[rowData, 13, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 13].Value = item.Requester.Position;
                        workSheet.Cells[rowData, 13, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 13, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 13].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 14, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 14].Value = item.Requester.Campus;
                        workSheet.Cells[rowData, 14, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 14].Merge = true;
                        workSheet.Cells[rowData, 14, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 14].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 14, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 15, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 15].Value = item.reporterHomeAddress + " " + "-" + " " + await getWardName(item.reporterWardId) + " " + "-" + " " + await getDistrictName(item.reporterDistrictId) + " " + "-" + " " + await getProvinceName(item.reporterProvinceId);
                        workSheet.Cells[rowData, 15, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 15].Merge = true;
                        workSheet.Cells[rowData, 15, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 15].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 15, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 15].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 16, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 16].Value = item.Requester.Mobile;
                        workSheet.Cells[rowData, 16, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 16].Merge = true;
                        workSheet.Cells[rowData, 16, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 16].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 16, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 16].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 17, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 17].Value = item.emergencyContact != null ? item.emergencyContact : "";
                        workSheet.Cells[rowData, 17, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 17].Merge = true;
                        workSheet.Cells[rowData, 17, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 17].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 17, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 17].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 18, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 18].Value = item.phoneContact != null ? item.phoneContact : "";
                        workSheet.Cells[rowData, 18, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 18].Merge = true;
                        workSheet.Cells[rowData, 18, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 18].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 18, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 18].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 19, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 19].Value = item.relationshipContact != null ? item.relationshipContact : "";
                        workSheet.Cells[rowData, 19, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 19].Merge = true;
                        workSheet.Cells[rowData, 19, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 19].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 19, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 19].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 20, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 20].Value = item.contactAddress != null ? item.contactAddress : "";
                        workSheet.Cells[rowData, 20, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 20].Merge = true;
                        workSheet.Cells[rowData, 20, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 20].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 20, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 20].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 21, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 21].Value = item.redZoneProvinceId != null ? await getProvinceName(item.redZoneProvinceId) : "";
                        workSheet.Cells[rowData, 21, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 21].Merge = true;
                        workSheet.Cells[rowData, 21, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 21].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 21, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 21].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 22, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 22].Value = item.redZoneDistrictId != null ? await getDistrictName(item.redZoneDistrictId) : "";
                        workSheet.Cells[rowData, 22, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 22].Merge = true;
                        workSheet.Cells[rowData, 22, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 22].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 22, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 22].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 23, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 23].Value = item.redZoneWardId != null ? await getWardName(item.redZoneWardId) : "";
                        workSheet.Cells[rowData, 23, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 23].Merge = true;
                        workSheet.Cells[rowData, 23, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 23].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 23, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 23].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 24, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 24].Value = item.redZoneHomeAddress != null ? item.redZoneHomeAddress : "";
                        workSheet.Cells[rowData, 24, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 24].Merge = true;
                        workSheet.Cells[rowData, 24, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 24].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 24, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 24].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        // Detail
                        // 2.
                         
                        workSheet.Cells[rowData, 25, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 25].Value = item.departureDate != null ? item.departureDate.Value.ToString("dd/MM/yyyy") : "";
                        workSheet.Cells[rowData, 25, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 25].Merge = true;
                        workSheet.Cells[rowData, 25, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 25].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 25, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 25].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 26].Value = item.departureTransportation;
                        workSheet.Cells[rowData, 26, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 26].Merge = true;
                        workSheet.Cells[rowData, 26, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 26].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 26, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 26].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 27, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 27].Value = item.departureTicket != null ? "X" : "";
                        workSheet.Cells[rowData, 27, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 27].Merge = true;
                        workSheet.Cells[rowData, 27, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 27].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        workSheet.Cells[rowData, 27, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 27].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 28, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 28].Value = item.returningDate != null ? item.returningDate.Value.ToString("dd/MM/yyyy") : "";
                        workSheet.Cells[rowData, 28, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 28].Merge = true;
                        workSheet.Cells[rowData, 28, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 28].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 28, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 28].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 29].Value = item.returningTransportaion;
                        workSheet.Cells[rowData, 29, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 29].Merge = true;
                        workSheet.Cells[rowData, 29, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 29].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 29, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 29].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 30, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 30].Value = item.returningTicket != null ? "X" : "";
                        workSheet.Cells[rowData, 30, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 30].Merge = true;
                        workSheet.Cells[rowData, 30, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 30].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        workSheet.Cells[rowData, 30, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 30].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        //4.

                        workSheet.Cells[rowData, 55, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 55].Value = item.contactTime != null ? item.contactTime.Value.ToString("dd/MM/yyyy") : "";
                        workSheet.Cells[rowData, 55, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 55].Merge = true;
                        workSheet.Cells[rowData, 55, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 55].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 55, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 55].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 56, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 56].Value = item.informationReciever;
                        workSheet.Cells[rowData, 56, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 56].Merge = true;
                        workSheet.Cells[rowData, 56, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 56].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 56, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 56].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 57, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 57].Value = item.informationMedicalCenter;
                        workSheet.Cells[rowData, 57, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 57].Merge = true;
                        workSheet.Cells[rowData, 57, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 57].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 57, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 57].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 58, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 58].Value = item.guidanceMedicalCenter;
                        workSheet.Cells[rowData, 58, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 58].Merge = true;
                        workSheet.Cells[rowData, 58, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 58].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 58, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 58].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 59, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 59].Value = item.fTypeConfirmed;
                        workSheet.Cells[rowData, 59, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 59].Merge = true;
                        workSheet.Cells[rowData, 59, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 59].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 59, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 59].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 60, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 60].Value = (int)item.ConfirmToTest == 1 ? "Yes" : (int)item.ConfirmToTest == 2 ? "No" : "";
                        workSheet.Cells[rowData, 60, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 60].Merge = true;
                        workSheet.Cells[rowData, 60, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 60].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 60, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 60].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        //4.7
                        workSheet.Cells[rowData, 61, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 61].Value = item.testingStatus.GetDisplayName() == "tested" ? "X" : "";
                        workSheet.Cells[rowData, 61, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 61].Merge = true;
                        workSheet.Cells[rowData, 61, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 61].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        workSheet.Cells[rowData, 61, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 61].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 62, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 62].Value = item.testingStatus.GetDisplayName() == "schedule" ? "X" : "";
                        workSheet.Cells[rowData, 62, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 62].Merge = true;
                        workSheet.Cells[rowData, 62, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 62].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        workSheet.Cells[rowData, 62, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 62].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 63, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 63].Value = item.testingStatus.GetDisplayName() == "notYetTested" ? "X" : "";
                        workSheet.Cells[rowData, 63, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 63].Merge = true;
                        workSheet.Cells[rowData, 63, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 63].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        workSheet.Cells[rowData, 63, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 63].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 64, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 64].Value = item.testingStatus.GetDisplayName() == "others" ? "X" : "";
                        workSheet.Cells[rowData, 64, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 64].Merge = true;
                        workSheet.Cells[rowData, 64, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 64].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        workSheet.Cells[rowData, 64, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 64].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        //4.8
                        workSheet.Cells[rowData, 65, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 65].Value = item.testingStatus.GetDisplayName() == "haveTestResult" ? "X" : "";
                        workSheet.Cells[rowData, 65, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 65].Merge = true;
                        workSheet.Cells[rowData, 65, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 65].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        workSheet.Cells[rowData, 65, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 65].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 66, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 66].Value = item.testingStatus.GetDisplayName() == "waitingTestResult" ? "X" : "";
                        workSheet.Cells[rowData, 66, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 66].Merge = true;
                        workSheet.Cells[rowData, 66, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 66].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        workSheet.Cells[rowData, 66, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 66].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 67, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 67].Value = item.testResultPath != null ? "X" : "";
                        workSheet.Cells[rowData, 67, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 67].Merge = true;
                        workSheet.Cells[rowData, 67, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 67].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        workSheet.Cells[rowData, 67, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 67].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        //5.
                        workSheet.Cells[rowData, 68, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 68].Value = item.currentHealthSituation;
                        workSheet.Cells[rowData, 68, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 68].Merge = true;
                        workSheet.Cells[rowData, 68, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 68].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 68, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 68].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        //6.
                        workSheet.Cells[rowData, 69, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 69].Value = item.backToWorkStatus.GetDisplayName() == "backToWorkAlready" ? "X" : "";
                        workSheet.Cells[rowData, 69, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 69].Merge = true;
                        workSheet.Cells[rowData, 69, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 69].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        workSheet.Cells[rowData, 69, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 69].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 70, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 70].Value = item.backToWorkStatus.GetDisplayName() == "notYetBackToWork" ? "X" : "";
                        workSheet.Cells[rowData, 70, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 70].Merge = true;
                        workSheet.Cells[rowData, 70, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 70].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        workSheet.Cells[rowData, 70, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 70].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 71, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 71].Value = item.dateBackToWork != null ? item.dateBackToWork.Value.ToString("dd/MM/yyyy") : "";
                        workSheet.Cells[rowData, 71, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 71].Merge = true;
                        workSheet.Cells[rowData, 71, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 71].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 71, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 71].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 72, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 72].Value = item.estimatedDateBackToWork != null ? item.estimatedDateBackToWork.Value.ToString("dd/MM/yyyy") : "";
                        workSheet.Cells[rowData, 72, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 72].Merge = true;
                        workSheet.Cells[rowData, 72, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 72].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 72, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 72].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 73, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 73].Value = item.comment;
                        workSheet.Cells[rowData, 73, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 73].Merge = true;
                        workSheet.Cells[rowData, 73, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 73].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 73, listIncidentReport > 1 ? (listIncidentReport + rowData) - 1 : rowData, 73].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        //3. 

                        //3.1.1
                        int initRow = rowData;

                        foreach (var itemRoute in item.routesAtRedZones)
                        {

                            var lastItem = item.routesAtRedZones.LastOrDefault().routesAtRedZonesId;
                            workSheet.Cells[rowData, 31].Value = itemRoute.routeAtRedZoneDate != null ? itemRoute.routeAtRedZoneDate.ToString("dd/MM/yyyy") : "";
                            workSheet.Cells[rowData, 32].Value = ""; //toDate
                            workSheet.Cells[rowData, 33].Value = itemRoute.routeAtRedZoneFullAddress;
                            workSheet.Cells[rowData, 34].Value = itemRoute.routeAtRedZoneDatetransportation;
                            workSheet.Cells[rowData, 35].Value = ""; //notes

                            if (item.routesAtRedZones.Count > 1)
                            {
                                rowData++;
                            }

                        }

                        rowData = initRow;


                        //3.1.2
                        foreach (var itemRoute in item.routesOutsizeRedZones)
                        {
                            var lastItem = item.routesOutsizeRedZones.LastOrDefault().routesOutsizeRedZonesId;
                            workSheet.Cells[rowData, 36].Value = itemRoute.routeOutsideRedZoneDate != null ? itemRoute.routeOutsideRedZoneDate.ToString("dd/MM/yyyy") : "";
                            workSheet.Cells[rowData, 37].Value = ""; //toDate
                            workSheet.Cells[rowData, 38].Value = itemRoute.routeOutsideRedZoneFullAddress;
                            workSheet.Cells[rowData, 39].Value = itemRoute.routeOutsideRedZoneDatetransportation;
                            workSheet.Cells[rowData, 40].Value = ""; //notes

                            if (item.routesOutsizeRedZones.Count > 1)
                            {
                                rowData++;
                            }


                        }
                        rowData = initRow;



                        //3.2.1
                        foreach (var itemRoute in item.informationContactSuspectCaseCovid)
                        {
                            var lastItem = item.informationContactSuspectCaseCovid.LastOrDefault().InformationContactSuspectCaseCovidId;
                            workSheet.Cells[rowData, 41].Value = itemRoute.suspectCaseDate != null ? itemRoute.suspectCaseDate.ToString("dd/MM/yyyy") : "";
                            workSheet.Cells[rowData, 42].Value = ""; //toDate
                            workSheet.Cells[rowData, 43].Value = itemRoute.suspectCase;
                            workSheet.Cells[rowData, 44].Value = itemRoute.suspectCaseFullAddress;
                            workSheet.Cells[rowData, 45].Value = itemRoute.suspectCaseRelationShip;
                            workSheet.Cells[rowData, 46].Value = itemRoute.suspectCaseContactSituation;
                            workSheet.Cells[rowData, 47].Value = ""; //notes

                            if (item.informationContactSuspectCaseCovid.Count > 1)
                            {
                                rowData++;
                            }

                        }
                        rowData = initRow;

                        //3.3
                        foreach (var itemRoute in item.routesOfContactingWithColleagues)
                        {
                            var lastItem = item.routesOfContactingWithColleagues.LastOrDefault().routesOfContactingWithColleaguesId;
                            workSheet.Cells[rowData, 48].Value = itemRoute.routesOfContactingWithColleaguesDate != null ? itemRoute.routesOfContactingWithColleaguesDate.ToString("dd/MM/yyyy") : "";
                            workSheet.Cells[rowData, 49].Value = ""; //toDate
                            workSheet.Cells[rowData, 50].Value = itemRoute.routesOfContactingWithColleaguesCampus;
                            workSheet.Cells[rowData, 51].Value = itemRoute.routesOfContactingWithColleaguesInfor;
                            workSheet.Cells[rowData, 52].Value = itemRoute.routesOfContactingWithColleaguesFullAddress;
                            workSheet.Cells[rowData, 53].Value = itemRoute.routesOfContactingWithColleaguesContactSituation;
                            workSheet.Cells[rowData, 54].Value = ""; //notes

                            if (item.routesOfContactingWithColleagues.Count > 1)
                            {
                                rowData++;
                            }

                        }
                        rowData = initRow + listIncidentReport;


                        orderNumber++;
                    }

                    workSheet.Cells[workSheet.Dimension.Address].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    workSheet.Cells[workSheet.Dimension.Address].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    workSheet.Cells[workSheet.Dimension.Address].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    workSheet.Cells[workSheet.Dimension.Address].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                    workSheet.Cells[workSheet.Dimension.Address].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheet.Cells[workSheet.Dimension.Address].Style.WrapText = true;

                    workSheet.Column(7).Hidden = true;
                    workSheet.Column(32).Hidden = true;
                    workSheet.Column(37).Hidden = true;
                    workSheet.Column(42).Hidden = true;
                    workSheet.Column(49).Hidden = true;



                    workSheet.Row(6).Height = 40;
                    workSheet.Row(7).Height = 40;

                    //workSheet.Cells[workSheet.Dimension.Address].AutoFitColumns();
                    //workSheet.Cells.AutoFitColumns();


                package.Save();
                }
                stream.Position = 0;

                string excelName = $"CovidIncidentReport-Report-{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.xlsx";
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
