using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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
using src.Repositories.Category;
using src.Repositories.IncidentReports;
using src.Repositories.TravelDeclarations;
using src.Web.Common;
using src.Core.Enums;
using src.Web.Extensions;
using System.Data;
using src.Repositories.RedZone;
using System.Web.Http;
using HttpGetAttribute = System.Web.Http.HttpGetAttribute;
using src.Data;
using src.Web.Common.Models.RedZone;
using src.Web.Common.Models.IncidentReportViewModel;
using src.Web.Common.Mvc.Alerts;

namespace src.Web.Areas.HRBP.Controllers
{
    [Area(Constants.Areas.HRBP)]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = Constants.RoleNames.HRBP)]
    public class RedZoneFollowUpController : Controller
    {
        private readonly IDateTime _dateTime;
        private readonly IMapper _mapper;
        private readonly ILogger<ReportController> _logger;
        private readonly IUserSession _userSession;
        private readonly IHostingEnvironment _hostingEnvironment;
        private IAuthorizationService _authorizationService;
        private readonly ITravelDeclarationRepository _travelDeclarationRepository;
        private readonly IIncidentReportRepository _incidentReportRpository;
        private readonly IBaseCategoryRepository _baseCategoryRepository;
        private readonly IRedZoneRepo _redZoneRepo;
        private readonly IDbContext _dbContext;

        public RedZoneFollowUpController(
            IDateTime dateTime,
            IMapper mapper,
            ILogger<ReportController> logger,
            IUserSession userSession,
            IHostingEnvironment hostingEnvironment,
            IAuthorizationService authorizationService,
            IBaseCategoryRepository baseCategoryRepository,
            ITravelDeclarationRepository travelDeclarationRepository,
            IIncidentReportRepository incidentReportRepository,
            IRedZoneRepo redZoneRepo,
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
            _travelDeclarationRepository = travelDeclarationRepository;
            _incidentReportRpository = incidentReportRepository;
            _redZoneRepo = redZoneRepo;
            _dbContext = dbContext;

        }


        public IActionResult Index()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> GetFollowList(Guid id)
        {
            try
            {
                var allFollowUps = await _redZoneRepo.listAllFollowUp();
                var entities = allFollowUps.Where(h => h.RedZoneId == id).ToList();

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
                var entity = await _redZoneRepo.GetRedZonesFollowUpAndIncidentById(id);
                if (entity == null)
                    return RedirectToAction("Index");

                var model = _mapper.Map<RedZoneFollowUp, RedZoneFollowUpViewModel>(entity);
                if (model.IncidentReport != null)
                {
                    model.ProvinceName = await getProvinceName(model.IncidentReport.reporterProvinceId);
                    model.DistrictName = await getDistrictName(model.IncidentReport.reporterDistrictId);
                    model.WardName = await getWardName(model.IncidentReport.reporterWardId);
                    model.FType = model.IncidentReport.fTypeConfirmed;
                    return View(model);
                }

                return RedirectToAction("Index").WithError("Incident Report is not available !!!");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw ex;
            }

        }

        [System.Web.Http.HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditRequest(RedZoneFollowUp model)
        {
            try
            {
                //update RedZoneFollowUp
                var redZone = _redZoneRepo.GetRedZonesFollowUpById(model.RedZoneFollowUpId);
                redZone.FType = model.FType;
                redZone.FTypeByVas = model.FTypeByVas;
                redZone.RegulatedAction = model.RegulatedAction;
                redZone.QuarantineDuration = model.QuarantineDuration;
                redZone.VasQuarantineDuration = model.VasQuarantineDuration;
                redZone.Notes = model.Notes;
                redZone.InfoProvider = model.InfoProvider;
                redZone.isFollowUp = model.isFollowUp;
                redZone.isRelated = model.isRelated;

                _redZoneRepo.UpdateRedZoneFollowUp(redZone);

                //add to history
                var ftype = "Phân loại lây nhiễm (Theo xác nhận của CQYT/ NV) / Infection Classification (By Medical Center/ Employee)";
                var fTypeByVAS = "Phân loại lây nhiễm (Theo VAS) / Infection Clsasification (by VAS)";
                var RegAct = "Thực hiện theo quy định (Bước 3 của quy trình) / Regulated actions (As Step #3 of the process)";
                var quarantine = "Thời gian cách ly (Theo hướng dẫn của CQYT) / Quarantine Duration by Mecidal Center";
                var quarantineByVAS = "Thời gian cách ly (Theo hướng dẫn của VAS) / Quarantine Duration by VAS";
                var infoProvide = "Người cung cấp thông tin / Information provided by";
                var notes = "Ghi chú / Notes";
                var isFollow = "Đang theo dõi / Follow - up";
                var isRelated = "Không liên quan / Unrelated";



                if (model.FType != null)
                {
                    var historyMapper = _mapper.Map<RedZoneFollowUp, updateHistory>(redZone);
                    historyMapper.updateHistoryId = new Guid();
                    historyMapper.updatedDate = _dateTime.Now;
                    historyMapper.campus = model.Campus;
                    historyMapper.travelNo = model.RequestId;
                    historyMapper.incidentNo = model.incidentRequest;
                    historyMapper.updatedBy = model.Employee;
                    historyMapper.updatedValue = model.FType;
                    historyMapper.updatedField = ftype;
                    historyMapper.redZoneFollowUpId = model.RedZoneFollowUpId;
                    historyMapper.incidentId = model.IncidentReportId;
                    historyMapper.travelId = model.travelId;


                    _redZoneRepo.insertUpdateHistory(historyMapper);
                }
                if (model.FTypeByVas != null)
                {
                    var historyMapper = _mapper.Map<RedZoneFollowUp, updateHistory>(redZone);
                    historyMapper.updateHistoryId = new Guid();
                    historyMapper.updatedDate = _dateTime.Now;
                    historyMapper.campus = model.Campus;
                    historyMapper.travelNo = model.RequestId;
                    historyMapper.incidentNo = model.incidentRequest;
                    historyMapper.updatedBy = model.Employee;
                    historyMapper.updatedValue = model.FTypeByVas;
                    historyMapper.updatedField = fTypeByVAS;
                    historyMapper.redZoneFollowUpId = model.RedZoneFollowUpId;
                    historyMapper.incidentId = model.IncidentReportId;
                    historyMapper.travelId = model.travelId;
                    _redZoneRepo.insertUpdateHistory(historyMapper);
                }
                if (model.RegulatedAction != null)
                {
                    var historyMapper = _mapper.Map<RedZoneFollowUp, updateHistory>(redZone);
                    historyMapper.updateHistoryId = new Guid();
                    historyMapper.updatedDate = _dateTime.Now;
                    historyMapper.campus = model.Campus;
                    historyMapper.travelNo = model.RequestId;
                    historyMapper.incidentNo = model.incidentRequest;
                    historyMapper.updatedBy = model.Employee;
                    historyMapper.updatedValue = model.RegulatedAction;
                    historyMapper.updatedField = RegAct;
                    historyMapper.redZoneFollowUpId = model.RedZoneFollowUpId;
                    historyMapper.incidentId = model.IncidentReportId;
                    historyMapper.travelId = model.travelId;
                    _redZoneRepo.insertUpdateHistory(historyMapper);
                }
                if (model.QuarantineDuration != 0)
                {
                    var historyMapper = _mapper.Map<RedZoneFollowUp, updateHistory>(redZone);
                    historyMapper.updateHistoryId = new Guid();
                    historyMapper.updatedDate = _dateTime.Now;
                    historyMapper.campus = model.Campus;
                    historyMapper.travelNo = model.RequestId;
                    historyMapper.incidentNo = model.incidentRequest;
                    historyMapper.updatedBy = model.Employee;
                    historyMapper.updatedValue = model.QuarantineDuration.ToString();
                    historyMapper.updatedField = quarantine;
                    historyMapper.redZoneFollowUpId = model.RedZoneFollowUpId;
                    historyMapper.incidentId = model.IncidentReportId;
                    historyMapper.travelId = model.travelId;
                    _redZoneRepo.insertUpdateHistory(historyMapper);
                }
                if (model.VasQuarantineDuration != 0)
                {
                    var historyMapper = _mapper.Map<RedZoneFollowUp, updateHistory>(redZone);
                    historyMapper.updateHistoryId = new Guid();
                    historyMapper.updatedDate = _dateTime.Now;
                    historyMapper.campus = model.Campus;
                    historyMapper.travelNo = model.RequestId;
                    historyMapper.incidentNo = model.incidentRequest;
                    historyMapper.updatedBy = model.Employee;
                    historyMapper.updatedValue = model.VasQuarantineDuration.ToString();
                    historyMapper.updatedField = quarantineByVAS;
                    historyMapper.redZoneFollowUpId = model.RedZoneFollowUpId;
                    historyMapper.incidentId = model.IncidentReportId;
                    historyMapper.travelId = model.travelId;
                    _redZoneRepo.insertUpdateHistory(historyMapper);
                }
                if (model.InfoProvider != null)
                {
                    var historyMapper = _mapper.Map<RedZoneFollowUp, updateHistory>(redZone);
                    historyMapper.updateHistoryId = new Guid();
                    historyMapper.updatedDate = _dateTime.Now;
                    historyMapper.campus = model.Campus;
                    historyMapper.travelNo = model.RequestId;
                    historyMapper.incidentNo = model.incidentRequest;
                    historyMapper.updatedBy = model.Employee;
                    historyMapper.updatedValue = model.InfoProvider;
                    historyMapper.updatedField = infoProvide;
                    historyMapper.redZoneFollowUpId = model.RedZoneFollowUpId;
                    historyMapper.incidentId = model.IncidentReportId;
                    historyMapper.travelId = model.travelId;
                    _redZoneRepo.insertUpdateHistory(historyMapper);
                }
                if (model.Notes != null)
                {
                    var historyMapper = _mapper.Map<RedZoneFollowUp, updateHistory>(redZone);
                    historyMapper.updateHistoryId = new Guid();
                    historyMapper.updatedDate = _dateTime.Now;
                    historyMapper.campus = model.Campus;
                    historyMapper.travelNo = model.RequestId;
                    historyMapper.incidentNo = model.incidentRequest;
                    historyMapper.updatedBy = model.Employee;
                    historyMapper.updatedValue = model.Notes;
                    historyMapper.updatedField = notes;
                    historyMapper.redZoneFollowUpId = model.RedZoneFollowUpId;
                    historyMapper.incidentId = model.IncidentReportId;
                    historyMapper.travelId = model.travelId;
                    _redZoneRepo.insertUpdateHistory(historyMapper);
                }

                if (model.isFollowUp == true || model.isFollowUp == false)
                {
                    var historyMapper = _mapper.Map<RedZoneFollowUp, updateHistory>(redZone);
                    historyMapper.updateHistoryId = new Guid();
                    historyMapper.updatedDate = _dateTime.Now;
                    historyMapper.campus = model.Campus;
                    historyMapper.travelNo = model.RequestId;
                    historyMapper.incidentNo = model.incidentRequest;
                    historyMapper.updatedBy = model.Employee;
                    historyMapper.updatedValue = model.isFollowUp.ToString();
                    historyMapper.updatedField = isFollow;
                    historyMapper.redZoneFollowUpId = model.RedZoneFollowUpId;
                    historyMapper.incidentId = model.IncidentReportId;
                    historyMapper.travelId = model.travelId;
                    _redZoneRepo.insertUpdateHistory(historyMapper);
                }
                if (model.isRelated == true || model.isRelated == false)
                {
                    var historyMapper = _mapper.Map<RedZoneFollowUp, updateHistory>(redZone);
                    historyMapper.updateHistoryId = new Guid();
                    historyMapper.updatedDate = _dateTime.Now;
                    historyMapper.campus = model.Campus;
                    historyMapper.travelNo = model.RequestId;
                    historyMapper.incidentNo = model.incidentRequest;
                    historyMapper.updatedBy = model.Employee;
                    historyMapper.updatedValue = model.isRelated.ToString();
                    historyMapper.updatedField = isRelated;
                    historyMapper.redZoneFollowUpId = model.RedZoneFollowUpId;
                    historyMapper.incidentId = model.IncidentReportId;
                    historyMapper.travelId = model.travelId;
                    _redZoneRepo.insertUpdateHistory(historyMapper);
                }

                return RedirectToAction("Index").WithSuccess("Update success!");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw ex;
            }
        }




        public async Task<IActionResult> History(Guid id)
        {
            try
            {
                var entities = await _redZoneRepo.GetHistoryList(id);
                var model = entities.OrderBy(t => t.updatedDate).ToList();
                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw;
            }

        }

        //export excel
        public async Task<IActionResult> ExportExcelUpdateHistory(Guid id)
        {
            try
            {
                var stream = new MemoryStream();
                using (var package = new ExcelPackage(stream))
                {

                    var workSheet = package.Workbook.Worksheets.Add("Summary of Update History");
                    #region template excel
                    //// Header
                    workSheet.Cells[1, 1].Value = "UPDATE HISTORY";
                    workSheet.Cells[1, 1, 2, 5].Merge = true; //Merge columns start and end range
                    workSheet.Cells[1, 1, 2, 5].Style.Font.Bold = true; //Font should be bold
                    workSheet.Cells[1, 1, 2, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[1, 1, 2, 5].Style.Font.Size = 16;
                    workSheet.Cells[1, 1, 2, 5].Style.Font.Color.SetColor(Color.Red);

                    //No
                    workSheet.Cells[3, 1].Value = "No";
                    workSheet.Cells[3, 1, 4, 1].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 1, 4, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 1, 4, 1].Style.Font.Size = 11;
                    workSheet.Cells[3, 1, 4, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 1, 4, 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(1).Width = 5;
                    workSheet.Column(1).Style.WrapText = true;

                    //Date
                    workSheet.Cells[3, 2].Value = "Ngày / Date";
                    workSheet.Cells[3, 2, 4, 2].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 2, 4, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 2, 4, 2].Style.Font.Size = 11;
                    workSheet.Cells[3, 2, 4, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 2, 4, 2].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(2).Width = 15;
                    workSheet.Column(2).Style.WrapText = true;

                    //Campus
                    workSheet.Cells[3, 3].Value = "Dữ liệu / Field";
                    workSheet.Cells[3, 3, 4, 3].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 3, 4, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 3, 4, 3].Style.Font.Size = 11;
                    workSheet.Cells[3, 3, 4, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 3, 4, 3].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(3).Width = 30;
                    workSheet.Column(3).Style.WrapText = true;

                    //Số Du lịch / Travel No.
                    workSheet.Cells[3, 4].Value = "Giá trị cập nhật / Updated value";
                    workSheet.Cells[3, 4, 4, 4].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 4, 4, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 4, 4, 4].Style.Font.Size = 11;
                    workSheet.Cells[3, 4, 4, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 4, 4, 4].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(4).Width = 30;
                    workSheet.Column(4).Style.WrapText = true;

                    //Số tường trình covid / Incident No.
                    workSheet.Cells[3, 5].Value = "Người dùng / User";
                    workSheet.Cells[3, 5, 4, 5].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 5, 4, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 5, 4, 5].Style.Font.Size = 11;
                    workSheet.Cells[3, 5, 4, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 5, 4, 5].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(5).Width = 30;
                    workSheet.Column(5).Style.WrapText = true;



                    #endregion

                    var entities = await _redZoneRepo.GetHistoryList(id);
                    var model = entities.OrderBy(t => t.updatedDate).ToList();
                    int rowData = 5;
                    var orderNumber = 1;

                    foreach (var item in model)
                    {

                        workSheet.Cells[rowData, 1].Value = orderNumber;

                        workSheet.Cells[rowData, 2].Value = item.updatedDate != null ? item.updatedDate.ToString("dd/MM/yyyy") : ""; ;
                        workSheet.Cells[rowData, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 3].Value = item.updatedField;
                        workSheet.Cells[rowData, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 4].Value = item.updatedValue;
                        workSheet.Cells[rowData, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 5].Value = item.updatedBy;
                        workSheet.Cells[rowData, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

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

                string excelName = $"UpdateHistory-Report-{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.xlsx";
                return File(stream, "application/octet-stream", excelName);

            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<IActionResult> ExportExcel(Guid id)
        {
            try
            {
                var stream = new MemoryStream();
                using (var package = new ExcelPackage(stream))
                {

                    var workSheet = package.Workbook.Worksheets.Add("Red Zone in Red Zone Follow Up");
                    #region template excel
                    //// Header
                    workSheet.Cells[1, 1].Value = "Red Zone Travelling List in Red Zone Follow Up";
                    workSheet.Cells[1, 1, 2, 10].Merge = true; //Merge columns start and end range
                    workSheet.Cells[1, 1, 2, 10].Style.Font.Bold = true; //Font should be bold
                    workSheet.Cells[1, 1, 2, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[1, 1, 2, 10].Style.Font.Size = 16;
                    workSheet.Cells[1, 1, 2, 10].Style.Font.Color.SetColor(Color.Red);

                    //No
                    workSheet.Cells[3, 1].Value = "No";
                    workSheet.Cells[3, 1, 4, 1].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 1, 4, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 1, 4, 1].Style.Font.Size = 11;
                    workSheet.Cells[3, 1, 4, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 1, 4, 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(1).Width = 5;
                    workSheet.Column(1).Style.WrapText = true;

                    //Số yêu cầu / Request ID
                    workSheet.Cells[3, 2].Value = "Số yêu cầu / Request ID";
                    workSheet.Cells[3, 2, 4, 2].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 2, 4, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 2, 4, 2].Style.Font.Size = 11;
                    workSheet.Cells[3, 2, 4, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 2, 4, 2].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(2).Width = 15;
                    workSheet.Column(2).Style.WrapText = true;

                    //Nhân viên / Employe
                    workSheet.Cells[3, 3].Value = "Nhân viên / Employe";
                    workSheet.Cells[3, 3, 4, 3].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 3, 4, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 3, 4, 3].Style.Font.Size = 11;
                    workSheet.Cells[3, 3, 4, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 3, 4, 3].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(3).Width = 30;
                    workSheet.Column(3).Style.WrapText = true;

                    //Ngày gửi / Submitted Date
                    workSheet.Cells[3, 4].Value = "Ngày gửi / Submitted Date";
                    workSheet.Cells[3, 4, 4, 4].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 4, 4, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 4, 4, 4].Style.Font.Size = 11;
                    workSheet.Cells[3, 4, 4, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 4, 4, 4].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(4).Width = 30;
                    workSheet.Column(4).Style.WrapText = true;

                    //Vị trí / Position
                    workSheet.Cells[3, 5].Value = "Vị trí / Position";
                    workSheet.Cells[3, 5, 4, 5].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 5, 4, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 5, 4, 5].Style.Font.Size = 11;
                    workSheet.Cells[3, 5, 4, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 5, 4, 5].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(5).Width = 30;
                    workSheet.Column(5).Style.WrapText = true;

                    //Cơ sở / Campus
                    workSheet.Cells[3, 6].Value = "Cơ sở / Campus";
                    workSheet.Cells[3, 6, 4, 6].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 6, 4, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 6, 4, 6].Style.Font.Size = 11;
                    workSheet.Cells[3, 6, 4, 6].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 6, 4, 6].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(6).Width = 30;
                    workSheet.Column(6).Style.WrapText = true;

                    //Tình trạng / Req. Status
                    workSheet.Cells[3, 7].Value = "Tình trạng / Req. Status";
                    workSheet.Cells[3, 7, 4, 7].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 7, 4, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 7, 4, 7].Style.Font.Size = 11;
                    workSheet.Cells[3, 7, 4, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 7, 4, 7].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(7).Width = 20;
                    workSheet.Column(7).Style.WrapText = true;

                    //Đơn khai báo y tế / Incident
                    workSheet.Cells[3, 8].Value = "Đơn khai báo y tế / Incident";
                    workSheet.Cells[3, 8, 4, 8].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 8, 4, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 8, 4, 8].Style.Font.Size = 11;
                    workSheet.Cells[3, 8, 4, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 8, 4, 8].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(8).Width = 15;
                    workSheet.Column(8).Style.WrapText = true;

                    //Theo dõi / Follow up
                    workSheet.Cells[3, 9].Value = "Theo dõi / Follow up";
                    workSheet.Cells[3, 9, 4, 9].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 9, 4, 9].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 9, 4, 9].Style.Font.Size = 11;
                    workSheet.Cells[3, 9, 4, 9].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 9, 4, 9].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(9).Width = 10;
                    workSheet.Column(9).Style.WrapText = true;

                    //Liên quan / Related
                    workSheet.Cells[3, 10].Value = "Liên quan / Related";
                    workSheet.Cells[3, 10, 4, 10].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 10, 4, 10].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 10, 4, 10].Style.Font.Size = 11;
                    workSheet.Cells[3, 10, 4, 10].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 10, 4, 10].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(10).Width = 10;
                    workSheet.Column(10).Style.WrapText = true;


                    #endregion

                    var allFollowUps = await _redZoneRepo.listAllFollowUp();
                    var entities = allFollowUps.Where(h => h.RedZoneId == id).ToList();
                    int rowData = 5;
                    var orderNumber = 1;

                    foreach (var item in entities)
                    {

                        workSheet.Cells[rowData, 1].Value = orderNumber;

                        workSheet.Cells[rowData, 2].Value = item.RequestId;
                        workSheet.Cells[rowData, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 3].Value = item.Employee;
                        workSheet.Cells[rowData, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 4].Value = item.submittedDate != null ? item.submittedDate.Value.ToString("dd / MM / yyyy") : "";
                        workSheet.Cells[rowData, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 5].Value = item.Position;
                        workSheet.Cells[rowData, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 6].Value = item.Campus;
                        workSheet.Cells[rowData, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 7].Value = item.status;
                        workSheet.Cells[rowData, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 8].Value = item.incidentRequest;
                        workSheet.Cells[rowData, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 9].Value = item.isFollowUp != true ? "Yes" : "No";
                        workSheet.Cells[rowData, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 9].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 10].Value = item.isRelated != true ? "Yes" : "No";
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

                string excelName = $"RedZoneFollowUpList-Report-{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.xlsx";
                return File(stream, "application/octet-stream", excelName);

            }
            catch (Exception ex)
            {
                throw ex;
            }

        }



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
    }
}