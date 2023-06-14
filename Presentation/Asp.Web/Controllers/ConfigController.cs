using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace src.Web.Controllers
{
    public class ConfigController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
