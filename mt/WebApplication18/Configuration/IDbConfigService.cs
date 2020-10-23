using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication18.Configuration
{

    public interface IDbTokenService
    {
        bool ReloadToken();
        DbTokenData GetDbTokenData();
    }

    public class DbTokenService : IDbTokenService
    {
        private string DbToken = "dbTOken";

        public DbTokenData GetDbTokenData()
        {
            return new DbTokenData
            {
                DbToken = this.DbToken,
            };
        }

        public bool ReloadToken()
        {
            return false;
        }
    }
}
