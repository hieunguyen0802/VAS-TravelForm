using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using src.Core;
using src.Core.Domains;
using src.Core.Enums;
using src.Web.Common.Models.UserViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace src.Web.Common.Models.IncidentReportViewModel
{
    public class IncidentReportViewModel
    {
        public Guid IncidentReportId { get; set; }
        public string request_id { get; set; }
        public DateTime? createdOn { get; set; }
        public UserViewModel Requester { get; set; }
        public string emergencyContact { get; set; }
        public string phoneContact { get; set; }
        public string relationshipContact { get; set; }
        public string contactAddress { get; set; }
        public DateTime? departureDate { get; set; }
        public int CovidIncidentReportStatus { get; set; }
        public bool isReturnFromRedZone { get; set; }
        public bool isContactWithSuspectCase { get; set; }
        //reporter
        public string reporterProvinceId { get; set; }
        public string reporterDistrictId { get; set; }
        public string reporterWardId { get; set; }
        public string reporterHomeAddress { get; set; }
        public DateTime? dateOfStatus { get; set; }
        public RequestStatus status { get; set; }
        public string comment { get; set; }
        //RedZone
        public string redZoneProvinceId { get; set; }
        public string redZoneDistrictId { get; set; }
        public string redZoneWardId { get; set; }
        public string redZoneHomeAddress { get; set; }

        public string departureTransportation { get; set; }
        public string departureTicket { get; set; }
        public string departureTicketPath { get; set; }
        public DateTime? returningDate { get; set; }
        public string returningTransportaion { get; set; }
        public string returningTicket { get; set; }
        public string returningTicketPath { get; set; }

        public DateTime? contactTime { get; set; }
        public string informationReciever { get; set; }
        public string informationMedicalCenter { get; set; }
        public string guidanceMedicalCenter { get; set; }
        public string fTypeConfirmed { get; set; }
        public ConfirmToTest ConfirmToTest { get; set; }
        public testingStatus testingStatus { get; set; }
        public DateTime? appoinmentDateForTestResult { get; set; }
        public string othersTestResult { get; set; }

        public string testResultName { get; set; }
        public string testResultPath { get; set; }
        public string nameOfLineManager { get; set; }
        public string informedEmail { get; set; }
        public List<SelectListItem> lineManagerGroup { get; set; }
        public string currentHealthSituation { get; set; }
        public workSatus backToWorkStatus { get; set; }

        public DateTime? dateBackToWork { get; set; }
        public DateTime? estimatedDateBackToWork { get; set; }

        public SelectList Provinces { get; set; }
        public SelectList ProvincesWithOutHCM { get; set; }

        public string campusTemp { get; set; }
        public string positionTemp { get; set; }

        //ECSD
        public string ECSDEmail { get; set; }
        public string ECSDComment { get; set; }
        public DateTime? ECSDCommentDate { get; set; }
        public RequestStatus? ECSDVerifyStatus { get; set; }
        public List<SelectListItem> ECSDEmailList { get; set; }
        //HR
        public string HREmail { get; set; }
        public string HRComment { get; set; }
        public DateTime? HRCommentDate { get; set; }
        public RequestStatus? HRVerifyStatus { get; set; }

        public Guid travelId { get; set; }
        public Guid redZoneId { get; set; }

        public string travelRequestId { get; set; }

        public IList<routesAtRedZonesViewModel> routesAtRedZones { get; set; }
        public IList<routesOutsizeRedZonesViewModel> routesOutsizeRedZones { get; set; }
        public IList<informationContactSuspectCaseCovidViewModel> informationContactSuspectCaseCovid { get; set; }
        public IList<routesOfContactingWithColleaguesViewModel> routesOfContactingWithColleagues { get; set; }
    }

    public class routesAtRedZonesViewModel
    {
        public Guid routesAtRedZonesId { get; set; }
        public DateTime routeAtRedZoneDate { get; set; }
        public string routeAtRedZoneProvinceId { get; set; }
        public string routeAtRedZoneDistrictId { get; set; }
        public string routeAtRedZoneWardId { get; set; }
        public string routeAtRedZoneDateAddress { get; set; }
        public string routeAtRedZoneDatetransportation { get; set; }
        public string routeAtRedZoneFullAddress { get; set; }
        public Guid IncidentReportId { get; set; }
        public DateTime? routeAtRedZoneToDate { get; set; }
    }
    public class routesOutsizeRedZonesViewModel
    {
        public Guid routesOutsizeRedZonesId { get; set; }
        public DateTime routeOutsideRedZoneDate { get; set; }
        public string routeOutsideRedZoneProvinceId { get; set; }
        public string routeOutsideRedZoneDistrictId { get; set; }
        public string routeOutsideRedZoneWardId { get; set; }
        public string routeOutsideRedZoneDateAddress { get; set; }
        public string routeOutsideRedZoneDatetransportation { get; set; }
        public string routeOutsideRedZoneFullAddress { get; set; }
        public Guid IncidentReportId { get; set; }
        public DateTime? routeOutsideRedZoneToDate { get; set; }

    }
    public class informationContactSuspectCaseCovidViewModel
    {
        public Guid InformationContactSuspectCaseCovidId { get; set; }
        public DateTime suspectCaseDate { get; set; }
        public string suspectCase { get; set; }
        public string suspectCaseProvinceId { get; set; }
        public string suspectCaseDistrictId { get; set; }
        public string suspectCaseWardId { get; set; }
        public string suspectCaseAddress { get; set; }
        public string suspectCaseRelationShip { get; set; }
        public string suspectCaseContactSituation { get; set; }
        public string suspectCaseFullAddress { get; set; }
        public Guid IncidentReportId { get; set; }
        public DateTime? suspectCaseToDate { get; set; }
    }

    public class routesOfContactingWithColleaguesViewModel
    {
        public Guid routesOfContactingWithColleaguesId { get; set; }
        public DateTime routesOfContactingWithColleaguesDate { get; set; }
        public string routesOfContactingWithColleaguesInfor { get; set; }
        public string routesOfContactingWithColleaguesCampus { get; set; }
        public string routesOfContactingWithColleaguesProvinceId { get; set; }
        public string routesOfContactingWithColleaguesDistrictId { get; set; }
        public string routesOfContactingWithColleaguesWardId { get; set; }
        public string routesOfContactingWithColleaguesAddress { get; set; }
        public string routesOfContactingWithColleaguesContactSituation { get; set; }
        public string routesOfContactingWithColleaguesFullAddress { get; set; }
        public Guid IncidentReportId { get; set; }
        public DateTime? routesOfContactingWithColleaguesToDate { get; set; }

    }

}
