using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using src.Core.Enums;


namespace src.Core.Domains
{
    public class IncidentReport
    {
        [Key]
        public Guid IncidentReportId { get; set; }
        [ForeignKey("RequesterId")]
        public string request_id { get; set; }
        public int RequesterId { get; set; }
        public User Requester { get; set; }
        public DateTime? createdOn { get; set; }
        public CovidIncidentReportStatus CovidIncidentReportStatus { get; set; }

        public bool isReturnFromRedZone { get; set; }
        public bool isContactWithSuspectCase { get; set; }

        //reporter
        public string reporterProvinceId { get; set; }
        public string reporterDistrictId { get; set; }
        public string reporterWardId { get; set; }
        public string reporterHomeAddress { get; set; }
        //redZone
        public string redZoneProvinceId { get; set; }
        public string redZoneDistrictId { get; set; }
        public string redZoneWardId { get; set; }
        public string redZoneHomeAddress { get; set; }

        public DateTime? dateOfStatus { get; set; }
        public int status { get; set; }
        public string comment { get; set; }
        public string emergencyContact { get; set; }
        public string phoneContact { get; set; }
        public string relationshipContact { get; set; }
        public string contactAddress { get; set; }
        public DateTime? departureDate { get; set; }
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
        public string currentHealthSituation { get; set; }
        public workSatus backToWorkStatus { get; set; }
        public DateTime? dateBackToWork { get; set; }
        public DateTime? estimatedDateBackToWork { get; set; }

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

        public IList<routesAtRedZones> routesAtRedZones { get; set; }
        public IList<routesOutsizeRedZones> routesOutsizeRedZones { get; set; }
        public IList<informationContactSuspectCaseCovid> informationContactSuspectCaseCovid { get; set; }
        public IList<routesOfContactingWithColleagues> routesOfContactingWithColleagues { get; set; }
    
        public Guid? travelId { get; set; }
        public Guid? redZoneId { get; set; }
    }



    public class routesAtRedZones
    {
        [Key]
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
    public class routesOutsizeRedZones
    {
        [Key]
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
    public class informationContactSuspectCaseCovid
    {
        [Key]
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

    public class routesOfContactingWithColleagues
    {
        [Key]
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
