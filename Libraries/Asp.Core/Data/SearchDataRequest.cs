using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text;


namespace src.Core.Data
{
    public class SearchDataRequest
    {
        public DateTime? from { get; set; }
        public DateTime? to { get; set; }
        public SelectList provinces { get; set; }
        public string travelLocation { get; set; }
        public string cityOfTravelRoutes { get; set; }
        public string districtOfTravelRoutes { get; set; }
        public string wardOfTravelRoutes { get; set; }

        //redzone
        public string redZone { get; set; }
        

    }
}
