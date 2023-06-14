using Newtonsoft.Json;
using src.Core;
using src.Core.Enums;
using src.Web.Common.Models.UserViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace src.Web.Common.Models.IncidentReportViewModel
{
    public class IncidentReportListDto
    {
        public Guid IncidentReportId { get; set; }
        public string request_id { get; set; }
        [ForeignKey("RequesterId")]
        public int RequesterId { get; set; }
        public UserViewModel Requester { get; set; }
        [JsonConverter(typeof(JsonDateConverter))]
        public DateTime departureDate { get; set; }
        [JsonConverter(typeof(JsonDateConverter))]
        public DateTime returningDate { get; set; }
        public DateTime backToWorkDate { get; set; }
        public string nameOfLineManager { get; set; }
        public string ECSDEmail { get; set; }
        public DateTime dateOfStatus { get; set; }
        public RequestStatus? ECSDVerifyStatus { get; set; }
        public RequestStatus? HRVerifyStatus { get; set; }
        public RequestStatus status { get; set; }
        public RequestStatus currentStatus { get; set; }
        public string typeOfGroup { get; set; }

    }

}
