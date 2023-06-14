using System;

namespace src.Core
{
    public class DateTimeAdapter : IDateTime
    {
        public DateTime Now => DateTime.Now;
    }
}
