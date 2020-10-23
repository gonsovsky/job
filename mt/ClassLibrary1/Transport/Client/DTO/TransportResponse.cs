using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ClassLibrary1.Transport
{
    public class StaticConfigDataResponse
    {
        public string Config { get; set; }
        public string ConfigToken { get; set; }
        public int ConfigVersion { get; set; }
    }

    public class DbTokenDataResponse
    {
        public string DbToken { get; set; }
    }

    public enum PacketProcessingResult
    {
        Unknown = 0,
        Saved = 1,
        Error = 2,
        Resend = 3,
        PacketsNeedUpdate = 4
    }

    public class TransferedPacketResponse
    {
        public string PacketId { get; set; }
        public string ProviderKey { get; set; }
        public PacketProcessingResult Result { get; set; }
    }

    //[JsonConverter(typeof(ConfigurationsByteArraysToFilesConverter))]
    public class ConfigurationResponse
    {
        //public long PacketId { get; set; }
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
    }

    public class TransportResponse
    {
        public string ErrorMessage { get; private set; }
        public StaticConfigDataResponse StaticConfigData { get; set; }
        public DbTokenDataResponse DbTokenData { get; set; }

        public IList<TransferedPacketResponse> TransferedPackets { get; set; }
        public IList<ConfigurationResponse> Configurations { get; set; }
    }
}
