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
using src.Core.Data;
using src.Repositories.Category;
using src.Repositories.IncidentReports;
using src.Repositories.TravelDeclarations;
using src.Web.Common;
using src.Core.Enums;
using src.Web.Extensions;
using src.Repositories.RedZone;

namespace src.Web.Areas.Administration.Controllers
{
    [Area(Constants.Areas.Administration)]
    [Authorize(Policy = Constants.RoleNames.Administrator)]
    
    public class SummaryController : Controller
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

        public SummaryController(
            IDateTime dateTime,
            IMapper mapper,
            ILogger<ReportController> logger,
            IUserSession userSession,
            IHostingEnvironment hostingEnvironment,
            IAuthorizationService authorizationService,
            IBaseCategoryRepository baseCategoryRepository,
            ITravelDeclarationRepository travelDeclarationRepository,
            IIncidentReportRepository incidentReportRepository,
            IRedZoneRepo redZoneRepo
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
        }
       
        public async Task<IActionResult> Index()
        {
            try
            {
                var entities = await _redZoneRepo.ListAllUpdateHistories();
                var model = entities.OrderBy(t => t.updatedDate).ToList();
                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw;
            }

        }

        public async Task<IActionResult> ExportExcel()
        {
            try
            {
                var stream = new MemoryStream();
                using (var package = new ExcelPackage(stream))
                {

                    var workSheet = package.Workbook.Worksheets.Add("Summary of Update History");
                    #region template excel
                    //// Header
                    workSheet.Cells[1, 1].Value = "SUMMARY OF UPDATE HISTORY";
                    workSheet.Cells[1, 1, 2, 8].Merge = true; //Merge columns start and end range
                    workSheet.Cells[1, 1, 2, 8].Style.Font.Bold = true; //Font should be bold
                    workSheet.Cells[1, 1, 2, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                    workSheet.Cells[1, 1, 2, 8].Style.Font.Size = 16;
                    workSheet.Cells[1, 1, 2, 8].Style.Font.Color.SetColor(Color.Red);

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
                    workSheet.Cells[3, 3].Value = "Cơ sở / Campus";
                    workSheet.Cells[3, 3, 4, 3].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 3, 4, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 3, 4, 3].Style.Font.Size = 11;
                    workSheet.Cells[3, 3, 4, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 3, 4, 3].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(3).Width = 20;
                    workSheet.Column(3).Style.WrapText = true;

                    //Số Du lịch / Travel No.
                    workSheet.Cells[3, 4].Value = "Số Du lịch / Travel No.";
                    workSheet.Cells[3, 4, 4, 4].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 4, 4, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 4, 4, 4].Style.Font.Size = 11;
                    workSheet.Cells[3, 4, 4, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 4, 4, 4].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(4).Width = 10;
                    workSheet.Column(4).Style.WrapText = true;

                    //Số tường trình covid / Incident No.
                    workSheet.Cells[3, 5].Value = "Số tường trình covid / Incident No.";
                    workSheet.Cells[3, 5, 4, 5].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 5, 4, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 5, 4, 5].Style.Font.Size = 11;
                    workSheet.Cells[3, 5, 4, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 5, 4, 5].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(5).Width = 10;
                    workSheet.Column(5).Style.WrapText = true;

                    //Dữ liệu / Field
                    workSheet.Cells[3, 6].Value = "Dữ liệu / Field";
                    workSheet.Cells[3, 6, 4, 6].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 6, 4, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 6, 4, 6].Style.Font.Size = 11;
                    workSheet.Cells[3, 6, 4, 6].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 6, 4, 6].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(6).Width = 25;
                    workSheet.Column(6).Style.WrapText = true;

                    //Giá trị cập nhật / Updated value
                    workSheet.Cells[3, 7].Value = "Giá trị cập nhật / Updated value";
                    workSheet.Cells[3, 7, 4, 7].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 7, 4, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 7, 4, 7].Style.Font.Size = 11;
                    workSheet.Cells[3, 7, 4, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 7, 4, 7].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(7).Width = 15;
                    workSheet.Column(7).Style.WrapText = true;

                    //Người dùng / User
                    workSheet.Cells[3, 8].Value = "Người dùng / User";
                    workSheet.Cells[3, 8, 4, 8].Merge = true; //Merge columns start and end range
                    workSheet.Cells[3, 8, 4, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Alignment is center
                    workSheet.Cells[3, 8, 4, 8].Style.Font.Size = 11;
                    workSheet.Cells[3, 8, 4, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Cells[3, 8, 4, 8].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    workSheet.Column(8).Width = 25;
                    workSheet.Column(8).Style.WrapText = true;

                    #endregion

                    var entities = await _redZoneRepo.ListAllUpdateHistories();
                    var model = entities.OrderBy(t => t.updatedDate).ToList();
                    int rowData = 5;
                    var orderNumber = 1;

                    foreach (var item in model)
                    {

                        workSheet.Cells[rowData, 1].Value = orderNumber;

                        workSheet.Cells[rowData, 2].Value = item.updatedDate != null ? item.updatedDate.ToString("dd/MM/yyyy") : ""; ;
                        workSheet.Cells[rowData, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        workSheet.Cells[rowData, 3].Value = item.campus;
                        workSheet.Cells[rowData, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 4].Value = item.travelNo;
                        workSheet.Cells[rowData, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 5].Value = item.incidentNo;
                        workSheet.Cells[rowData, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 6].Value = item.updatedField;
                        workSheet.Cells[rowData, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 7].Value = item.updatedValue;
                        workSheet.Cells[rowData, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                        workSheet.Cells[rowData, 8].Value = item.updatedBy;
                        workSheet.Cells[rowData, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        workSheet.Cells[rowData, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

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

                string excelName = $"SummaryUpdateHistory-Report-{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.xlsx";
                return File(stream, "application/octet-stream", excelName);

            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }


    }
}