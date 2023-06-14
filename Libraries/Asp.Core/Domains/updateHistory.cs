using System;
using System.Collections.Generic;
using System.Text;

namespace src.Core.Domains
{
    public class updateHistory
    {
		public Guid updateHistoryId { get; set; }
		public DateTime updatedDate { get; set; }

		public string campus { get; set; }
		public string travelNo { get; set; }
		public string incidentNo { get; set; }
		public string updatedField { get; set; }
		public string updatedValue { get; set; }
		public string updatedBy { get; set; }

		public Guid redZoneFollowUpId { get; set; }
		public Guid? travelId { get; set; }
		public Guid? incidentId { get; set; }

	}
}
