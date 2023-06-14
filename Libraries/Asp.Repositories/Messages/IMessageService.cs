using System.Threading.Tasks;
using src.Core.Domains;

namespace src.Repositories.Messages
{
    public interface IMessageService
    {
        Task SendAddNewUserNotification(User user);
    }
}