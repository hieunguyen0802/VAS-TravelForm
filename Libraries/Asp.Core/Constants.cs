using System;

namespace src.Core
{
    public class Constants
    {
        public static class Areas
        {
            public const string Administration = "Administration";
            public const string HRBP = "HRBP";
        }

        public static class EmailTemplates
        {
            public const string AddNewUserNotification = "Add New User Notification";
        }

        /// <summary>
        /// Cache time in minutes.
        /// </summary>
        public static class CacheTimes
        {
            public static TimeSpan DefaultTimeSpan = TimeSpan.FromMinutes(60);
        }

        public static class MainPages
        {
            // System pages
            public const string AccessDenied = "Access Denied";
            public const string AntiForgery = "Oops!";
            public const string Dashboard = "Dashboard";
            public const string EmailTemplates = "Email Templates";
            public const string EmailTemplateEdit = "Edit Email Template";
            public const string Error = "Error";
            public const string Login = "Login";
            public const string ForgotPassWord = "Forgot PassWord";
            public const string Logs = "Application Logs";
            public const string PageNotFound = "Page Not Found";
            public const string ReleaseHistory = "Release History";
            public const string Settings = "Settings";
            public const string SettingEdit = "Edit Setting";
            public const string Users = "Users";
            public const string UserCreate = "Add New User";
            public const string UserEdit = "Edit User";

            public const string News = "News";
            public const string NewsEdit = "Edit News";

            public const string InvitationLetter = "Invitation Letters";
            public const string InvitationLetterEdit = "Edit";

            // Application pages
            public const string Home = "Trường Quốc tế Việt Úc (VAS)";
            public const string Sample = "Sample";
            public const string ApplicationUsers = "Application Users";
        }

        public static class Messages
        {
            public const string Error = @"An error occurred while processing your request.If these issue persists, then please contact customer service.";

            public const string PageNotFound = @"Sorry, the page you're looking for cannot be found.If these issue persists, then please contact customer service.";

            public const string AntiForgery = @"You tried to submit the same page twice.You appear to have submitted a page twice (often caused by pressing the back button and trying again).To avoid getting these errors, simply refresh the page containing the form you wish to submit and try again.</p>";

            public const string AccessDenied = "You do not have access to the application.If you think this is an error, then please contact your manager";
        }

        public static class RoleNames
        {
            public const string Administrator = "Administrator";
            public const string Vasuser = "VAS";
            public const string Parent = "Parent";
            public const string HRBP = "HRBP";
        }
        public static class ContactEmailType
        {
            public const string HomeEmail = "Home";
            public const string FatherEmail = "Father Email";
            public const string MotherEmail = "Mother Email";
        }
        public static class StaticVariables
        {
            public const string AllCampus = "ALL";
        }
        public static class Permission
        {
            public const string EditPhoneNumber = "EditPhoneNumber";
        }

        public static class Permission_Workflow
        {
            public const string IsOwner = "IsOwner";
            public const string IsApprover = "IsApprover";
            public const string IsApproverForMultiSelect = "IsApproverForMultiSelect";
            public const string IsApproverForMultiSelectForECSD = "IsApproverForMultiSelectForECSD";
            public const string IsHeadOfDepartment = "IsHeadOfDepartment";
            public const string isECSDGroup = "isECSDGroup";
            public const string isHRDivisionGroups = "isHRDivisionGroups";
        }
        public static class Permission_Workflow_Covid
        {
            public const string IsOwner = "IsOwnerForCovid";
            public const string IsApprover = "IsApproverForCovid";
            public const string IsApproverForMultiSelect = "IsApproverForMultiSelectForCovidOfLineManager";
            public const string IsApproverForMultiSelectForECSD = "IsApproverForMultiSelectForCovidOfECSD";
            public const string IsHeadOfDepartment = "IsHeadOfDepartmentForCovid";
            public const string isECSDGroup = "isECSDGroupForCovid";
            public const string isHRDivisionGroups = "isHRDivisionGroupsForCovid";
        }
    }
}