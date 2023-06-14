using System.Collections.Generic;
using System.Threading.Tasks;
using src.Core.Data;
using src.Core.Domains;

namespace src.Repositories.Users
{
    public interface IUserRepository
    {
        Task<IList<User>> GetUsersAsync();
        Task<User> GetUserByUserNameAsync(string userName);
        Task<User> GetUserByIdAsync(int userId);
        Task<int> AddUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task<IList<User>> GetUsersGroupSetting(List<string> listUser);
    }
}