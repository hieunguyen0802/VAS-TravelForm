using System.ComponentModel.DataAnnotations;
using src.Localization.Resources;
namespace src.Web.Common.Models.AccountViewModels
{
    public class ParentLoginViewModel
    {
        [Display(Name = "UserName")]
        [Required(ErrorMessageResourceName = "EnterUsername", ErrorMessageResourceType = typeof(SharedResource))]
        [RegularExpression(@"^[0-9a-zA-Z\.]*$", ErrorMessageResourceName = "RegularExpression_Username", ErrorMessageResourceType = typeof(SharedResource))]
        public string PhoneNumber { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [Required(ErrorMessageResourceName = "EnterPassword", ErrorMessageResourceType = typeof(SharedResource))]
        public string Password { get; set; }
    }
}
