using Atoll.Transport.Client.Contract;

namespace Atoll.Transport.Client.Bundle.Dto
{
    //[JsonConverter(typeof(ConfigurationsByteArraysToFilesConverter))]
    public class ConfigurationResponse
    {
        public string ProviderKey { get; set; }
        public long StartPosition { get; set; }
        public long? EndPosition { get; set; }
        public bool IsFinal { get; set; }

        /// <summary>
        /// текущая часть конфигурации
        /// </summary>
        //public Stream Stream { get; set; }
        public byte[] Stream { get; set; }

        public string Token { get; set; }

        public ConfigurationPart ToConfigurationPart()
        {
            return new ConfigurationPart
            {
                ProviderKey = this.ProviderKey,
                StartPosition = this.StartPosition,
                EndPosition = this.EndPosition,
                IsFinal = this.IsFinal,
                Stream = this.Stream,
                Token = this.Token,
            };
        }
    }
}
