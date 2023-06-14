using System;
using System.Linq;
using System.Threading.Tasks;
using src.Core.Data;
using src.Core.Domains;
using src.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace src.Repositories.Users
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbContext _context;

        public UserRepository(IDbContext context)
        {
            _context = context;
        }

        public async Task<IList<User>> GetUsersAsync()
        {
            var query = _context.Users
                .Include(x => x.UserRoles)
                .AsQueryable();
            return await query.ToListAsync();
        }

        public async Task<User> GetUserByUserNameAsync(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentNullException(nameof(userName));

            var query = _context.Users
                .Where(x => x.UserName == userName);

            return await query.FirstOrDefaultAsync();
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            var query = _context.Users.Include(t=>t.HeadOfDepartments)
                .Where(x => x.Id == userId);

            return await query.FirstOrDefaultAsync();
        }

        public async Task<int> AddUserAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user.Id;
        }

        public async Task UpdateUserAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<IList<User>> GetUsersGroupSetting(List<string> listUser)
        {
            var result = _context.Users.Where(t => listUser.Contains(t.UserName)).AsQueryable();
            return await result.ToListAsync();
        }
    }
}