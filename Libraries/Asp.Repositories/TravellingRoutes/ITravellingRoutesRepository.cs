using src.Core.Domains;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace src.Repositories.TravellingRoutes
{
    public interface ITravellingRoutesRepository
    {
        Task  insert(TravellingRoute model);
    }
}
