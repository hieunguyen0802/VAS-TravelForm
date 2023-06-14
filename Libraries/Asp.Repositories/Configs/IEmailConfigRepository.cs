using src.Core.Domains;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace src.Repositories.Configs
{
    public interface IEmailConfigRepository
    {
        Task addEmaiConfig(configs_email model);
        Task<configs_email> getTemplatesByRequestStatus(int status);
        Task<configs_email> getAllConfig();
        Task removeAllconfigs();
    }
}
