using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace src.Core.Domains
{
    public class Parents
    {
        [Key]
        public string id { get; set; }
        public string name { get; set; }
        public string hash { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string code { get; set; }
        public string created_by { get; set; }
        public string updated_by { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string language_id { get; set; }
        public string status { get; set; }
        public string relationship { get; set; }
        public Guid? avatar { get; set; }
    }
}
