using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebApplication18.Configuration;

namespace WebApplication18.Transport
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

    public enum TransferedProcessingResult
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
        public TransferedProcessingResult Result { get; set; }
    }

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
        [JsonConverter(typeof(StreamToJsonByteArrayConverter))]
        public Stream Stream { get; set; }

        public string Token { get; set; }
    }

    public class TransportResponse
    {
        public string ErrorMessage { get; private set; }
        public StaticConfigDataResponse StaticConfigData { get; set; }
        public DbTokenDataResponse DbTokenData { get; set; }

        public IList<TransferedPacketResponse> TransferedPackets { get; set; }
        public IList<ConfigurationResponse> Configurations { get; set; }

        public TransportResponse SetDbTokenData(DbTokenData configData)
        {
            this.DbTokenData = new DbTokenDataResponse
            {
                DbToken = configData.DbToken,
            };

            return this;
        }

        public TransportResponse SetStaticConfigChanged(ConfigData configData)
        {
            this.StaticConfigData = new StaticConfigDataResponse
            {
                Config = configData.Config,
                ConfigToken = configData.ConfigToken,
                ConfigVersion = configData.ConfigVersion,
            };

            return this;
        }

        public TransportResponse SetConfigurations(IList<ConfigurationResponse> configurations)
        {
            this.Configurations = configurations;

            return this;
        }

        public TransportResponse SetPacketResponses(IList<TransferedPacketResponse> responses)
        {
            this.TransferedPackets = responses;
            return this;
        }

        public static TransportResponse Fail(string errorMessage)
        {
            return new TransportResponse
            {
                ErrorMessage = errorMessage
            };
        }
    }

    public class StreamToJsonByteArrayConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Stream).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var stream = (Stream)value;

            if (stream == null)
            {
                writer.WriteNull();
                return;
            }

            // Compose an array.
            writer.WriteStartArray();

            byte[] data = new byte[4086];
            int read;
            while ((read = stream.Read(data, 0, data.Length)) > 0)
            {
                for (var i = 0; i < read; i++)
                {
                    writer.WriteValue(data[i]);
                }
            }

            writer.WriteEndArray();
        }
    }
}
