using System.ComponentModel.DataAnnotations;
using src.Localization.Resources;
namespace src.Web.Common.Models.AccountViewModels
{
    public class ForgotPasswordViewModel
    {
        [Phone]
        [Display(Name = "UserName")]
        [Required(ErrorMessageResourceName = "EnterUsername", ErrorMessageResourceType = typeof(SharedResource))]
        [RegularExpression(@"^[0-9a-zA-Z\.]*$", ErrorMessageResourceName = "RegularExpression_Username", ErrorMessageResourceType = typeof(SharedResource))]
        public string PhoneNumber { get; set; }
    }
}
