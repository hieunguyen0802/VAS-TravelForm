using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace src.Web.Extensions
{
    public static class AccountManagmentExtensions
    {
        public static string ExtensionGet(this UserPrincipal up, string key)
        {
            string value = null;
            MethodInfo mi = up.GetType()
                .GetMethod("ExtensionGet", BindingFlags.NonPublic | BindingFlags.Instance);

            Func<UserPrincipal, string, object[]> extensionGet = (k, v) =>
                ((object[])mi.Invoke(k, new object[] { v }));

            if (extensionGet(up, key).Length > 0)
            {
                value = (string)extensionGet(up, key)[0];
            }

            return value;
        }
    }
}
