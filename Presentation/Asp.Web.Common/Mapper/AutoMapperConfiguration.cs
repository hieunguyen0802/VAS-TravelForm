using System.Collections.Generic;
using System.Linq;
using src.Core.Domains;
using src.Web.Common.Models.EmailTemplateViewModels;
using src.Web.Common.Models.LogViewModels;
using src.Web.Common.Models.SettingViewModels;
using src.Web.Common.Models.UserViewModels;
using src.Web.Common.Models.RedZone;
using src.Web.Common.Models.TravelDeclarations;
using src.Web.Common.Models.ConfigViewModels;
using src.Web.Common.Models.IncidentReportViewModel;
using src.Core.Enums;

namespace src.Web.Common.Mapper
{
    public class MappingProfile : AutoMapper.Profile
    {
        public MappingProfile()
        {
            // ----- EmailTemplate -----
            CreateMap<EmailTemplate, EmailTemplateViewModel>();
            CreateMap<EmailTemplateViewModel, EmailTemplate>();
            // ----- Log -----
            CreateMap<Log, LogViewModel>();

            // ----- Setting -----
            CreateMap<Setting, SettingViewModel>();
            CreateMap<SettingViewModel, Setting>();

            //RedZone
            CreateMap<redZone, RedZoneModel>();
            CreateMap<RedZoneModel, redZone>();

            CreateMap<RedZoneFollowUp, RedZoneFollowUpViewModel>();
            CreateMap<RedZoneFollowUpViewModel, RedZoneFollowUp>();

            CreateMap<IncidentReportViewModel, RedZoneFollowUpViewModel>();

            CreateMap<TravelDeclaration, TravelDeclarationModel>();
            CreateMap<TravelDeclarationModel, TravelDeclaration>();

            CreateMap<TravelDeclaration, TravelDeclarationListOfUser>()
                .ForMember(dest => dest.currentStatus, o => o.ResolveUsing(convertStatusForDeclaration))
                .ForMember(dest => dest.isValidSubmit, o => o.ResolveUsing(checkSubmitDate))
                .ForMember(dest => dest.typeOfGroup, opt => opt.MapFrom(src => (src.Requester.UserCode.Substring(0, 3).Contains("FEM") || src.Requester.UserCode.Substring(0, 3).Contains("VEM")) ? "CAM" : "MOET"));
            CreateMap<TravelDeclarationListOfUser, TravelDeclaration>();

            CreateMap<TravelDeclaration, RedZoneFollowUp>();
            CreateMap<RedZoneFollowUp, TravelDeclaration>();

            CreateMap<RedZoneFollowUp, updateHistory>();

            CreateMap<IncidentReport, IncidentReportViewModel>();
            CreateMap<IncidentReportViewModel, IncidentReport>();

            CreateMap<User, UserViewModel>();
            CreateMap<UserViewModel, User>();


            CreateMap<configs_email, ConfigsEmailViewModel>();
            CreateMap<ConfigsEmailViewModel, configs_email>();

            CreateMap<TravellingRoute, TravellingRoutes>();
            CreateMap<TravellingRoutes, TravellingRoute>();

            CreateMap<routesAtRedZones, routesAtRedZonesViewModel>();
            CreateMap<routesAtRedZonesViewModel, routesAtRedZones>();

            CreateMap<routesOutsizeRedZones, routesOutsizeRedZonesViewModel>();
            CreateMap<routesOutsizeRedZonesViewModel, routesOutsizeRedZones>();

            CreateMap<informationContactSuspectCaseCovid, informationContactSuspectCaseCovidViewModel>();
            CreateMap<informationContactSuspectCaseCovidViewModel, informationContactSuspectCaseCovid>();

            CreateMap<routesOfContactingWithColleagues, routesOfContactingWithColleaguesViewModel>();
            CreateMap<routesOfContactingWithColleaguesViewModel, routesOfContactingWithColleagues>();

            CreateMap<IncidentReport, IncidentReportListDto>()
             .ForMember(dest => dest.currentStatus, o => o.ResolveUsing(convertStatusForIncidentReports))

              .ForMember(dest => dest.typeOfGroup, opt => opt.MapFrom(src => (src.Requester.UserCode.Substring(0, 3).Contains("FEM") || src.Requester.UserCode.Substring(0, 3).Contains("VEM")) ? "CAM" : "MOET")); ;
            CreateMap<IncidentReportListDto, IncidentReport>();

            // ----- User -----

            CreateMap<User, UserViewModel>()
                .ForMember(dest => dest.AuthorizedRoleIds,
                    mo => mo.MapFrom(src =>
                        src.UserRoles != null ? src.UserRoles.Select(r => r.RoleId).ToList() : new List<int>()));

            CreateMap<User, UserCreateUpdateViewModel>();
            CreateMap<UserCreateUpdateViewModel, User>()
                .ForMember(dest => dest.UserName, mo => mo.MapFrom(src => src.UserName.ToLowerInvariant()))
                .ForMember(dest => dest.LastLoginDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore());
        }

        private static object checkSubmitDate(TravelDeclaration model)
        {

            if (model != null)
            {
                if (model.createdOn != null)
                {
                    if (((model.departureDate.Date - model.createdOn.Value.Date).Days) < 5)
                    {
                        return "Invalid";
                    }
                }
                return "Valid";
            }
            return "Invalid";
        }

        private static object convertStatusForDeclaration(TravelDeclaration TravelDeclarationModels)
        {
            var status = TravelDeclarationModels.status;
            if (TravelDeclarationModels.status == (int)RequestStatus.lineManager_Approved || TravelDeclarationModels.status == (int)RequestStatus.lineManager_Rejected)
            {
                status = TravelDeclarationModels.status;
            }
            if (TravelDeclarationModels.ECSDVerifyStatus != null)
            {
                status = (int)TravelDeclarationModels.ECSDVerifyStatus.Value;
            }

            if (TravelDeclarationModels.HRVerifyStatus != null)
            {
                status = (int)TravelDeclarationModels.HRVerifyStatus.Value;
            }

            if (TravelDeclarationModels.CancelStatus != null)
            {
                status = (int)TravelDeclarationModels.CancelStatus.Value;
            }
            
            return status;
        }
        private static object convertStatusForIncidentReports(IncidentReport incidentReportModels)
        {
            var status = 0;

            if ((incidentReportModels.status == (int)RequestStatus.lineManager_Approved || incidentReportModels.status == (int)RequestStatus.lineManager_Rejected ||
           incidentReportModels.status == (int)RequestStatus.Submitted) && incidentReportModels.ECSDVerifyStatus == null && incidentReportModels.HRVerifyStatus == null)
            {
                status = incidentReportModels.status;
            }
            if (incidentReportModels.ECSDVerifyStatus != null)
            {
                status = (int)incidentReportModels.ECSDVerifyStatus.Value;
            }

            if (incidentReportModels.HRVerifyStatus != null)
            {
                status = (int)incidentReportModels.HRVerifyStatus.Value;
            }
            return status;
        }

      
    }

}