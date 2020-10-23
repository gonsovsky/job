namespace Atoll.Transport.Client.Contract
{
    public class ConfigurationPart
    {

        public string ProviderKey { get; set; }

        public long StartPosition { get; set; }

        public long? EndPosition { get; set; }

        public bool IsFinal { get; set; }

        /// <summary>
        /// текущая часть конфигурации
        /// </summary>
        public byte[] Stream { get; set; }

        public string Token { get; set; }

    }
}
