using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Newtonsoft.Json;
using src.Core.Enums;
namespace src.Core.Domains
{
    public class TravelDeclaration
    {
        public Guid TravelDeclarationId { get; set; }
        public string request_id { get; set; }
        [ForeignKey("RequesterId")]
        public int RequesterId { get; set; }

        [JsonConverter(typeof(JsonDateConverter))]
        public DateTime? createdOn { get; set; }
        public User Requester { get; set; }
        public string travelFrom { get; set; }
        public string travelTo { get; set; }
        public string travelFromIntl { get; set; }
        public string travelToIntl { get; set; }

        public string travelFromCountryId { get; set; }
        public string travelToCountryId { get; set; }

        public DateTime departureDate { get; set; }
        public string departureTransportation { get; set; }
        public string departureTicket { get; set; }
        public string departureTicketPath { get; set; }
        public DateTime returningDate { get; set; }
        public string returningTransportaion { get; set; }
        public string returningTicket { get; set; }
        public string returningTicketPath { get; set; }
        public IList<TravellingRoute> travellingRoutes { get; set; }
        public string informedEmail { get; set; }
        public DateTime backToWorkDate { get; set; }
        public string nameOfLineManager { get; set; }
        public DateTime? dateOfStatus { get; set; }
        public string comment { get; set; }
        public int status { get; set; }
        public string travelProvinceId { get; set; }
        public string travelDistrictId { get; set; }
        public string travelWardId { get; set; }
        public string travelHomeAddress { get; set; }

        public string campusTemp { get; set; }
        public string positionTemp { get; set; }

        //ECSD
        public string ECSDEmail { get; set; }
        public string ECSDComment { get; set; }
        public DateTime? ECSDCommentDate { get; set; }
        public RequestStatus? ECSDVerifyStatus { get; set; }

        //HR
        public string HREmail { get; set; }
        public string HRComment { get; set; }
        public DateTime? HRCommentDate { get; set; }
        public RequestStatus? HRVerifyStatus { get; set; }

        //Cancel
        public string CancelComment { get; set; }
        public DateTime? CancelDate { get; set; }
        public RequestStatus? CancelStatus { get; set; }

        public int LatestStatus { get; set; }

        public Guid? redZoneId { get; set; }
        public Guid? incidentId { get; set; }

    }
}
