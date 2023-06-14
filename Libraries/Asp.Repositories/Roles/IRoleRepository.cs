﻿using System.Collections.Generic;
using System.Threading.Tasks;
using src.Core.Domains;

namespace src.Repositories.Roles
{
    public interface IRoleRepository
    {
        Task<IList<Role>> GetAllRolesAsync();

        Task<IList<Role>> GetRolesForUserAsync(int userId);

        Task<IList<UserRole>> GetUserRolesForUserAsync(int userId);
    }
}