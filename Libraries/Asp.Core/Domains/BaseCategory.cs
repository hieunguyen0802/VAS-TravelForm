using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace src.Core.Domains
{
    public class BaseCategory
    {
        [Key]
        public string Code { get; set; }
        public string CategoryName { get; set; }
    }
}
