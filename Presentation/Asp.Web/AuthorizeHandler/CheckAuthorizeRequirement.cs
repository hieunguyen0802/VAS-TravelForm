using Microsoft.AspNetCore.Authorization;

namespace src.Web.AuthorizeHandler
{
    public class isApproverPermission : IAuthorizationRequirement { }
    public class isOwnerPermission : IAuthorizationRequirement { }
    public class isHeadOfDepartmentsPermission : IAuthorizationRequirement { }
    public class isECSDGroupPermission : IAuthorizationRequirement { }
    public class isApproverMultSelectRequest : IAuthorizationRequirement { }
    public class isApproverMultSelectRequestForECSD : IAuthorizationRequirement { }
    public class isHRDivisionGroups: IAuthorizationRequirement { }

    //
    public class Covid_isApproverPermission : IAuthorizationRequirement { }
    public class Covid_isOwnerPermission : IAuthorizationRequirement { }
    public class Covid_isHeadOfDepartmentsPermission : IAuthorizationRequirement { }
    public class Covid_isECSDGroupPermission : IAuthorizationRequirement { }
    public class Covid_isApproverMultSelectRequest : IAuthorizationRequirement { }
    public class Covid_isApproverMultSelectRequestForECSD : IAuthorizationRequirement { }
    public class Covid_isHRDivisionGroups : IAuthorizationRequirement { }
}
