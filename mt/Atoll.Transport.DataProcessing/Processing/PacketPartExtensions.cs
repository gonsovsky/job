using Atoll.Transport.ServerBundle;

namespace Atoll.Transport.DataProcessing
{
    public static class PacketPartExtensions
    {

        public static ComputerIdentity ToComputerIdentity(this AgentIdentity agentInfo)
        {
            return new ComputerIdentity(agentInfo.ComputerName, agentInfo.DomainName);
        }

    }
}
