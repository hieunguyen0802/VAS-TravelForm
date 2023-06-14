using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace src.Core.Domains
{
    public class TravellingRoute
    {
        [Key]
        public Guid TravellingRouteId { get; set; }
        public DateTime dateTravel { get; set; }
        public DateTime? toDateTravel { get; set; }
        public string TravelRouteProvinceId { get; set; }
        public string TravelRouteDistrictId { get; set; }
        public string TravelRouteWardId { get; set; }
        public string TravelRouteAddress { get; set; }
        public string TravelRouteFullAddress { get; set; }
        public string transportation { get; set; }
        public Guid TravelDeclarationId { get; set; }
        public string travelRoutesNotes { get; set; }
        public string travelRouteCountryId { get; set; }
        public string travelRouteCity { get; set; }

    }
}
