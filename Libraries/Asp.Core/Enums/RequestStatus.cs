using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace src.Core.Enums
{
    public enum RequestStatus
    {
        Submitted = 1,

        [Display(Name = "Verified")]
        lineManager_Approved = 2,

        [Display(Name = "Rejected")]
        lineManager_Rejected = 3,

        [Display(Name = "Approved")]
        ecsd_Approved = 4,

        [Display(Name = "Rejected")]
        ecsd_Rejected = 5,

        [Display(Name = "HR Division approved")]
        hr_approved = 6,

        [Display(Name = "HR Division rejected")]
        hr_reject = 7,

        [Display(Name = "sent request to ECSD/DD")]
        sent_request_to_ECSD = 8,

        [Display(Name = "Cancelled")]
        cancelled = 9
    }
}
