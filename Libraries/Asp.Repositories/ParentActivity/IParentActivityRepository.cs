using src.Core.Domains;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace src.Repositories.ParentActivity
{
    public interface IParentActivityRepository
    {

        Task<IList<parents_activities>> getAllParentActivities();

        Task saveParentActivity(parents_activities model);

        Task<parents_activities> checkParentActivity(int id);

        Task updateParentActivity(parents_activities model);
    }
}
