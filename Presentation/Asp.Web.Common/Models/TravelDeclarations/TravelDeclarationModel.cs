using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using src.Web.Common.Helpers;
using src.Web.Common.Models.UserViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using src.Core.Enums;
namespace src.Web.Common.Models.TravelDeclarations
{

    public class TravellingRoutes
    {
        public Guid TravellingRouteId { get; set; }
        [JsonConverter(typeof(JsonDateTimeConverter))]
        public DateTime dateTravel { get; set; }
        [JsonConverter(typeof(JsonDateTimeConverter))]
        public DateTime? toDateTravel { get; set; }
        public string travelRouteProvinceId { get; set; }
        public string travelRouteDistrictId { get; set; }
        public string travelRouteWardId { get; set; }
        public string travelRouteAddress { get; set; }
        public string travelRouteFullAddress { get; set; }
        public string transportation { get; set; }
        public Guid travelDeclarationId { get; set; }
        public string travelRoutesNotes { get; set; }
        public string travelRouteCountryId { get; set; }
        public string travelRouteCity { get; set; }

    }
    public class TravelDeclarationModel
    {
        public Guid TravelDeclarationId { get; set; }
        public string request_id { get; set; }
        public DateTime? createdOn { get; set; }
        public int RequesterId { get; set; }
        public UserViewModel Requester { get; set; }
        public string userName { get; set; }
        [DataType(DataType.Date)]
        public DateTime departureDate { get; set; }
        public string travelFrom { get; set; }
        public string travelTo { get; set; }
        public string travelFromCountryId { get; set; }
        public string travelToCountryId { get; set; }


        public string departureTransportation { get; set; }
        public string departureTicket { get; set; }
        public string departureTicketPath { get; set; }
        public DateTime returningDate { get; set; }
        public string returningTransportaion { get; set; }
        public string returningTicket { get; set; }
        public string returningTicketPath { get; set; }
        public IList<TravellingRoutes> travellingRoutes { get; set; }
        public DateTime backToWorkDate { get; set; }
        public string nameOfLineManager { get; set; }
        public string informedEmail { get; set; }
        public List<SelectListItem> lineManagerGroup { get; set; }
        public DateTime dateOfStatus { get; set; }
        public string comment { get; set; }
        public RequestStatus status { get; set; }
        public string travelProvinceId { get; set; }
        public SelectList Provinces { get; set; }
        public SelectList Districts { get; set; }
        public SelectList Wards { get; set; }
        public string travelDistrictId { get; set; }
        public string travelWardId { get; set; }
        public string travelHomeAddress { get; set; }

        public int LatestStatus { get; set; }

        public SelectList Countries { get; set; }
        public string travelFromIntl { get; set; }
        public string travelToIntl { get; set; }

        public string campusTemp { get; set; }
        public string positionTemp { get; set; }

        //ECSD
        public string ECSDEmail { get; set; }
        public string ECSDComment { get; set; }
        public DateTime? ECSDCommentDate { get; set; }
        public List<SelectListItem> ECSDEmailList { get; set; }
        public RequestStatus? ECSDVerifyStatus { get; set; }

        //HR
        public string HREmail { get; set; }
        public string HRComment { get; set; }
        public DateTime? HRCommentDate { get; set; }
        public RequestStatus? HRVerifyStatus { get; set; }
        public List<SelectListItem> HREmailList { get; set; }

        //Cancel
        public string CancelComment { get; set; }
        public DateTime? CancelDate { get; set; }

        public RequestStatus? CancelStatus { get; set; }



        public TravelDeclarationModel()
        {
            ECSDEmailList = new List<SelectListItem>();
            HREmailList = new List<SelectListItem>();
        }


    }
    public class MyDateValidation : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            DateTime myDatetime;
            bool isParsed = DateTime.TryParseExact((string)value, "dd/MM/yyyy",
                                 new CultureInfo("en-US"),
                                 DateTimeStyles.None, out myDatetime);
            if (!isParsed)
                return false;
            return true;
        }
    }
    public class EnsureOneElementAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            var list = value as IList;
            if (list != null)
            {
                return list.Count > 0;
            }
            return false;
        }
    }

}
