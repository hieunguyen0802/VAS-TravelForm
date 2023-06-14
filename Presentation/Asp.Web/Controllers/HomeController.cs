using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using src.Core;
using src.Core.Domains;
using src.Repositories.ParentActivity;
using src.Web.Common;
using src.Web.Helpers;
using System.Data;
using Dapper;
using System.Linq;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using src.Web.Common.Models.FileManagerModels;

namespace src.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IUserSession _userSession;
        private readonly IDateTime _dateTime;
        private IParentActivityRepository _parentActivityRepository;
        private readonly IConfiguration _configuration;
        private IHostingEnvironment _env;
        public HomeController(ILogger<AccountController> logger, IUserSession userSession, IDateTime dateTime, IParentActivityRepository parentActivityRepository, IConfiguration configuration, IHostingEnvironment env)
        {
            _logger = logger;
            _userSession = userSession;
            _dateTime = dateTime;
            _parentActivityRepository = parentActivityRepository;
            _configuration = configuration;
            _env = env;
            
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }
        public IActionResult HandBook()
        {
            FileManagerModel model = new FileManagerModel();
            var userImagesPath = Path.Combine(_env.WebRootPath, "handbook-img");
            DirectoryInfo dir = new DirectoryInfo(userImagesPath);
            FileInfo[] files = dir.GetFiles();
            model.Files = files;

            if (_userSession.Id != 0 && !string.IsNullOrEmpty(_userSession.UserName))
            {
                var isExistParentOnPP = isExistParent(_userSession.Id.ToString());
                if (isExistParentOnPP != null)
                {
                    var isExist = getParentByid(_userSession.Id);
                    if (isExist == null)
                    {
                        var parent_activity_model = new parents_activities()
                        {
                            id = _userSession.Id,
                            email = _userSession.UserName,
                            view_at = _dateTime.Now
                        };
                        _parentActivityRepository.saveParentActivity(parent_activity_model);

                    }
                    _logger.LogInformation($"Parents view handbook - Email {_userSession.UserName} - At {_dateTime.Now.ToString()}");
                }
            }
            return View(model);
        }
        public JsonResult handBookAccept()
        {
            if (_userSession.Id != 0 && !string.IsNullOrEmpty(_userSession.UserName))
            {
                var isExistParentOnPP = isExistParent(_userSession.Id.ToString());
                if (isExistParentOnPP != null)
                {
                    var isExist = getParentByid(_userSession.Id);
                    if (isExist != null && isExist.accept_at == null)
                    {
                        isExist.accept_at = _dateTime.Now;
                        _parentActivityRepository.updateParentActivity(isExist);
                        _logger.LogInformation($"Parents accepted handbook - Email {_userSession.UserName} - At {_dateTime.Now.ToString()}");
                    }
                    return Json(new
                    {
                        success = true,
                        message = "accepting represents parent’s and student’s" +
                           " awareness of school policies and procedures for" +
                           " school year 2020 - 2021. \n * việc xác nhận đồng ý được hiểu là quý phụ huynh đã hiểu" +
                           " rõ và cam kết tuân thủ các nội quy, quy định và chính" +
                           " sách áp dụng tại vas, năm học 2020 - 2021 !"
                    });
                }
                else
                {
                    return Json(new
                    {

                        success = false,
                        message = "Oops"
                    }); ;
                }
            }
            else
            {
                return Json(new
                {

                    success = false,
                    message = " Something went wrong. We're working on getting this fixed as soon as we can. You may be able to try again."
                }); ;

            }

        }
        private parents_activities getParentByid(int id)
        {
            string _dbCon = _configuration.GetSection("ConnectionStrings").GetSection("DefaultConnection").Value;
            using (IDbConnection dbConnection = DapperConnection.sqlConnection(_dbCon))
            {
                dbConnection.Open();
                return dbConnection.Query<parents_activities>("" +
                    "SELECT * FROM parents_activities " +
                    "WHERE " +
                    "parents_activities.id = @id",
                    new { id = id }).FirstOrDefault();
            }
        }
        private Parents isExistParent(string id)
        {
            string _dbCon = _configuration.GetSection("ConnectionStrings").GetSection("ParentPortalConnection").Value;
            using (IDbConnection dbConnection = DapperConnection.postgreConnection(_dbCon))
            {
                dbConnection.Open();
                return dbConnection.Query<Parents>("" +
                    "SELECT * FROM parents " +
                    "WHERE " +
                    "parents.id = @id",
                    new { id = id }).FirstOrDefault();
            }
        }

    }
}