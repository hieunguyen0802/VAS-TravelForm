using Newtonsoft.Json;
using src.Core;
using src.Web.Common.Models.UserViewModels;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using src.Core.Enums;
namespace src.Web.Common.Models.TravelDeclarations
{
    public class TravelDeclarationListOfUser
    {
        public Guid TravelDeclarationId { get; set; }
        public string request_id { get; set; }
        [ForeignKey("RequesterId")]
        public int RequesterId { get; set; }
        public UserViewModel Requester { get; set; }
        [JsonConverter(typeof(JsonDateConverter))]
        public DateTime departureDate { get; set; }
        [JsonConverter(typeof(JsonDateConverter))]
        public DateTime returningDate { get; set; }
        [JsonConverter(typeof(JsonDateConverter))]
        public DateTime? createdOn { get; set; }
        public DateTime backToWorkDate { get; set; }
        public string nameOfLineManager { get; set; }
        public string ECSDEmail { get; set; }
        public DateTime dateOfStatus { get; set; }
        public RequestStatus? ECSDVerifyStatus { get; set; }
        public RequestStatus? HRVerifyStatus { get; set; }
        public RequestStatus status { get; set; }
        public RequestStatus currentStatus { get; set; }
        public string typeOfGroup { get; set; }
        public string isValidSubmit { get; set; }
    }
}
