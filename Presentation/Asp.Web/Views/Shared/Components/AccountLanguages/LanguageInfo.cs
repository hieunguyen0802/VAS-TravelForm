using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace src.Web.Views.Shared.Components.AccountLanguages
{
    public class LanguageInfo
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Icon { get; set; }
        public bool IsDefault { get; set; }
    }
}
