using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ClassLibrary1.Transport
{



    /// <summary>
    /// Конвертёр для сохранения данных по конфигурациям для агента
    /// </summary>
    /// <remarks>Сейчас внутри используется парсинг в виде полной загрузки в память, следует переделать на потоковое чтение</remarks>
    public class ConfigurationsByteArraysToFilesConverter : JsonConverter
    {
        //class SaveJsonByteArrayConverter : JsonConverter
        //{
        //    private readonly Func<Stream> streamFunc;

        //    public SaveJsonByteArrayConverter(Func<Stream> streamFunc)
        //    {
        //        this.streamFunc = streamFunc;
        //    }

        //    public override bool CanConvert(Type objectType)
        //    {
        //        return typeof(Stream).IsAssignableFrom(objectType);
        //    }

        //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        //    {
        //        if (reader.TokenType == JsonToken.Null)
        //            return null;

        //        if (this.streamFunc != null)
        //        {
        //            using (var stream = this.streamFunc())
        //            {
        //                int bufferSize = 4086;
        //                List<byte> buffer = new List<byte>(bufferSize);
        //                while (reader.Read() && reader.TokenType != JsonToken.EndArray)
        //                {
        //                    var byteValue = (byte)reader.Value;
        //                    buffer.Add(byteValue);

        //                    if (bufferSize <= buffer.Count)
        //                    {
        //                        stream.Write(buffer.ToArray(), 0, buffer.Count);
        //                        buffer.Clear();
        //                    }
        //                }
        //            }

        //            return null;
        //        }

        //        return null;
        //    }

        //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        //    {
        //        throw new NotSupportedException();
        //    }
        //}

        private readonly IConfigurationStoreService configurationStore;

        public ConfigurationsByteArraysToFilesConverter(IConfigurationStoreService configurationStore)
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
            configurationStore.Save(cnf);
            return cnf;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException("Converter does not support write mode.");
        }
    }
}
