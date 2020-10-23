namespace Atoll.Transport.DataHub
{
    /// <summary>
    /// данные об агенте требуемые для получения данных о его динамической 
    /// </summary>
    public class AgentIdentity
    {
        public string DomainName { get; set; }
        public string ComputerName { get; set; }

        public static AgentIdentity FromMessageHeaders(MessageHeaders messageHeaders)
        {
            return new AgentIdentity
            {
                ComputerName = messageHeaders.ComputerName,
                DomainName = messageHeaders.DomainName,
            };
        }
    }
}
