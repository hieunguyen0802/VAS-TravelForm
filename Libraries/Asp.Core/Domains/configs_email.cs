using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using System.Text;

namespace src.Core.Domains
{
    public class configs_email
    {
        [Key]
        public Guid configs_email_id { get; set; }
        [ForeignKey("forSubmitted")]
        public virtual  Guid forSubmittedId { get; set; }
        [ForeignKey("forApproved")]
        public virtual Guid forApprovedId { get; set; }
        [ForeignKey("forRejected")]
        public virtual Guid forRejectedId { get; set; }
        [ForeignKey("forECSDApproved")]
        public virtual Guid forECSDApprovedId { get; set; }
        [ForeignKey("forRequestToECSD")]
        public virtual Guid forRequestToECSDId { get; set; }
        [ForeignKey("forECSDRejected")]
        public virtual Guid forECSDRejectedId { get; set; }
        [ForeignKey("forCancelled")]
        public virtual Guid? forCancelledId { get; set; }
        [ForeignKey("forRedzone")]
        public virtual Guid? forRedZoneId{ get; set; }


        public virtual EmailTemplate forSubmitted { get; set; }
        public virtual EmailTemplate forApproved { get; set; }
        public virtual EmailTemplate forRejected { get; set; }
        public virtual EmailTemplate forECSDApproved { get; set; }
        public virtual EmailTemplate forECSDRejected { get; set; }
        public virtual EmailTemplate forRequestToECSD { get; set; }
        public virtual EmailTemplate forCancelled { get; set; }

        public virtual EmailTemplate forRedzone { get; set; }





        public DateTime createdAt { get; set; }
        public string createdBy { get; set; }
    }
}
