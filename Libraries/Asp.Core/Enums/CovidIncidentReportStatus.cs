using System;
using System.Collections.Generic;
using System.Text;

namespace src.Core.Enums
{
    public enum CovidIncidentReportStatus
    {
        returningFromRedZone = 1,
        contactingWithAsuspectCaseOfCovid = 2
    }
    public enum ConfirmToTest
    {
        yes = 1,
        no = 2
    }
    public enum testingStatus
    {
        tested = 1,
        schedule = 2,
        notYetTested = 3,
        others = 4,
        haveTestResult = 6,
        waitingTestResult = 7
    }
    public enum workSatus
    {
        backToWorkAlready = 1,
        notYetBackToWork = 2
    }
}
