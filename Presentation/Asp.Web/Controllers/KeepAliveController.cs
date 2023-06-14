using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace src.Web.Controllers
{
    [AllowAnonymous]
    public class KeepAliveController : Controller
    {
        //
        // GET: /KeepAlive
        [AllowAnonymous]
        public IActionResult Index()
        {
            return Content("I am alive!");
        }
    }
}