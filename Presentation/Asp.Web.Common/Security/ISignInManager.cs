using System.Collections.Generic;
using System.Threading.Tasks;
using src.Core.Domains;

namespace src.Web.Common.Security
{
    public interface ISignInManager
    {
        Task SignInAsync(User user, IList<string> roleNames);
        Task ParentSignInAsync(string email, string id, string roleName);
        Task SignOutAsync();


    }
}