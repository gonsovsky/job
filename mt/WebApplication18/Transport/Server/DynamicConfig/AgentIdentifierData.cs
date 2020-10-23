namespace WebApplication18.Transport
{
    /// <summary>
    /// данные об агенте требуемые для получения данных о его динамической 
    /// </summary>
    public class AgentIdentifierData
    {
        public string Domain { get; set; }
        public string ComputerName { get; set; }

        public static AgentIdentifierData FromMessageHeaders(MessageHeaders messageHeaders)
        {
            return new AgentIdentifierData
            {
                ComputerName = messageHeaders.ComputerName,
                Domain = messageHeaders.Domain,
            };
        }
    }
}
