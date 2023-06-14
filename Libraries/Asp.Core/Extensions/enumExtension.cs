using System;
using System.Collections.Generic;
using System.Text;

namespace src.Core.Extensions
{
    public static class enumExtension
    {
        public static string GetTypeText(this int value)
        {
            return "condition from enum method";
        }
    }
}
