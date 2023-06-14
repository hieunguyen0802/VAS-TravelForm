using Microsoft.AspNetCore.Mvc.Rendering;
using src.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace src.Core.Domains
{
    public class RedZoneFollowUp
    {
        [Key]
        public Guid RedZoneFollowUpId { get; set; }
        public DateTime? createdOn { get; set; }
        public string RequestId { get; set; }
        public string Employee { get; set; }
        public string Position { get; set; }
        public string Campus { get; set; }
        public DateTime? submittedDate { get; set; }
        public int status { get; set; }
        public string incidentRequest { get; set; }
        public bool isFollowUp { get; set; }
        public bool isRelated { get; set; }

        public Guid? RedZoneId { get; set; }
        public Guid? travelId { get; set; }
        public Guid? IncidentReportId { get; set; }

        //Declaration to Medical Center 
        public string FType { get; set; }
        public string FTypeByVas { get; set; }
        public string RegulatedAction { get; set; }
        public int? QuarantineDuration { get; set; }
        public int? VasQuarantineDuration { get; set; }

        public string InfoProvider { get; set; }
        public string Notes { get; set; }

        public IncidentReport IncidentReport { get; set; }

        
    }
}
