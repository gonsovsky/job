
namespace Atoll.Transport.Client.Contract
{
    /// <summary>
    /// данные об агенте для передачи транспортного сообщения
    /// </summary>
    public class TransportAgentInfo
    {
        public string Domain { get; set; }
        public string ComputerName { get; set; }
        public string DbToken { get; set; }
        public string ConfigToken { get; set; }
        public int ConfigVersion { get; set; }
        //
        public string OrganizationUnit { get; set; }
    }
}
