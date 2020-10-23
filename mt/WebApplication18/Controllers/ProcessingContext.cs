using WebApplication18.Configuration;
using WebApplication18.Transport;

namespace WebApplication18.Controllers
{
    public class ProcessingContext
    {
        public ConfigData CurrentConfigData { get; set; }
        public DbTokenData CurrentDbTokenData { get; set; }
        public MessageHeaders MessageHeaders { get; set; }
    }
}
