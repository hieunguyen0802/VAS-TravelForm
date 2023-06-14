using System;
using System.ComponentModel.DataAnnotations;
using src.Localization.Resources;

namespace src.Web.Common.Models.AccountViewModels
{
    public class SetPasswordViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessageResourceName = "EnterPassword", ErrorMessageResourceType = typeof(SharedResource))]
        [Display(Name = "Password")]
        [StringLength(100, MinimumLength = 6, ErrorMessageResourceName = "Password_atleast", ErrorMessageResourceType = typeof(SharedResource))]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "ConfirmPassword")]
        [Required(ErrorMessageResourceName = "EnterConfirmPassword", ErrorMessageResourceType = typeof(SharedResource))]
        [Compare("Password", ErrorMessageResourceName = "ConfirmPassword_Message", ErrorMessageResourceType = typeof(SharedResource))]
        public string ConfirmPassword { get; set; }
    }
}
