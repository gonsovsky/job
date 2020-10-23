namespace Atoll.Transport.DataHub
{
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
