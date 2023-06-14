using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace src.Web.Common.Models.InvitationLettersViewModel
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    sealed public class CustomAttributeNoGuidEmpty : ValidationAttribute
    {
        public override bool IsValid(Object value)
        {
            bool result = true;

            if ((Guid)value == Guid.Empty)
                result = false;

            return result;
        }
    }
}
