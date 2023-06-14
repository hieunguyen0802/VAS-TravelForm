using src.Core.Domains;
using src.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using src.Core.Enums;

namespace src.Repositories.Configs
{
    public class EmailConfigRepository : IEmailConfigRepository
    {
        private readonly IDbContext _dbcontext;

        public EmailConfigRepository(IDbContext dbContext)
        {
            _dbcontext = dbContext;
        }
        public async Task addEmaiConfig(configs_email model)
        {
            if (model==null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            _dbcontext.email_configs.Add(model);
            await _dbcontext.SaveChangesAsync();
        }

        

        public async Task<configs_email> getAllConfig()
        {
            return await _dbcontext.email_configs.AsQueryable().FirstOrDefaultAsync();
        }

        public async Task<configs_email> getTemplatesByRequestStatus(int status)
        {
            if (status == (int)RequestStatus.Submitted)
            {
                return await _dbcontext.email_configs.Include(e => e.forSubmitted).FirstOrDefaultAsync();
            }
            if (status == (int)RequestStatus.lineManager_Approved)
            {
                return await _dbcontext.email_configs.Include(e => e.forApproved).FirstOrDefaultAsync();
            }
            if (status == (int)RequestStatus.lineManager_Rejected)
            {
                return await _dbcontext.email_configs.Include(e => e.forRejected).FirstOrDefaultAsync();
            }
            if (status == (int)RequestStatus.sent_request_to_ECSD)
            {
                return await _dbcontext.email_configs.Include(e => e.forRequestToECSD).FirstOrDefaultAsync();
            }
            if (status == (int)RequestStatus.ecsd_Approved)
            {
                return await _dbcontext.email_configs.Include(e => e.forECSDApproved).FirstOrDefaultAsync();
            }
            if (status == (int)RequestStatus.ecsd_Rejected)
            {
                return await _dbcontext.email_configs.Include(e => e.forECSDRejected).FirstOrDefaultAsync();
            }
            if (status == (int)RequestStatus.cancelled)
            {
                return await _dbcontext.email_configs.Include(e => e.forCancelled).FirstOrDefaultAsync();
            }
            if (status == 10 || status == 11)
            {
                return await _dbcontext.email_configs.Include(e => e.forRedzone).FirstOrDefaultAsync();
            }            


            return null;
        }

        public async Task removeAllconfigs()
        {
             _dbcontext.email_configs.RemoveRange(_dbcontext.email_configs);
            await _dbcontext.SaveChangesAsync();
        }
    }
}
