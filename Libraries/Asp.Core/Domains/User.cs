using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace src.Core.Domains
{
    public partial class User
    {
        public User()
        {
            UserRoles = new HashSet<UserRole>();
            HeadOfDepartments = new Collection<HeadOfDepartment>();
        }

        public int Id { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string UserCode { get; set; }
        public string Position { get; set; }
        public string Campus { get; set; }
        public string Mobile { get; set; }
        public string informed1 { get; set; }
        public string informed2 { get; set; }
        public string informed3 { get; set; }
        public string informed4 { get; set; }
        public string informed5 { get; set; }
        public string informed6 { get; set; }
        public string linemanager { get; set; }
        public string ecsd { get; set; }

        public virtual ICollection<UserRole> UserRoles { get; set; }
        public virtual ICollection<HeadOfDepartment> HeadOfDepartments { get; set; }
    }
}
