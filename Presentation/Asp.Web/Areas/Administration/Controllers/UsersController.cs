using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using src.Core;
using src.Core.Data;
using src.Core.Domains;
using src.Repositories.Messages;
using src.Repositories.Roles;
using src.Repositories.Users;
using src.Web.Common;
using src.Web.Common.Models.UserViewModels;
using src.Web.Common.Mvc.Alerts;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.DirectoryServices.AccountManagement;

namespace src.Web.Areas.Administration.Controllers
{
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

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        public IActionResult List()
        {
            return View();
        }

        //GetADGroups

        public async Task<ActionResult> AddOrEdit(int Id)
        {
            if (Id == 0)
            {
                UserCreateUpdateViewModel viewmodel = new UserCreateUpdateViewModel();
                viewmodel.Id = 0;
                viewmodel.CreatedBy = _userSession.UserName;
                viewmodel.AvailableRoles = await GetAvailableRoles();
                viewmodel.HeadOfDepartment = await getAllCampusDepartMent();
                viewmodel.IsActive = true;
                return View(viewmodel);
            }
            else
            {
                var entity = await _userRepository.GetUserByIdAsync(Id);
                var viewmodel = _mapper.Map<User, UserCreateUpdateViewModel>(entity);
                viewmodel.SelectedRoleIds = (await _roleRepository.GetUserRolesForUserAsync(entity.Id)).Select(r => r.RoleId).ToList();
                viewmodel.AvailableRoles = await GetAvailableRoles();
                viewmodel.SelectedCampusIds = entity.HeadOfDepartments.Select(t => t.DepartmentOrCampus).ToArray();
                viewmodel.HeadOfDepartment = await getAllCampusDepartMent();
                return View(viewmodel);
            }
        }

        private async Task<IList<SelectListItem>> getAllCampusDepartMent()
        {
            var entities = await _userRepository.GetUsersAsync();
           return entities
                               .GroupBy(l => l.Campus)
                               .Select(i =>
                                   new SelectListItem
                                   {
                                       Text = i.First().Campus,
                                       Value = i.First().Campus,
                                   }
                               ).ToList();
        }
        private async Task<IList<SelectListItem>> GetAvailableRoles()
        {
            return (await _roleRepository.GetAllRolesAsync())
                .Select(role => new SelectListItem { Text = role.Name, Value = role.Id.ToString() })
                .ToList();
        }
    }
}