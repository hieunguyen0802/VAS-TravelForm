using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace src.Core.Domains
{

    public class TravelDeclarationDashboardModel
    {
        [Key]
        //Travel
        public string Campus { get; set; }
        public int Submitted { get; set; }
        public int Verified { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int Cancelled { get; set; }
        public int Total { get; set; }

        //Covid
        
        public int ReturningFromRedZone { get; set; }
        public int ContactingWithSuspectCase { get; set; }
    }

}

