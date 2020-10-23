using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace WebApplication18.Transport
{
    public enum MessageHints
    {
        None = 0,
        OrderedFormData = 1,
    }

    public class MessageHeaders
    {
        //
        //public long MessageId { get; set; }
        public string Domain { get; set; }
        public string ComputerName { get; set; }
        public string DbToken { get; set; }
        public string ConfigToken { get; set; }
        public int ConfigVersion { get; set; }

        //
        public string OrganizationUnit { get; set; }

        //
        public int Attempt { get; set; }
        //public int Timeout { get; set; }

        /// <summary>
        /// формат который используется для передачи пакетов\файлов в запросе
        /// </summary>
        public string Format { get; set; }

        public MessageHints[] Hints { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            var paramsDict = new Dictionary<string, string>();

            //if (this.MessageId != 0)
            //{
            //    paramsDict.Add(nameof(this.MessageId), this.MessageId.ToString());
            //}

            if (!string.IsNullOrEmpty(this.Domain))
            {
                paramsDict.Add(nameof(this.Domain), this.Domain);
            }

            if (!string.IsNullOrEmpty(this.ComputerName))
            {
                paramsDict.Add(nameof(this.ComputerName), this.ComputerName);
            }

            if (!string.IsNullOrEmpty(this.DbToken))
            {
                paramsDict.Add(nameof(this.DbToken), this.DbToken);
            }

            if (!string.IsNullOrEmpty(this.ConfigToken))
            {
                paramsDict.Add(nameof(this.ConfigToken), this.ConfigToken);
            }

            var configVersion = this.ConfigVersion.ToString();
            if (!string.IsNullOrEmpty(configVersion))
            {
                paramsDict.Add(nameof(this.ConfigVersion), configVersion);
            }

            if (!string.IsNullOrEmpty(this.OrganizationUnit))
            {
                paramsDict.Add(nameof(this.OrganizationUnit), this.OrganizationUnit);
            }

            var attempt = this.Attempt.ToString();
            if (!string.IsNullOrEmpty(attempt))
            {
                paramsDict.Add(nameof(this.Attempt), attempt);
            }

            if (this.Hints != null && this.Hints.Any())
            {
                paramsDict.Add(nameof(this.Hints), GetHintsString(this.Hints));
            }

            return paramsDict;
        }
        
        public static MessageHeaders FromDictionary(IDictionary<string, string> dict)
        {
            var headers = new MessageHeaders();
            //headers.MessageId = dict.GetOrDefault(nameof(headers.MessageId), long.Parse);
            headers.Domain = dict.GetOrDefault(nameof(headers.Domain));
            headers.ComputerName = dict.GetOrDefault(nameof(headers.ComputerName));
            headers.DbToken = dict.GetOrDefault(nameof(headers.DbToken));
            headers.ConfigToken = dict.GetOrDefault(nameof(headers.ConfigToken));
            headers.OrganizationUnit = dict.GetOrDefault(nameof(headers.OrganizationUnit));
            headers.ConfigVersion = dict.GetOrDefault(nameof(headers.ConfigVersion), int.Parse);
            headers.Attempt = dict.GetOrDefault(nameof(headers.Attempt), int.Parse);
            headers.Hints = dict.GetOrDefault(nameof(headers.Hints), GetHints);

            return headers;
        }

        private static char SplitDelimiter = ',';
        private static MessageHints[] GetHints(string hintsStr)
        {
            if (string.IsNullOrEmpty(hintsStr))
            {
                return Array.Empty<MessageHints>();
            }

            return hintsStr.Split(SplitDelimiter, StringSplitOptions.RemoveEmptyEntries)
                .Select(x =>
                {
                    x = x.Trim();
                    return Enum.TryParse<MessageHints>(x, out var result)
                            ? result
                            : MessageHints.None;
                })
            .Where(x => x != MessageHints.None)
            .ToArray();
        }

        private static string GetHintsString(MessageHints[] hints)
        {
            return string.Join(SplitDelimiter, hints.Select(x => x.ToString()));
        }

        public string GetAgentId()
        {
            return string.Concat(this.Domain, "\\", this.ComputerName);
        }

        public AgentIdentifierData GetAgentIdData()
        {
            return new AgentIdentifierData
            {
                Domain = this.Domain,
                ComputerName = this.ComputerName,
            };
        }
    }

    //public class SectionDataConverter : JsonConverter
    //{
    //    public override bool CanConvert(Type objectType)
    //    {
    //        return true;
    //    }

    //    public static JsonReader CopyReaderForObject(JsonReader reader, JToken jToken)
    //    {
    //        JsonReader jTokenReader = jToken.CreateReader();
    //        jTokenReader.Culture = reader.Culture;
    //        jTokenReader.DateFormatString = reader.DateFormatString;
    //        jTokenReader.DateParseHandling = reader.DateParseHandling;
    //        jTokenReader.DateTimeZoneHandling = reader.DateTimeZoneHandling;
    //        jTokenReader.FloatParseHandling = reader.FloatParseHandling;
    //        jTokenReader.MaxDepth = reader.MaxDepth;
    //        jTokenReader.SupportMultipleContent = reader.SupportMultipleContent;
    //        return jTokenReader;
    //    }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        if (reader.TokenType == JsonToken.Null)
    //            return null;

    //        return JToken.Load(reader);
    //    }

    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        serializer.Serialize(writer, value);
    //    }
    //}

    //public class ByteArrayConverter : JsonConverter
    //{
    //    public override void WriteJson(
    //        JsonWriter writer,
    //        object value,
    //        JsonSerializer serializer)
    //    {
    //        if (value == null)
    //        {
    //            writer.WriteNull();
    //            return;
    //        }

    //        byte[] data = (byte[])value;

    //        // Compose an array.
    //        writer.WriteStartArray();

    //        for (var i = 0; i < data.Length; i++)
    //        {
    //            writer.WriteValue(data[i]);
    //        }

    //        writer.WriteEndArray();
    //    }

    //    public override object ReadJson(
    //        JsonReader reader,
    //        Type objectType,
    //        object existingValue,
    //        JsonSerializer serializer)
    //    {
    //        if (reader.TokenType == JsonToken.StartArray)
    //        {
    //            var byteList = new List<byte>();

    //            while (reader.Read())
    //            {
    //                switch (reader.TokenType)
    //                {
    //                    case JsonToken.Integer:
    //                        byteList.Add(Convert.ToByte(reader.Value));
    //                        break;
    //                    case JsonToken.EndArray:
    //                        return byteList.ToArray();
    //                    case JsonToken.Comment:
    //                        // skip
    //                        break;
    //                    default:
    //                        throw new Exception(
    //                        string.Format(
    //                            "Unexpected token when reading bytes: {0}",
    //                            reader.TokenType));
    //                }
    //            }

    //            throw new Exception("Unexpected end when reading bytes.");
    //        }
    //        else
    //        {
    //            throw new Exception(
    //                string.Format(
    //                    "Unexpected token parsing binary. "
    //                    + "Expected StartArray, got {0}.",
    //                    reader.TokenType));
    //        }
    //    }

    //    public override bool CanConvert(Type objectType)
    //    {
    //        return objectType == typeof(byte[]);
    //    }
    //}

    //public class MessageSection
    //{
    //    public string Key { get; private set; }
    //    [JsonConverter(typeof(ByteArrayConverter))]
    //    public byte[] Data { get; private set; }
    //    //public bool IsLastPart { get; private set; }
    //}

    //public class PacketSection
    //{

    //    public string Key { get; private set; }

    //    public long PacketId { get; set; }

    //    [JsonConverter(typeof(ByteArrayConverter))]
    //    public byte[] Data { get; private set; }

    //}

    //public class MessageBody
    //{
    //    public MessageBody()
    //    {
    //        //Sections = new List<MessageSection>();
    //        Packets = new List<PacketSection>();
    //    }

    //    //public IEnumerable<MessageSection> Sections { get; set; }
    //    public IEnumerable<PacketSection> Packets { get; set; }
    //}

    //public class MessageSizeLimits
    //{
    //    public MessageSizeLimits(long? min, long? max)
    //    {
    //        this.Min = min;
    //        this.Max = max;
    //    }

    //    public long? Min { get; private set; }
    //    public long? Max { get; private set; }
    //}

    //public class Message
    //{

    //    public MessageHeaders Headers { get; protected set; }

    //    public Stream Body { get; protected set; }

    //    public Message(MessageHeaders headers, Stream body)
    //    {
    //        this.Headers = headers;
    //        this.Body = body;
    //    }
    //}
}
