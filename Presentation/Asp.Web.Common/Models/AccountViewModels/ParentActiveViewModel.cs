using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using src.Localization.Resources;

namespace src.Web.Common.Models.AccountViewModels
{
    public class ParentActiveViewModel
    {
        [Display(Name = "UserName")]
        [Required(ErrorMessageResourceName = "EnterUsername", ErrorMessageResourceType = typeof(SharedResource))]
        [RegularExpression(@"^[0-9a-zA-Z\.]*$", ErrorMessageResourceName = "RegularExpression_Username", ErrorMessageResourceType = typeof(SharedResource))]
        public string PhoneNumber { get; set; }

        [Display(Name = "Student")]
        public string Student { get; set; }
        [Required(ErrorMessageResourceName = "PleaseSelectStudent", ErrorMessageResourceType = typeof(SharedResource))]
        public IList<SelectListItem>listStudents { get; set; }

        [Display(Name = "DateOfBirthStudent")]
        //[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        [Required(ErrorMessageResourceName = "DateOfBirthStudent_Message", ErrorMessageResourceType = typeof(SharedResource))]
        public DateTime DateOfBirthStudent { get; set; }

        public ParentActiveViewModel()
        {
            listStudents = new List<SelectListItem>();
        }
    }
}
