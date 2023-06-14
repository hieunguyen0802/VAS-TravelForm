using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using src.Core;
using System.ComponentModel.DataAnnotations.Schema;
using src.Core.Enums;
using src.Web.Common.Models.UserViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using src.Core.Domains;

namespace src.Web.Common.Models.RedZone
{
    public class RedZoneFollowUpViewModel
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
        public string RegulatedAction { get; set;}
        public int QuarantineDuration { get; set; }
        public int VasQuarantineDuration { get; set; }

        public string InfoProvider { get; set;  }
        public string Notes { get; set; }


        public IncidentReport IncidentReport { get; set; }
        public string ProvinceName { get; set; }
        public string DistrictName { get; set; }
        public string WardName { get; set; }
        public string CountryName { get; set; }
        public string City { get; set; }


    }

}
