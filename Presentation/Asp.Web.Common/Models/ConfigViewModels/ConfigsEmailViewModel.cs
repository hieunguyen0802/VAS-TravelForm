using Microsoft.AspNetCore.Mvc.Rendering;
using src.Web.Common.Models.EmailTemplateViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace src.Web.Common.Models.ConfigViewModels
{
    public class ConfigsEmailViewModel
    {
        public Guid configs_email_id { get; set; }

        [Display(Name = "Email template for submit")]
        public  Guid forSubmittedId { get; set; }
        public virtual EmailTemplateViewModel forSubmitted { get; set; }

        [Display(Name = "Email template for line manager approve")]
        public  Guid forApprovedId { get; set; }
        public virtual EmailTemplateViewModel forApproved { get; set; }

        [Display(Name = "Email template for line manager reject")]
        public Guid forRejectedId { get; set; }
        public EmailTemplateViewModel forRejected { get; set; }

        [Display(Name = "The email template announce to ECSD")]
        public Guid forRequestToECSDId { get; set; }
        public EmailTemplateViewModel forRequestToECSD { get; set; }

        [Display(Name = "Email template for ECSD approve")]
        public Guid forECSDApprovedId { get; set; }
        public EmailTemplateViewModel forECSDApproved { get; set; }

        [Display(Name = "Email template for ECSD reject")]
        public Guid forECSDRejectedId { get; set; }
        public EmailTemplateViewModel forECSDRejected { get; set; }

        [Display(Name = "Email template for Cancel")]
        public virtual Guid? forCancelledId { get; set; }
        public virtual EmailTemplateViewModel forCancelled { get; set; }
        public DateTime createdAt { get; set; }
        public string createdBy { get; set; }
        public SelectList listEmails { set; get; }
    }
}
