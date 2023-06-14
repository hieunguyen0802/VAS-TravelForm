﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace src.Web.Helpers
{
    public class SetTempDataModelStateAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);

            var controller = filterContext.Controller as Controller;
            var modelState = controller?.ViewData.ModelState;
            if (modelState != null)
            {
                var listError = modelState.Where(x => x.Value.Errors.Any())
                    .ToDictionary(m => m.Key, m => m.Value.Errors
                    .Select(s => s.ErrorMessage)
                    .FirstOrDefault(s => s != null));
                controller.TempData["errorMessage"] = JsonConvert.SerializeObject(listError);
            }
        }

        public class RestoreModelStateFromTempDataAttribute : ActionFilterAttribute
        {
            public override void OnActionExecuting(ActionExecutingContext filterContext)
            {
                base.OnActionExecuting(filterContext);

                var controller = filterContext.Controller as Controller;
                var tempData = controller?.TempData?.Keys;
                if (controller != null && tempData != null)
                {
                    if (tempData.Contains("errorMessage"))
                    {
                        var modelStateString = controller.TempData["errorMessage"].ToString();
                        var listError = JsonConvert.DeserializeObject<Dictionary<string, string>>(modelStateString);
                        var modelState = new ModelStateDictionary();
                        foreach (var item in listError)
                        {
                            modelState.AddModelError(item.Key, item.Value ?? "");
                        }

                        controller.ViewData.ModelState.Merge(modelState);
                    }
                }
            }
        }
    }
}
