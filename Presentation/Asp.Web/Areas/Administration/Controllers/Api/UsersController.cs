using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Core;
using src.Core.Domains;
using src.Repositories.Messages;
using src.Repositories.Roles;
using src.Repositories.Users;
using src.Web.Common;
using src.Web.Common.Models.UserViewModels;
using System.DirectoryServices.AccountManagement;
using src.Web.Common.Mvc.Alerts;
using src.Web.Extensions;
using System.Collections;
using System.Collections.Generic;

namespace src.Web.Areas.Administration.Controllers.Api
{
    [Produces("application/json")]
    [Route("api/Users")]
    [Area(Constants.Areas.Administration)]
    [Authorize(Policy = Constants.RoleNames.Administrator)]
    public class UsersController : Controller
    {
        private readonly IDateTime _dateTime;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserSession _userSession;

        public UsersController(
            IDateTime dateTime,
            IMapper mapper,
            IMessageService messageService,
            IRoleRepository roleRepository,
            IUserRepository userRepository,
            IUserSession userSession)
        {
            _dateTime = dateTime;
            _mapper = mapper;
            _roleRepository = roleRepository;
            _messageService = messageService;
            _userRepository = userRepository;
            _userSession = userSession;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var entities = await _userRepository.GetUsersAsync();
                var roles = await _roleRepository.GetAllRolesAsync();
                var models = entities.Select(e => _mapper.Map<User, UserViewModel>(e)).ToList();
                foreach (var m in models)
                {
                    if (m.AuthorizedRoleIds.Any())
                    {
                        m.AuthorizedRoleNames = string.Join(",",
                            roles.Where(r => m.AuthorizedRoleIds.Contains(r.Id)).Select(r => r.Name).OrderBy(r => r)
                                .ToArray());
                    }
                }
                return Json(new { data = models });
            }
            catch (Exception ex)
            {
                return Json(new { sucess = false, message = ex.Message });
            }
        }
        [HttpGet("getFirstLastNameFromAD")]
        public async Task<IActionResult> getFirstLastNameFromAD(string userName)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, "vais.local"))
                {
                    var user = UserPrincipal.FindByIdentity(context, userName);
                    if (user != null)
                    {
                        var userOnDb = await _userRepository.GetUserByUserNameAsync(userName);
                        if (userOnDb != null)
                        {
                            return Json(data: $"The UserName {userName} has created !!!");
                        }
                        else
                            return Json(new
                            {
                                success = true,
                                FristName = user.GivenName,
                                LastName = user.Surname,
                                UserCode = user.Description,
                                JobTitle = AccountManagmentExtensions.ExtensionGet(user, "title"),
                                Campus = AccountManagmentExtensions.ExtensionGet(user, "physicaldeliveryofficename")

                            });
                    }
                    else
                    {
                        return Json(new { success = false });
                    }
                }
            }
            catch (Exception ex)
            {

                return Json(new { success = 0, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostUsers([FromBody] UserCreateUpdateViewModel model)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                if (model.Id == 0)
                {
                    var dateTimeNow = _dateTime.Now;
                    var username = _userSession.UserName;
                    var user = _mapper.Map<UserCreateUpdateViewModel, User>(model);
                    user.LastLoginDate = dateTimeNow;
                    user.CreatedBy = username;
                    user.CreatedOn = dateTimeNow;
                    user.ModifiedBy = username;
                    user.ModifiedOn = dateTimeNow;

                    var allRoles = await _roleRepository.GetAllRolesAsync();
                    foreach (var role in allRoles)
                    {
                        if (model.SelectedRoleIds.Any(r => r == role.Id))
                            user.UserRoles.Add(new UserRole { User = user, Role = role });
                    }

                    if (model.SelectedCampusIds != null && model.SelectedCampusIds.Count() > 0)
                    {
                        foreach (var item in model.SelectedCampusIds)
                        {
                            var head_of_campus = new HeadOfDepartment()
                            {
                                HeadOfDepartmentId = Guid.NewGuid(),
                                User = user,
                                DepartmentOrCampus = item
                            };
                            user.HeadOfDepartments.Add(head_of_campus);
                        }
                    }
                    await _userRepository.AddUserAsync(user);

                    return Json(new { success = true, message = $"Add username {model.UserName} successful !" });
                }
                else
                {
                    var user = await _userRepository.GetUserByIdAsync(model.Id);
                    if (user == null)
                    {
                        return RedirectToAction("List").WithError("Please select a user.");
                    }
                    var dateTimeNow = _dateTime.Now;
                    var username = _userSession.UserName;
                    var userMapper = _mapper.Map(model, user);
                    userMapper.FirstName = model.FirstName;
                    userMapper.LastName = model.LastName;
                    userMapper.IsActive = model.IsActive;
                    //user.LastLoginDate = dateTimeNow;
                    //user.ModifiedOn = dateTimeNow;
                    //user.ModifiedBy = username;

                    var allRoles = await _roleRepository.GetAllRolesAsync();
                    var allUserRoles = await _roleRepository.GetUserRolesForUserAsync(model.Id);
                    foreach (var role in allRoles)
                    {
                        if (model.SelectedRoleIds.Any(r => r == role.Id))
                        {
                            if (allUserRoles.All(r => r.RoleId != role.Id))
                                userMapper.UserRoles.Add(new UserRole { User = user, Role = role });
                        }
                        else if (allUserRoles.Any(r => r.RoleId == role.Id))
                        {
                            var removingUserRole = allUserRoles.FirstOrDefault(r => r.RoleId == role.Id);
                            userMapper.UserRoles.Remove(removingUserRole);
                        }
                    }

                    var dept = userMapper.HeadOfDepartments.Where(c => c.UserId == user.Id).ToList();
                    foreach (var item in dept)
                    {
                        userMapper.HeadOfDepartments.Remove(item);
                    }

                    if (model.SelectedCampusIds !=null && model.SelectedCampusIds.Count() > 0)
                    {
                        foreach (var item in model.SelectedCampusIds)
                        {
                            var head_of_campus = new HeadOfDepartment()
                            {
                                HeadOfDepartmentId = Guid.NewGuid(),
                                User = userMapper,
                                DepartmentOrCampus = item
                            };
                            userMapper.HeadOfDepartments.Add(head_of_campus);
                        }
                    }
                    await _userRepository.UpdateUserAsync(userMapper);
                    return Json(new { success = true, message = $"Update data infor {model.UserName} successful !" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}