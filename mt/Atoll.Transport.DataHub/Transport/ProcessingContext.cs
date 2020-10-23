
namespace Atoll.Transport.DataHub
{
    public class ProcessingContext
    {
        public ConfigData CurrentConfigData { get; set; }
        public DbTokenData CurrentDbTokenData { get; set; }
        public MessageHeaders MessageHeaders { get; set; }
    }
}
