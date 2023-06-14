using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace src.Web.Common.Models.AccountViewModels
{
    public class StaffLoginViewModel
    {
        [Display(Name = "Username")]
        [Required(ErrorMessage = "Please enter your username.")]
        [RegularExpression(@"^[0-9a-zA-Z\.]*$", ErrorMessage = "Only Alphabets and Numbers allowed.")]
        public string UserName { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [Required(ErrorMessage = "Please enter your password.")]
        public string Password { get; set; }
    }
}
