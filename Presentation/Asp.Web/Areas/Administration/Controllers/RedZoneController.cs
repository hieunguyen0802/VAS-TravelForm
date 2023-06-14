using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using src.Emails;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using src.Core;
using src.Core.Domains;
using src.Core.Data;
using src.Repositories.Category;
using src.Repositories.RedZone;
using src.Web.Common;
using src.Web.Common.Models.RedZone;
using src.Core.Enums;
using src.Web.Extensions;
using src.Web.Common.Mvc.Alerts;
using Microsoft.AspNetCore.Identity.UI.Services;
using src.Repositories.Configs;
using MimeKit;
using IEmailSender = src.Emails.IEmailSender;
using Hangfire;
using src.Repositories.TravelDeclarations;
using src.Data;
using src.Repositories.IncidentReports;
using Microsoft.AspNetCore.Http;

namespace src.Web.Areas.Administration.Controllers
{
    [Area(Constants.Areas.Administration)]
    [Authorize(Policy = Constants.RoleNames.Administrator)]

    public class RedZoneController : Controller
    {
        private readonly IDateTime _dateTime;
        private readonly IMapper _mapper;
        private readonly ILogger<ReportController> _logger;
        private readonly IUserSession _userSession;
        private readonly IHostingEnvironment _hostingEnvironment;
        private IAuthorizationService _authorizationService;
        private readonly IRedZoneRepo _redZoneRepo;
        private readonly IBaseCategoryRepository _baseCategoryRepository;
        private IEmailSender _emailSender;
        private readonly IBackgroundJobClient _backgroungJobClient;
        private IEmailConfigRepository _emailConfigRepository;
        private readonly ITravelDeclarationRepository _travelDeclarationRepository;
        private readonly IIncidentReportRepository _incidentReportRepository;
        private readonly IDbContext _dbContext;
        private string _fileName;
        private string _filePath;


        public RedZoneController(
            IDateTime dateTime,
            IMapper mapper,
            ILogger<ReportController> logger,
            IUserSession userSession,
            IHostingEnvironment hostingEnvironment,
            IAuthorizationService authorizationService,
            IBaseCategoryRepository baseCategoryRepository,
            IRedZoneRepo redZoneRepo,
            IBackgroundJobClient backgroungJobClient,
            IEmailConfigRepository emailConfigRepository,
            IEmailSender emailSender,
            ITravelDeclarationRepository travelDeclarationRepository,
            IIncidentReportRepository incidentReportRepository,
            IDbContext dbContext

            )
        {
            _dateTime = dateTime;
            _mapper = mapper;
            _logger = logger;
            _userSession = userSession;
            _hostingEnvironment = hostingEnvironment;
            _authorizationService = authorizationService;
            _baseCategoryRepository = baseCategoryRepository;
            _backgroungJobClient = backgroungJobClient;
            _redZoneRepo = redZoneRepo;
            _emailConfigRepository = emailConfigRepository;
            _emailSender = emailSender;
            _travelDeclarationRepository = travelDeclarationRepository;
            _dbContext = dbContext;
            _incidentReportRepository = incidentReportRepository;
            _fileName = $"RedZoneFiltered-Report.xlsx";
            _filePath = string.Format("{0}/{1}", _hostingEnvironment.WebRootPath, _fileName);
        }
        

        public async Task<IActionResult> Filter(RedZoneModel model)
        {
            model.Provinces = new SelectList(await _baseCategoryRepository.GetProvinces(), "provinceId", "name");
            model.Countries = new SelectList(await _baseCategoryRepository.GetCountries(), "countryId", "name");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create(RedZoneModel model)
        {
            model.Provinces = new SelectList(await _baseCategoryRepository.GetProvinces(), "provinceId", "name");
            model.Countries = new SelectList(await _baseCategoryRepository.GetCountries(), "countryId", "name");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRedZone(RedZoneModel model)
        {
            try
            {
                var redZone = _mapper.Map<RedZoneModel, redZone>(model);
                redZone.redZoneId = Guid.NewGuid();
                redZone.createdAt = DateTime.Now;
                redZone.isActivate = false;
                redZone.isSendEmail = false;

                if (redZone.redZoneProvinceId != null )
                {
                    redZone.redZoneCountryId= null;
                    redZone.redZoneCity = null;
                }
                if (redZone.redZoneCountryId != null)
                {
                    redZone.redZoneProvinceId = null;
                    redZone.redZoneDistrictId = null;
                    redZone.redZoneWardId = null;
                }
                await _redZoneRepo.insert(redZone);
               
                return RedirectToAction("Filter").WithSuccess("Red Zone created successfully !");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw;
            }


        }

        public async Task<IActionResult> FilterRedZone(RedZoneModel model)
        {
            try
            {
                var entities = await _redZoneRepo.filterRedZone(model);
                foreach (var item in entities)
                {
                    item.redZoneProvinceId = await getProvinceName(item.redZoneProvinceId);
                    item.redZoneDistrictId = await getDistrictName(item.redZoneDistrictId);
                    item.redZoneWardId = await getWardName(item.redZoneWardId);
                    item.redZoneCountryId = await getCountryName(item.redZoneCountryId);
                }

                return Json(new { data = entities });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw ex;
            }
            
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var entity = _redZoneRepo.GetRedZonesById(id);
                if (entity == null)
                    return RedirectToAction("Index");
                var model = _mapper.Map<redZone,RedZoneModel>(entity);
                model.Provinces = new SelectList(await _baseCategoryRepository.GetProvinces(), "provinceId", "name");
                model.Countries = new SelectList(await _baseCategoryRepository.GetCountries(), "countryId", "name");
                model.Districts = new SelectList(await _baseCategoryRepository.GetDistrict(), "districtId", "name");
                model.Wards = new SelectList(await _baseCategoryRepository.GetWard(), "wardId", "name");

                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw ex;
            }

        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ActivateRedZone(Guid id)
        {
            try
            {
                var redZone = _redZoneRepo.GetRedZonesById(id);
                if (redZone.isActivate == true)
                {
                    redZone.isActivate = false;
                    _redZoneRepo.updateRedZone(redZone);
                    return Json(new { success = true, message = "Red Zone has been deactivated" });
                }
                else
                {
                    redZone.isActivate = true;
                    _redZoneRepo.updateRedZone(redZone);
                    return Json(new { success = true, message = "Red Zone has been activated" });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw ex;
            }
        }

        // filter by travel route
        public async Task<IActionResult> GetTravelRouteByRedZone(Guid id)
        {
            try
            {
                var allFollowUps = await _redZoneRepo.listAllFollowUp();
                foreach (var item in allFollowUps)
                {

                }
                var entities = await _redZoneRepo.FilterTravelRouteByRedZone(id);
                var modelMapper = entities.Select(e => _mapper.Map<TravelDeclaration, RedZoneFollowUp>(e)).FirstOrDefault();

               
                var redZone = _redZoneRepo.GetRedZonesById(id);
                if (entities != null)
                {
                    string host = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
                    foreach (var item in entities)
                    {
                        item.redZoneId = id;
                        await _travelDeclarationRepository.updateTravelDeclaration(item);

                        if (redZone.isSendEmail == false)
                        {
                            _backgroungJobClient.Enqueue(() => SendEmail(item, host, Guid.Empty));
                           
                        }
                        
                        //add info to follow up 
                        if (item.incidentId != null)
                        {
                            modelMapper.RedZoneFollowUpId = new Guid();
                            modelMapper.RequestId = item.request_id;
                            modelMapper.Employee = item.Requester.LastName + " " + item.Requester.FirstName;
                            modelMapper.submittedDate = item.createdOn;
                            modelMapper.Position = item.Requester.Position;
                            modelMapper.Campus = item.Requester.Campus;
                            modelMapper.status = item.LatestStatus;
                            var incident = await _incidentReportRepository.getIncidentReportById(item.incidentId);
                            modelMapper.incidentRequest = incident.request_id;
                            modelMapper.isFollowUp = true;
                            modelMapper.isRelated = false;
                            modelMapper.travelId = item.TravelDeclarationId;
                            modelMapper.IncidentReportId = item.incidentId;
                            modelMapper.createdOn = _dateTime.Now;

                            modelMapper.FTypeByVas = null;
                            modelMapper.RegulatedAction = null;
                            modelMapper.QuarantineDuration = 0;
                            modelMapper.VasQuarantineDuration = 0;

                            //add to history

                            var covid = await _incidentReportRepository.getIncidentReportById(item.incidentId);
                            if (covid.fTypeConfirmed != null)
                            {
                                modelMapper.FType = covid.fTypeConfirmed;
                            }
                            else
                            {
                                modelMapper.FType = null;
                            }


                            if (allFollowUps.Count(h => h.travelId == item.TravelDeclarationId) == 0)
                            {
                                await _redZoneRepo.insertFollowUp(modelMapper);
                            }
                        } else
                        {
                            modelMapper.RedZoneFollowUpId = new Guid();
                            modelMapper.RequestId = item.request_id;
                            modelMapper.Employee = item.Requester.LastName + " " + item.Requester.FirstName;
                            modelMapper.submittedDate = item.createdOn;
                            modelMapper.Position = item.Requester.Position;
                            modelMapper.Campus = item.Requester.Campus;
                            modelMapper.status = item.LatestStatus;
                            modelMapper.incidentRequest = null;
                            modelMapper.isFollowUp = true;
                            modelMapper.isRelated = false;
                            modelMapper.travelId = item.TravelDeclarationId;
                            modelMapper.IncidentReportId = null;
                            modelMapper.createdOn = _dateTime.Now;

                            modelMapper.FTypeByVas = null;
                            modelMapper.RegulatedAction = null;
                            modelMapper.QuarantineDuration = 0;
                            modelMapper.VasQuarantineDuration = 0;

                            //add to history
                            modelMapper.FType = null;
                            modelMapper.FType = null;


                            if (allFollowUps.Count(h => h.travelId == item.TravelDeclarationId) == 0)
                            {
                                await _redZoneRepo.insertFollowUp(modelMapper);
                            }
                        }

                    }
                    redZone.isSendEmail = true;
                    await _redZoneRepo.updateRedZone(redZone);
                }
                
                return Json(new { data = entities });

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw ex;

            }

        }

        //send email
        public async Task SendEmail(TravelDeclaration travelDetails, string currentUrl, Guid id)
        {
            try
            {
                var emailModel = await _emailConfigRepository.getTemplatesByRequestStatus(10);
                string body = emailModel.forRedzone.Body;
                travelDetails.redZoneId = id;
                body = TravelDeclarationSendEmail.ReplaceMessageTemplateTokensRedZone(travelDetails, currentUrl, body);
                await _emailSender.SendEmailWithVASCredential(travelDetails.Requester.UserName + "@vas.edu.vn", emailModel.forRedzone.Subject, body, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw ex;
            }

        }


        //Export excel filter redzone
        #region excel filter 
        //export excel filter 
        public async Task<IActionResult> ExportExcelFilter(RedZoneModel model)
        {
            var entities = await _redZoneRepo.filterRedZone(model);
            var stream = new MemoryStream();
            try
            {
                using (var package = new ExcelPackage(stream))
                {
                    var workSheet = package.Workbook.Worksheets.Add("Red Zone Filtered");
                    workSheet.Cells.Style.Font.Name = "Cambria";

                    // Header
                    workSheet.Cells[1, 1].Value = "Red Zone List";
                    workSheet.Cells[1, 1, 2, 10].Merge = true; //Merge columns start and end range
                    workSheet.Cells[1, 1, 2, 10].Style.Font.Bold = true; //Font should be bold
                    workSheet.Cells[1, 1, 2, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[1, 1, 2, 10].Style.Font.Size = 16;
                    workSheet.Cells[1, 1, 2, 10].Style.Font.Color.SetColor(Color.Red);


                    // No. 
                    workSheet.Cells[3, 1].Value = "No";
                    workSheet.Cells[3, 1].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 1].Style.Font.Bold = true; //Font should be bold
                    workSheet.Cells[3, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 1].Style.Font.Size = 16;
                    workSheet.Cells[3, 1].Style.Font.Color.SetColor(Color.LightSkyBlue);
                    workSheet.Column(1).Width = 5;

                    // Vùng dịch
                    workSheet.Cells[3, 2].Value = "Vùng dịch";
                    workSheet.Cells[3, 2, 3, 3].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 2, 3, 3].Style.Font.Bold = true; //Font should be bold
                    workSheet.Cells[3, 2, 3, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 2, 3, 3].Style.Font.Size = 16;
                    workSheet.Cells[3, 2, 3, 3].Style.Font.Color.SetColor(Color.LightSkyBlue);
                    workSheet.Column(3).Width = 25;


                    // Địa điểm
                    workSheet.Cells[3, 4].Value = "Địa điểm / phương tiện";
                    workSheet.Cells[3, 4, 3, 6].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 4, 3, 6].Style.Font.Bold = true; //Font should be bold
                    workSheet.Cells[3, 4, 3, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 4, 3, 6].Style.Font.Size = 16;
                    workSheet.Cells[3, 4, 3, 6].Style.Font.Color.SetColor(Color.LightSkyBlue);
                    workSheet.Column(4).Width = 30;


                    // từ ngày
                    workSheet.Cells[3, 7].Value = "Từ ngày";
                    workSheet.Cells[3, 7, 3, 8].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 7, 3, 8].Style.Font.Bold = true; //Font should be bold
                    workSheet.Cells[3, 7, 3, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 7, 3, 8].Style.Font.Size = 16;
                    workSheet.Cells[3, 7, 3, 8].Style.Font.Color.SetColor(Color.LightSkyBlue);
                    workSheet.Column(7).Width = 10;

                    // đến ngày
                    workSheet.Cells[3, 9].Value = "Đến ngày";
                    workSheet.Cells[3, 9, 3, 10].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 9, 3, 10].Style.Font.Bold = true; //Font should be bold
                    workSheet.Cells[3, 9, 3, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 9, 3, 10].Style.Font.Size = 16;
                    workSheet.Cells[3, 9, 3, 10].Style.Font.Color.SetColor(Color.LightSkyBlue);
                    workSheet.Column(9).Width = 10;

                    int rowData = 4;
                    int orderNumber = 1;

                    foreach (var item in entities)
                    {
                        workSheet.Cells[rowData, 1].Value = orderNumber;

                        workSheet.Cells[rowData, 2, rowData, 3 ].Value = item.redZoneName;
                        workSheet.Cells[rowData, 2, rowData, 3].Merge = true;
                        workSheet.Cells[rowData, 2, rowData, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 2, rowData, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 4, rowData, 6].Merge = true;
                        workSheet.Cells[rowData, 4, rowData, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 4, rowData, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        if (item.isRedZoneOnTransportation == true)
                        {
                            workSheet.Cells[rowData, 4, rowData, 6].Value = item.redZoneTransportation;
                        }
                        else
                        {
                            if (item.isDomestic == isDomestic.yes)
                            {
                                workSheet.Cells[rowData, 4, rowData, 6].Value = await getProvinceName(item.redZoneProvinceId) + "-" + await getDistrictName(item.redZoneDistrictId) + "-" + await getWardName(item.redZoneWardId);
                            }
                            else if (item.isDomestic == isDomestic.no)
                            {
                                workSheet.Cells[rowData, 4, rowData, 6].Value = await getCountryName(item.redZoneCountryId) + "-" + item.redZoneCity;

                            }
                        }
                        workSheet.Cells[rowData, 7, rowData, 8].Value = item.redZoneDate != null ? item.redZoneDate.ToString("dd/MM/yyyy") : "";
                        workSheet.Cells[rowData, 7, rowData, 8].Merge = true;
                        workSheet.Cells[rowData, 7, rowData, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 7, rowData, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 9, rowData, 10].Value = item.redZoneToDate != null ? item.redZoneToDate.ToString("dd/MM/yyyy") : "";
                        workSheet.Cells[rowData, 9, rowData, 10].Merge = true;
                        workSheet.Cells[rowData, 9, rowData, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 9, rowData, 10].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        rowData++;
                        orderNumber++;
                    }
                    workSheet.Cells[workSheet.Dimension.Address].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    workSheet.Cells[workSheet.Dimension.Address].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    workSheet.Cells[workSheet.Dimension.Address].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    workSheet.Cells[workSheet.Dimension.Address].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                    package.Save();

                }
                stream.Position = 0;
                using (var fileStream = new FileStream(_filePath, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
                }
              
                string fileName = _fileName;

                // return File(stream, "application/octet-stream", excelName);
                return Json(new { fileName });

            }
            catch (Exception)
            {
                throw;
            }
        
        }

        [HttpGet]
        public ActionResult DownloadExcel(string fileName)
        {
            byte[] fileByteArray = System.IO.File.ReadAllBytes(_filePath);
            System.IO.File.Delete(_filePath);
            return File(fileByteArray , "application/vnd.ms-excel", _fileName);
        }

        #endregion

        //Export excel filter travel route
        #region excel filter travel route
        public async Task<IActionResult> ExportExcelFilterTravelRoute(Guid id)
        {
            var entities = await _redZoneRepo.FilterTravelRouteByRedZone(id);
            var stream = new MemoryStream();
            try
            {
                using (var package = new ExcelPackage(stream))
                {
                    var workSheet = package.Workbook.Worksheets.Add("Red Zone Filtered By Travel Route");
                    workSheet.Cells.Style.Font.Name = "Cambria";

                    // Header
                    workSheet.Cells[1, 1].Value = "Red Zone Filtered By Travel Route";
                    workSheet.Cells[1, 1, 2, 10].Merge = true; //Merge columns start and end range
                    workSheet.Cells[1, 1, 2, 10].Style.Font.Bold = true; //Font should be bold
                    workSheet.Cells[1, 1, 2, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[1, 1, 2, 10].Style.Font.Size = 16;
                    workSheet.Cells[1, 1, 2, 10].Style.Font.Color.SetColor(Color.Red);


                    // No. 
                    workSheet.Cells[3, 1].Value = "No";
                    workSheet.Cells[3, 1].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 1].Style.Font.Bold = true; //Font should be bold
                    workSheet.Cells[3, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 1].Style.Font.Size = 14;
                    workSheet.Cells[3, 1].Style.Font.Color.SetColor(Color.LightSkyBlue);
                    workSheet.Column(1).Width = 5;

                    // Request ID
                    workSheet.Cells[3, 2].Value = "Request ID";
                    workSheet.Cells[3, 2].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 2].Style.Font.Bold = true; //Font should be bold
                    workSheet.Cells[3, 2].Style.Font.Size = 14;
                    workSheet.Cells[3, 2].Style.Font.Color.SetColor(Color.LightSkyBlue);
                    workSheet.Column(2).Width = 10;


                    // Requester
                    workSheet.Cells[3, 3].Value = "Requester";
                    workSheet.Cells[3, 3, 3, 4].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 3, 3, 4].Style.Font.Bold = true; //Font should be bold
                    workSheet.Cells[3, 3, 3, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 3, 3, 4].Style.Font.Size = 14;
                    workSheet.Cells[3, 3, 3, 4].Style.Font.Color.SetColor(Color.LightSkyBlue);
                    workSheet.Column(3).Width = 25;


                    // Submitted Date
                    workSheet.Cells[3, 5].Value = "Submitted Date";
                    workSheet.Cells[3, 5].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 5].Style.Font.Bold = true; //Font should be bold
                    workSheet.Cells[3, 5].Style.Font.Size = 14;
                    workSheet.Cells[3, 5].Style.Font.Color.SetColor(Color.LightSkyBlue);
                    workSheet.Column(5).Width = 15;

                    // Position
                    workSheet.Cells[3, 6].Value = "Position";
                    workSheet.Cells[3, 6, 3, 7].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 6, 3, 7].Style.Font.Bold = true; //Font should be bold
                    workSheet.Cells[3, 6, 3, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 6, 3, 7].Style.Font.Size = 14;
                    workSheet.Cells[3, 6, 3, 7].Style.Font.Color.SetColor(Color.LightSkyBlue);
                    workSheet.Column(6).Width = 20;

                    // Campus
                    workSheet.Cells[3, 8].Value = "Campus";
                    workSheet.Cells[3, 8, 3, 9].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 8, 3, 9].Style.Font.Bold = true; //Font should be bold
                    workSheet.Cells[3, 8, 3, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 8, 3, 9].Style.Font.Size = 14;
                    workSheet.Cells[3, 8, 3, 9].Style.Font.Color.SetColor(Color.LightSkyBlue);
                    workSheet.Column(8).Width = 20;


                    // status
                    workSheet.Cells[3, 10].Value = "Status";
                    workSheet.Cells[3, 10].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 10].Style.Font.Bold = true; //Font should be bold
                    workSheet.Cells[3, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 10].Style.Font.Size = 14;
                    workSheet.Cells[3, 10].Style.Font.Color.SetColor(Color.LightSkyBlue);
                    workSheet.Column(10).Width = 15;



                    int rowData = 4;
                    int orderNumber = 1;

                    foreach (var item in entities)
                    {
                        workSheet.Cells[rowData, 1].Value = orderNumber;

                        workSheet.Cells[rowData, 2].Value = item.request_id;
                        workSheet.Cells[rowData, 2].Merge = true;
                        workSheet.Cells[rowData, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 3, rowData, 4].Value = item.Requester.FirstName + item.Requester.LastName;
                        workSheet.Cells[rowData, 3, rowData, 4].Merge = true;
                        workSheet.Cells[rowData, 3, rowData, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 3, rowData, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 5].Value = item.createdOn != null ? item.createdOn.Value.ToString("dd/MM/yyyy") : "";
                        workSheet.Cells[rowData, 5].Merge = true;
                        workSheet.Cells[rowData, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 6, rowData, 7].Value = item.Requester.Position;
                        workSheet.Cells[rowData, 6, rowData, 7].Merge = true;
                        workSheet.Cells[rowData, 6, rowData, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 6, rowData, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 8, rowData, 9].Value = item.Requester.Campus;
                        workSheet.Cells[rowData, 8, rowData, 9].Merge = true;
                        workSheet.Cells[rowData, 8, rowData, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 8, rowData, 9].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 10].Value = EnumExtensions.GetDisplayName((RequestStatus)item.LatestStatus);
                        workSheet.Cells[rowData, 10].Merge = true;
                        workSheet.Cells[rowData, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 10].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        rowData++;
                        orderNumber++;
                    }
                    workSheet.Cells[workSheet.Dimension.Address].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    workSheet.Cells[workSheet.Dimension.Address].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    workSheet.Cells[workSheet.Dimension.Address].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    workSheet.Cells[workSheet.Dimension.Address].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                    package.Save();

                }
                stream.Position = 0;

                string excelName = $"RedZoneFilteredByTravelRoute-Report-{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.xlsx";
                return File(stream, "application/octet-stream", excelName);

            }
            catch (Exception)
            {
                throw;
            }

        }

        #endregion


        //support method 
        #region
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

    }
}