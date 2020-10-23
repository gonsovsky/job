using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using Atoll.Transport.Client.Contract;
using Atoll.Transport.Client.Bundle.Dto;

namespace Atoll.Transport.Client.Bundle
{
    /// <summary>
    /// Конвертёр для сохранения данных по конфигурациям для агента
    /// </summary>
    /// <remarks>Сейчас внутри используется парсинг в виде полной загрузки в память, следует переделать на потоковое чтение</remarks>
    public class TransportResponseConfigurationsConverter : JsonConverter
    {
        private readonly IConfigurationStoreService configurationStore;
        private readonly List<string> currentConfigurations = new List<string>();

        public TransportResponseConfigurationsConverter(IConfigurationStoreService configurationStore)
        {
            this.configurationStore = configurationStore;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(ConfigurationResponse).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            // TODO optimize memory usage
            JObject item = JObject.Load(reader);
            var cnf = item.ToObject<ConfigurationResponse>();
            configurationStore.Save(cnf.ToConfigurationPart());
            return cnf;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException("Converter does not support write mode.");
        }

        public void RemoveNonActualConfigurations()
        {
        }
    }
}
