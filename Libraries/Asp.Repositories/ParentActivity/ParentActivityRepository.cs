using Microsoft.EntityFrameworkCore;
using src.Core.Domains;
using src.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src.Repositories.ParentActivity
{
    public class ParentActivityRepository : IParentActivityRepository
    {
        private readonly IDbContext _context;

        public ParentActivityRepository(IDbContext context)
        {
            _context = context;
        }

        public async Task<parents_activities> checkParentActivity(int id)
        {
            if (id != 0)
            {
                var result = _context.parents_activities.Where(p => p.id == id).FirstOrDefaultAsync();
                return await result;
            }
            return null;
        }

        public async Task<IList<parents_activities>> getAllParentActivities()
        {
            return await _context.parents_activities.ToListAsync();
        }

        public async Task saveParentActivity(parents_activities model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            _context.parents_activities.Add(model);
            await _context.SaveChangesAsync();
        }

        public async Task updateParentActivity(parents_activities model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            _context.parents_activities.Update(model);
            await _context.SaveChangesAsync();
        }
    }
}
