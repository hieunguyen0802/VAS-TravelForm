using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;

namespace src.Web.Common.Models.UserViewModels
{
    public class UserCreateUpdateViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Username")]
        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(50, ErrorMessage = "{0} must not exceed {1} characters.")]
        public string UserName { get; set; }

        [Display(Name = "First Name")]
        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(50, ErrorMessage = "{0} must not exceed {1} characters.")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(50, ErrorMessage = "{0} must not exceed {1} characters.")]
        public string LastName { get; set; }

        [Display(Name = "Status")]
        public bool IsActive { get; set; }
        public List<SelectListItem> SelectedStatus { get; set; }

        [Display(Name = "Last Login Date")]
        [DisplayFormat(DataFormatString = "{0:M/d/yy}")]
        public DateTime LastLoginDate { get; set; }

        [Display(Name = "Created Date")]
        [DisplayFormat(DataFormatString = "{0:M/d/yy}")]
        public DateTime CreatedOn { get; set; }

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [Display(Name = "Last Updated Date")]
        [DisplayFormat(DataFormatString = "{0:M/d/yy}")]
        public DateTime ModifiedOn { get; set; }

        [Display(Name = "Last Updated By")]
        public string ModifiedBy { get; set; }

        [Display(Name = "Roles")]
        [Required, MinLength(1, ErrorMessage = "At least one item required in work order")]
        public IList<int> SelectedRoleIds { get; set; }
        public IList<SelectListItem> AvailableRoles { get; set; }
        [Display(Name = "EME code:")]
        public string UserCode { get; set; }
        [Display(Name = "Position")]
        public string Position { get; set; }
        [Display(Name = "Campus/ Dept.")]
        public string Campus { get; set; }
        [Display(Name = "Head Of Department/Campus")]
        public IList<string> SelectedCampusIds { get; set; }
        public IList<SelectListItem> HeadOfDepartment { get; set; } // change to select list

        [JsonConverter(typeof(EmptyToNullConverter))]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string informed1 { get; set; }

        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [JsonConverter(typeof(EmptyToNullConverter))]
        public string informed2 { get; set; }

        [JsonConverter(typeof(EmptyToNullConverter))]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string informed3 { get; set; }

        [JsonConverter(typeof(EmptyToNullConverter))]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string informed4 { get; set; }

        [JsonConverter(typeof(EmptyToNullConverter))]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string informed5 { get; set; }

        [JsonConverter(typeof(EmptyToNullConverter))]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string informed6 { get; set; }

        [Display(Name ="Line Manager")]
        [JsonConverter(typeof(EmptyToNullConverter))]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string linemanager { get; set; }

        [Display(Name = "ECSD")]
        [JsonConverter(typeof(EmptyToNullConverter))]
        [Required(ErrorMessage = "{0} is required.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string ecsd { get; set; }
        public UserCreateUpdateViewModel()
        {
            SelectedRoleIds = new List<int>();
            AvailableRoles = new List<SelectListItem>();
            SelectedStatus = new List<SelectListItem>
            {
                new SelectListItem{ Selected=true,Text="Active",Value="true",},
                new SelectListItem{ Selected = false,Text="InActive",Value="false"},
            };
        }
    }
    public class EmptyToNullConverter : JsonConverter
    {
        private JsonSerializer _stringSerializer = new JsonSerializer();

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType,
                                        object existingValue, JsonSerializer serializer)
        {
            string value = _stringSerializer.Deserialize<string>(reader);

            if (string.IsNullOrEmpty(value))
            {
                value = null;
            }

            return value;
        }

        public override void WriteJson(JsonWriter writer, object value,
                                       JsonSerializer serializer)
        {
            _stringSerializer.Serialize(writer, value);
        }
    }
}