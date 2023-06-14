using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace src.Core
{
    public class JsonDateConverter : IsoDateTimeConverter
    {
        public JsonDateConverter()
        {
            DateTimeFormat = "dd/MM/yyyy";
        }
    }
}
