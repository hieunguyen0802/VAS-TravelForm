using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using src.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace src.Core.Domains
{
    public class redZone
    {
        public Guid redZoneId { get; set; }
        public string redZoneName { get; set; }
        [JsonConverter(typeof(JsonDateConverter))]
        public DateTime? createdAt { get; set; }
        [JsonConverter(typeof(JsonDateConverter))]
        public DateTime redZoneDate { get; set; }
        [JsonConverter(typeof(JsonDateConverter))]
        public DateTime redZoneToDate { get; set; }

        public isDomestic isDomestic { get; set; }
        public bool isActivate { get; set; }
        public bool isRedZoneOnTransportation { get; set; }


        public string redZoneProvinceId { get; set; }
        public string redZoneDistrictId { get; set; }
        public string redZoneWardId { get; set; }

        public string redZoneCountryId { get; set; }
        public string redZoneCity { get; set; }
        public string redZoneTransportation { get; set; }


        public IList<TravelDeclaration> travelDeclarations { get; set; }
        public IList<IncidentReport> incidentReports { get; set; }

        public bool isSendEmail { get; set; }



    }
}
