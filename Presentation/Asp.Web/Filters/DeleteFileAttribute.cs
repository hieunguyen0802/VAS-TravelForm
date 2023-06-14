using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace src.Web.Filters
{
    public class DeleteFileAttributeAfterDownload : ActionFilterAttribute
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        public DeleteFileAttributeAfterDownload(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }


        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await next();
            if (context.ActionArguments.ContainsKey("filename"))
            {
                string filename = (string)context.ActionArguments["filename"];
                string webRootPath = _hostingEnvironment.WebRootPath;
                var fullPath = webRootPath + "/ExportStudent/" + filename;

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
        }
    }
}
