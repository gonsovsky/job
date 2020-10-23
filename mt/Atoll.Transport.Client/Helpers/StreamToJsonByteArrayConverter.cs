using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
#if NETSTANDARD2_0
using System.Net.Http;
#endif

namespace ClassLibrary1.Transport
{
    public class StreamToJsonByteArrayConverter : JsonConverter
    {
        private readonly Func<Stream> streamFunc;

        public StreamToJsonByteArrayConverter()
        {

        }

        public StreamToJsonByteArrayConverter(Func<Stream> streamFunc)
        {
            this.streamFunc = streamFunc;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(Stream).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var stream = this.streamFunc != null ? this.streamFunc() : new MemoryStream();
            int bufferSize = 4086;
            List<byte> buffer = new List<byte>(bufferSize);
            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                var byteValue = (byte)reader.Value;
                buffer.Add(byteValue);

                if (bufferSize <= buffer.Count)
                {
                    stream.Write(buffer.ToArray(), 0, buffer.Count);
                    buffer.Clear();
                }
            }

            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            return stream;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var stream = (Stream)value;

            if (stream == null)
            {
                writer.WriteNull();
                return;
            }

            //Compose an array.
            writer.WriteStartArray();

            byte[] data = new byte[4086];
            int read;
            while ((read = stream.Read(data, 0, data.Length)) > 0)
            {
                //output.Write(buffer, 0, read);
                for (var i = 0; i < read; i++)
                {
                    writer.WriteValue(data[i]);
                }
            }

            writer.WriteEndArray();
        }
    }
}
