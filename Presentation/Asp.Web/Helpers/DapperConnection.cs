using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace src.Web.Helpers
{
    public static class DapperConnection
    {
        internal static IDbConnection postgreConnection(string strConnection)
        {
            return new NpgsqlConnection(strConnection);
        }
        internal static IDbConnection sqlConnection(string strConnection)
        {
            return new SqlConnection(strConnection);
        }

        internal static IDbConnection postgreConnection(object p)
        {
            throw new NotImplementedException();
        }
    }
}
