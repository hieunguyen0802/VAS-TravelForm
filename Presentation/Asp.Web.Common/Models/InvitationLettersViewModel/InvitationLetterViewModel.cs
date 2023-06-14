using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace src.Web.Common.Models.InvitationLettersViewModel
{
    public class InvitationLetterViewModel
    {
        [Key]
        public Guid Id { get; set; }
        [Required(ErrorMessage = "{0} is required")]
        public string Name { get; set; }
        [Display(Name ="Meeting Date")]
        //[Required(ErrorMessage = "{0} is required")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime MeetingDate { get; set; }
        //[Required(ErrorMessage = "Select Campus")]
        public string Campus { get; set; }
        public string Grade { get; set; }
        public string Class { get; set; }
        //[Required(ErrorMessage = "Select SchoolYear")]
        public string SchoolYear { get; set; }
        [Display(Name="Email Template")]
        //[CustomAttributeNoGuidEmpty]
        public Guid EmailTemplateId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
