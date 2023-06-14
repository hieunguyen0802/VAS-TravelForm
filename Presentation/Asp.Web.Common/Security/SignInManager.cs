using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using src.Core.Domains;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;

namespace src.Web.Common.Security
{
    public class SignInManager : ISignInManager
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpContext context;
        public SignInManager(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            context = httpContextAccessor.HttpContext;
        }

        public async Task ParentSignInAsync(string email, string id, string roleName)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Sid, id),
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.GivenName, email)
            };

            claims.Add(new Claim(ClaimTypes.Role, roleName));
            var identity = new ClaimsIdentity(claims, "local", "name", "role");
            var principal = new ClaimsPrincipal(identity);

            await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

        public async Task SignInAsync(User user, IList<string> roleNames)
        {

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Sid, user.Id.ToString()),
                new Claim(ClaimTypes.Name,user.UserName),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                 new Claim(ClaimTypes.Email, user.UserName)
            };

            foreach (string roleName in roleNames)
            {
                claims.Add(new Claim(ClaimTypes.Role, roleName));
            }
            var identity = new ClaimsIdentity(claims, "local", "name", "role");
            var principal = new ClaimsPrincipal(identity);

            await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

        public async Task SignOutAsync()
        {
            await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }


    }
}
