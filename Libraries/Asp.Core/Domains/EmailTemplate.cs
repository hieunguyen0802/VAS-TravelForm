using System;
using System.ComponentModel.DataAnnotations;

namespace src.Core.Domains
{
    public partial class EmailTemplate
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string Campus { get; set; }
        public string Instruction { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        //public configs_email configs_email { get; set; }
    }
}
