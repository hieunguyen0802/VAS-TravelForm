using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace src.Web.Common.Models.EmailTemplateViewModels
{
    public class EmailTemplateViewModel
    {
        [Key]
        public Guid Id { get; set; }

        [Display(Name = "Template Name")]
        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(50, ErrorMessage = "{0} must not exceed {1} characters.")]
        public string Name { get; set; }

        [Display(Name = "Description")]
        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(200, ErrorMessage = "{0} must not exceed {1} characters.")]
        public string Description { get; set; }

        [Display(Name = "Subject")]
        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(200, ErrorMessage = "{0} must not exceed {1} characters.")]
        public string Subject { get; set; }

        //[AllowHtml]
        [Display(Name = "body")]
        [Required(ErrorMessage = "Please enter body.")]
        public string Body { get; set; }

        //[AllowHtml]
        [Display(Name = "Instruction")]
        public string Instruction { get; set; }
        public string Campus { get; set; }
        public IList<SelectListItem> listCampus { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }

        public EmailTemplateViewModel()
        {
            listCampus = new List<SelectListItem>();
        }
    }
}
