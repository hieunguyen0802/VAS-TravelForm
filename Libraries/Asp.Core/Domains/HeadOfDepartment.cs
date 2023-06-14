using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace src.Core.Domains
{
    public class HeadOfDepartment
    {
        public Guid HeadOfDepartmentId { get; set; }
        public int UserId { get; set; }
        public virtual User User { get; set; }
        public string DepartmentOrCampus { get; set; }
    }
}
