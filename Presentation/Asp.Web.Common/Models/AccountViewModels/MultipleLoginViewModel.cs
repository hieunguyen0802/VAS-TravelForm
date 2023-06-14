using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace src.Web.Common.Models.AccountViewModels
{
    public class MultipleLoginViewModel
    {
        [Display(Name = "Username or phone number")]
        [Required(ErrorMessage = "Please enter your username.")]
        public string UserName { get; set; }

        public LoginViewModel StaffLoginModel { get; set; }

        public ParentLoginViewModel ParentLoginViewModel { get; set; }

        public ParentActiveViewModel ParentActiveViewModel { get; set; }
    }
}
