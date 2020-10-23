using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atoll.Transport.DataHub
{

    public interface IAgentStaticConfigService
    {
        bool ReloadConfig();
        ConfigData GetConfigData();
    }
}
