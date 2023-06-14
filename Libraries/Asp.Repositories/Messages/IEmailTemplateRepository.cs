using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using src.Core.Domains;

namespace src.Repositories.Messages
{
    public interface IEmailTemplateRepository
    {
        Task<IList<EmailTemplate>> GetAllEmailTemplates();

        Task<EmailTemplate> GetEmailTemplateById(Guid id);

        Task<EmailTemplate> GetEmailTemplateByName(string name);

        Task<Guid> InsertEmailTemplate(EmailTemplate template);

        Task UpdateEmailTemplate(EmailTemplate template);
    }
}