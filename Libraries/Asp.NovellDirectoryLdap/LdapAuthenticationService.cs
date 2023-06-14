using src.Repositories.Authentication;
using System.DirectoryServices.AccountManagement;
namespace src.NovellDirectoryLdap
{
    public class LdapAuthenticationService : IAuthenticationService
    {
        public bool ValidateUser(string domainName, string username, string password)
        {
            PrincipalContext pc = new PrincipalContext(ContextType.Domain, domainName);
            bool isValid = pc.ValidateCredentials(username, password);
            return isValid;

        }
    }
}