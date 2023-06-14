using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using src.Core;
using src.Repositories.Authentication;
using src.Repositories.Domains;
using src.Repositories.Roles;
using src.Repositories.Users;
using src.Repositories.Category;
using src.Repositories.Messages;
using src.Web.Areas.Administration.Controllers;
using src.Web.Common.Models.AccountViewModels;
using src.Web.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using src.Web.Helpers;
using src.Web.Common.Mvc.Alerts;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using src.Localization;
using src.Web.Extensions;
using AutoMapper;
using src.Core.Domains;
using System.Data;
using Dapper;
//using AutoMapper.Configuration;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.DirectoryServices.AccountManagement;
using src.Web.Common.Models.UserViewModels;

namespace src.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IDateTime _dateTime;
        private readonly IDomainRepository _domainRepository;
        private readonly ILogger<AccountController> _logger;

        private readonly IRoleRepository _roleRepository;
        private readonly ISignInManager _signInManager;
        private readonly IUserRepository _userRepository;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IBaseCategoryRepository _baseCategoryRepository;

        private readonly IMapper _mapper;
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;
        private readonly EncryptionService _encryptionService;
        private IConfiguration _configuration;

        public AccountController(
            IAuthenticationService authenticationService,
            IDateTime dateTime,
            IDomainRepository domainRepository,
            ILogger<AccountController> logger,
            IRoleRepository roleRepository,
            ISignInManager signInManager,
            IUserRepository userRepository,
            IOptions<AppSettings> appSettings,
            IMessageService messageService,
            IBaseCategoryRepository baseCategoryRepository,
            IStringLocalizer<SharedResource> sharedLocalizer,
            EncryptionService encryptionService,
            IMapper mapper,
            IConfiguration configuration)
        {
            _authenticationService = authenticationService;
            _dateTime = dateTime;
            _domainRepository = domainRepository;
            _logger = logger;
            _roleRepository = roleRepository;
            _signInManager = signInManager;
            _userRepository = userRepository;
            _appSettings = appSettings;
            _baseCategoryRepository = baseCategoryRepository;
            _sharedLocalizer = sharedLocalizer;
            _encryptionService = encryptionService;
            _mapper = mapper;
            _configuration = configuration;
        }

        //
        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await _signInManager.SignOutAsync();
            ViewData["ReturnUrl"] = returnUrl;
            //var model = new LoginViewModel { AvailableDomains = await GetDomains() };
            var model = new LoginViewModel();
            return View("Login", model);
        }

        //POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    //bool result = true;
                    bool result = _authenticationService.ValidateUser(_appSettings.Value.Application.Domain, model.UserName, model.Password);
                    if (result)
                    {

                        var user = await _userRepository.GetUserByUserNameAsync(model.UserName);
                        if (user != null)
                        {
                            if (!user.IsActive)
                            {
                                _logger.LogError($"Authorization Fail: {model.UserName}");
                                ModelState.AddModelError("", _sharedLocalizer["AccessDenied"]);
                                return View(model);
                            }
                            var roleNames = (await _roleRepository.GetRolesForUserAsync(user.Id)).Select(r => r.Name).ToList();

                            await _signInManager.SignInAsync(user, roleNames);
                            user.LastLoginDate = _dateTime.Now;
                            await _userRepository.UpdateUserAsync(user);

                            _logger.LogInformation($"Login Successful: {user.UserName}.");

                            if (!string.IsNullOrEmpty(returnUrl) && !string.Equals(returnUrl, "/") && Url.IsLocalUrl(returnUrl))
                                return RedirectToLocal(returnUrl);

                            if (roleNames.Contains(Constants.RoleNames.Administrator))
                                return RedirectToAction(nameof(HomeController.Index), "Home");

                            return RedirectToAction(nameof(HomeController.Index), "Home");
                        }
                        _logger.LogError($"Authorization Fail: {model.UserName}");
                        ModelState.AddModelError("", Constants.Messages.AccessDenied);
                    }
                    else
                    {
                        _logger.LogError($"Login Fail: {model.UserName} - Incorrect username or password. ");
                        ModelState.AddModelError("", _sharedLocalizer["IncorrectUsernameOrPassword"]);
                    }
                }
            }

            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, _sharedLocalizer["InvalidLogin"]);
                return View(model);
            }
            return View(model);
        }

        //For staff login
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> StaffViewLogin(StaffLoginViewModel model)
        {
            await _signInManager.SignOutAsync();

            //var model = new StaffLoginViewModel();
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ParentLogin(ParentLoginViewModel model)
        {
            await _signInManager.SignOutAsync();
            return View("ParentLogin");
        }



        //POST: /Account/Logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation($"Logout Successful: {User.Identity.Name}");
            return RedirectToAction(nameof(AccountController.Login));
        }

        private async Task<IList<SelectListItem>> GetDomains()
        {
            return (await _domainRepository.GetAllDomainsAsync())
                .Select(d => new SelectListItem { Text = d.Name, Value = d.Name })
                .ToList();
        }
        private async Task<IList<SelectListItem>> GetListStudents(string phoneNumber)
        {
            return (await _baseCategoryRepository.getListStudentByPhoneNumber(phoneNumber)).
                Select(c => new SelectListItem() { Text = c.CategoryName, Value = c.Code }).ToList();
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return LocalRedirect(returnUrl);
        }
        [AllowAnonymous]
        private async Task<User> CreateUserIfNotExist(string userName)
        {
            using (var context = new PrincipalContext(ContextType.Domain, "vais.local"))
            {
                var user = UserPrincipal.FindByIdentity(context, userName);
                if (user != null)
                {
                    var userOnDb = _userRepository.GetUserByUserNameAsync(userName);
                    if (userOnDb.Result != null)
                    {
                        return null;
                    }
                    else
                    {
                        var userModel = new UserCreateUpdateViewModel();
                        userModel.FirstName = user.GivenName;
                        userModel.LastName = user.Surname;
                        userModel.UserName = userName.Trim();
                        userModel.UserCode = user.Description;
                        userModel.Position = AccountManagmentExtensions.ExtensionGet(user, "title");
                        userModel.Campus = AccountManagmentExtensions.ExtensionGet(user, "physicaldeliveryofficename");
                        userModel.IsActive = true;
                        userModel.CreatedBy = "binh.huy.doan";
                        var createModel = _mapper.Map<UserCreateUpdateViewModel, User>(userModel);
                        createModel.LastLoginDate = null;
                        createModel.ModifiedBy = "binh.huy.doan";
                        createModel.ModifiedOn = _dateTime.Now;
                        createModel.CreatedOn = _dateTime.Now;
                        createModel.UserRoles.Add(new UserRole { User = createModel, RoleId = 2 });
                        await _userRepository.AddUserAsync(createModel);
                        return createModel;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

    }
}