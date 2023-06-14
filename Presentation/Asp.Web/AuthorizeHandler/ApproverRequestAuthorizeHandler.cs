using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Novell.Directory.Ldap;
using src.Core;
using src.Core.Domains;
using src.Data;
using src.Repositories.Users;
using src.Web.Common.Models.IncidentReportViewModel;
using src.Web.Common.Models.TravelDeclarations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace src.Web.AuthorizeHandler
{
    public class ApproverRequestAuthorizeHandler : AuthorizationHandler<isApproverPermission, TravelDeclarationModel>
    {
        readonly IDbContext _context;
        readonly IHttpContextAccessor _contextAccessor;

        public ApproverRequestAuthorizeHandler(IDbContext context, IHttpContextAccessor ca)
        {
            _context = context;
            _contextAccessor = ca;

        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, isApproverPermission requirement, TravelDeclarationModel resource)
        {

            if (!string.IsNullOrEmpty(resource.nameOfLineManager) && resource.nameOfLineManager.Split('@')[0].Contains(context.User.FindFirst(ClaimTypes.Email).Value.ToString()))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
            return Task.CompletedTask;
        }

    }
    public class isApproverMultiSelectRequestAuthorizeHandler : AuthorizationHandler<isApproverMultSelectRequest, IEnumerable<TravelDeclarationListOfUser>>
    {
        readonly IDbContext _context;
        readonly IHttpContextAccessor _contextAccessor;

        public isApproverMultiSelectRequestAuthorizeHandler(IDbContext context, IHttpContextAccessor ca)
        {
            _context = context;
            _contextAccessor = ca;

        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, isApproverMultSelectRequest requirement, IEnumerable<TravelDeclarationListOfUser> resource)
        {
            if (resource == null && resource.Count() < 0)
            {
                context.Fail();
            }

            if (resource.Any(x => x.nameOfLineManager != null && x.nameOfLineManager.Split('@')[0].Contains(context.User.FindFirst(ClaimTypes.Email).Value.ToString())))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
            return Task.CompletedTask;
        }
    }

    public class isApproverMultiSelectForECSDRequestAuthorizeHandler : AuthorizationHandler<isApproverMultSelectRequestForECSD, IEnumerable<TravelDeclarationListOfUser>>
    {
        readonly IDbContext _context;
        readonly IHttpContextAccessor _contextAccessor;

        public isApproverMultiSelectForECSDRequestAuthorizeHandler(IDbContext context, IHttpContextAccessor ca)
        {
            _context = context;
            _contextAccessor = ca;

        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, isApproverMultSelectRequestForECSD requirement, IEnumerable<TravelDeclarationListOfUser> resource)
        {
            if (resource == null && resource.Count() < 0)
            {
                context.Fail();
            }
            else
            if (resource.Any(t => t.ECSDEmail.Split('@')[0] == context.User.FindFirst(ClaimTypes.Email).Value.ToString() && t.ECSDEmail != null))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
            return Task.CompletedTask;
        }
    }


    public class isECSDRequestAuthorizeHandler : AuthorizationHandler<isECSDGroupPermission, TravelDeclarationModel>
    {
        readonly IDbContext _context;
        readonly IHttpContextAccessor _contextAccessor;

        public isECSDRequestAuthorizeHandler(IDbContext context, IHttpContextAccessor ca)
        {
            _context = context;
            _contextAccessor = ca;

        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, isECSDGroupPermission requirement, TravelDeclarationModel resource)
        {
            if (string.IsNullOrEmpty(resource.ECSDEmail))
            {
                context.Fail();
            }
            else if (context.User.FindFirst(ClaimTypes.Email).Value.ToString() == resource.ECSDEmail.Split('@')[0])
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
            return Task.CompletedTask;
        }
    }

    public class IsOwerRequestAuthorizeHandler : AuthorizationHandler<isOwnerPermission, TravelDeclarationModel>
    {
        readonly IDbContext _context;
        readonly IHttpContextAccessor _contextAccessor;
        public IsOwerRequestAuthorizeHandler(IDbContext context, IHttpContextAccessor ca)
        {
            _context = context;
            _contextAccessor = ca;
        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, isOwnerPermission requirement, TravelDeclarationModel resource)
        {
            string[] informedEmail = new string[] { "" };
            if (resource.informedEmail != null)
            {
                informedEmail = resource.informedEmail.Split(",");
                if (informedEmail != null && informedEmail.Count() > 0)
                {
                    if (informedEmail.Any(t => t.ToUpper().Split('@')[0].Equals(context.User.FindFirst(ClaimTypes.Email).Value.ToString().ToUpper())))
                    {
                        context.Succeed(requirement);
                        return Task.CompletedTask;
                    }
                }
            }
            if (context.User.FindFirst(ClaimTypes.Sid).Value.ToString() == resource.RequesterId.ToString())
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
            return Task.CompletedTask;
        }
    }
    public class IsHeadOfDepartmentRequestAuthorizeHandler : AuthorizationHandler<isHeadOfDepartmentsPermission, TravelDeclarationModel>
    {
        readonly IDbContext _context;
        readonly IHttpContextAccessor _contextAccessor;
        readonly IUserRepository _userRepository;
        public IsHeadOfDepartmentRequestAuthorizeHandler(IDbContext context, IHttpContextAccessor ca, IUserRepository userRepository)
        {
            _context = context;
            _contextAccessor = ca;
            _userRepository = userRepository;
        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, isHeadOfDepartmentsPermission requirement, TravelDeclarationModel resource)
        {
            var userId = int.Parse(context.User.FindFirst(ClaimTypes.Sid).Value.ToString());
            var entity = _userRepository.GetUserByIdAsync(userId);
            var head_of_campus = entity.Result.HeadOfDepartments.Select(t => t.DepartmentOrCampus).ToArray();

            var requester = _userRepository.GetUserByIdAsync(resource.RequesterId);
            var campus_requester = requester.Result.Campus;
            if (head_of_campus.Contains(campus_requester))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
            return Task.CompletedTask;
        }
    }

    //
    public class isHRDivisionGroupsRequestAuthorizeHandler : AuthorizationHandler<isHRDivisionGroups>
    {
        private readonly userGroupSettings _userGroupSettings;
        public isHRDivisionGroupsRequestAuthorizeHandler(IOptions<userGroupSettings> userGroupSettings)
        {
            _userGroupSettings = userGroupSettings.Value;
        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, isHRDivisionGroups requirement)
        {
            var userName = context.User.FindFirst(ClaimTypes.Name).Value.ToString();

            if (_userGroupSettings.HRGroup.FirstOrDefault(t => t.ToUpper() == userName.ToUpper()) != null)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
            return Task.CompletedTask;
        }
    }

    //==================Covid========================

    public class covid_IsOwerRequestAuthorizeHandler : AuthorizationHandler<Covid_isOwnerPermission, IncidentReportViewModel>
    {
        readonly IDbContext _context;
        readonly IHttpContextAccessor _contextAccessor;
        public covid_IsOwerRequestAuthorizeHandler(IDbContext context, IHttpContextAccessor ca)
        {
            _context = context;
            _contextAccessor = ca;
        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, Covid_isOwnerPermission requirement, IncidentReportViewModel resource)
        {
            if (context.User.FindFirst(ClaimTypes.Sid).Value.ToString() == resource.Requester.Id.ToString())
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
            return Task.CompletedTask;
        }
    }

    public class Covid_isApproverMultiSelectRequestAuthorizeHandler : AuthorizationHandler<Covid_isApproverMultSelectRequest, IEnumerable<IncidentReportListDto>>
    {
        readonly IDbContext _context;
        readonly IHttpContextAccessor _contextAccessor;

        public Covid_isApproverMultiSelectRequestAuthorizeHandler(IDbContext context, IHttpContextAccessor ca)
        {
            _context = context;
            _contextAccessor = ca;

        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, Covid_isApproverMultSelectRequest requirement, IEnumerable<IncidentReportListDto> resource)
        {
            if (resource == null && resource.Count() < 0)
            {
                context.Fail();
            }

            if (resource.Any(x => x.nameOfLineManager != null && x.nameOfLineManager.ToUpper().Split('@')[0].Contains(context.User.FindFirst(ClaimTypes.Email).Value.ToString().ToUpper())))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
            return Task.CompletedTask;
        }
    }

    public class covid_ApproverRequestAuthorizeHandler : AuthorizationHandler<Covid_isApproverPermission, IncidentReportViewModel>
    {
        readonly IDbContext _context;
        readonly IHttpContextAccessor _contextAccessor;

        public covid_ApproverRequestAuthorizeHandler(IDbContext context, IHttpContextAccessor ca)
        {
            _context = context;
            _contextAccessor = ca;

        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, Covid_isApproverPermission requirement, IncidentReportViewModel resource)
        {
            if (string.IsNullOrEmpty(resource.nameOfLineManager))
            {
                context.Fail();
            }
            else if (context.User.FindFirst(ClaimTypes.Email).Value.ToString().ToUpper() == resource.nameOfLineManager.ToUpper().Split('@')[0])
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
            return Task.CompletedTask;
        }

    }


    public class Covid_isApproverMultiSelectForECSDRequestAuthorizeHandler : AuthorizationHandler<Covid_isApproverMultSelectRequestForECSD, IEnumerable<IncidentReportListDto>>
    {
        readonly IDbContext _context;
        readonly IHttpContextAccessor _contextAccessor;

        public Covid_isApproverMultiSelectForECSDRequestAuthorizeHandler(IDbContext context, IHttpContextAccessor ca)
        {
            _context = context;
            _contextAccessor = ca;

        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, Covid_isApproverMultSelectRequestForECSD requirement, IEnumerable<IncidentReportListDto> resource)
        {
            if (resource == null && resource.Count() < 0)
            {
                context.Fail();
            }
            else
            if (resource.Any(t => t.ECSDEmail.ToUpper().Split('@')[0] == context.User.FindFirst(ClaimTypes.Email).Value.ToString().ToUpper() && t.ECSDEmail != null))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
            return Task.CompletedTask;
        }
    }
    public class covid_isECSDRequestAuthorizeHandler : AuthorizationHandler<Covid_isECSDGroupPermission, IncidentReportViewModel>
    {
        readonly IDbContext _context;
        readonly IHttpContextAccessor _contextAccessor;

        public covid_isECSDRequestAuthorizeHandler(IDbContext context, IHttpContextAccessor ca)
        {
            _context = context;
            _contextAccessor = ca;

        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, Covid_isECSDGroupPermission requirement, IncidentReportViewModel resource)
        {
            if (string.IsNullOrEmpty(resource.ECSDEmail))
            {
                context.Fail();
            }
            else if (context.User.FindFirst(ClaimTypes.Email).Value.ToString() == resource.ECSDEmail.Split('@')[0])
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
            return Task.CompletedTask;
        }
    }

    public class covid_IsHeadOfDepartmentRequestAuthorizeHandler : AuthorizationHandler<Covid_isHeadOfDepartmentsPermission, IncidentReportViewModel>
    {
        readonly IDbContext _context;
        readonly IHttpContextAccessor _contextAccessor;
        readonly IUserRepository _userRepository;
        public covid_IsHeadOfDepartmentRequestAuthorizeHandler(IDbContext context, IHttpContextAccessor ca, IUserRepository userRepository)
        {
            _context = context;
            _contextAccessor = ca;
            _userRepository = userRepository;
        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, Covid_isHeadOfDepartmentsPermission requirement, IncidentReportViewModel resource)
        {
            var userId = int.Parse(context.User.FindFirst(ClaimTypes.Sid).Value.ToString());
            var entity = _userRepository.GetUserByIdAsync(userId);
            var head_of_campus = entity.Result.HeadOfDepartments.Select(t => t.DepartmentOrCampus).ToArray();

            var requester = _userRepository.GetUserByIdAsync(resource.Requester.Id);
            var campus_requester = requester.Result.Campus;
            if (head_of_campus.Contains(campus_requester))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
            return Task.CompletedTask;
        }
    }
    public class Covid_IsOwerRequestAuthorizeHandler : AuthorizationHandler<Covid_isOwnerPermission, IncidentReportViewModel>
    {
        readonly IDbContext _context;
        readonly IHttpContextAccessor _contextAccessor;
        public Covid_IsOwerRequestAuthorizeHandler(IDbContext context, IHttpContextAccessor ca)
        {
            _context = context;
            _contextAccessor = ca;
        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, Covid_isOwnerPermission requirement, IncidentReportViewModel resource)
        {
            string[] informedEmail = new string[] { "" };
           
            if (context.User.FindFirst(ClaimTypes.Sid).Value.ToString() == resource.Requester.Id.ToString())
            {
                context.Succeed(requirement);
            }
            else if (resource.informedEmail != null)
            {
                informedEmail = resource.informedEmail.Split(",");
                if (informedEmail != null && informedEmail.Count() > 0)
                {
                    if (informedEmail.Any(t => t.ToUpper().Split('@')[0].Equals(context.User.FindFirst(ClaimTypes.Email).Value.ToString().ToUpper())))
                    {
                        context.Succeed(requirement);
                    }
                }
            }
            else
            {
                context.Fail();
            }
            return Task.CompletedTask;
        }
    }
    public class Covid_isHRDivisionGroupsRequestAuthorizeHandler : AuthorizationHandler<Covid_isHRDivisionGroups>
    {
        private readonly userGroupSettings _userGroupSettings;
        public Covid_isHRDivisionGroupsRequestAuthorizeHandler(IOptions<userGroupSettings> userGroupSettings)
        {
            _userGroupSettings = userGroupSettings.Value;
        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, Covid_isHRDivisionGroups requirement)
        {
            var userName = context.User.FindFirst(ClaimTypes.Name).Value.ToString();

            if (_userGroupSettings.HRGroup.FirstOrDefault(t => t == userName) != null)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
            return Task.CompletedTask;
        }
    }
}



