using System;
using System.IO;
using System.Text;

namespace Coral.Atoll.Utils
{

    public class FormDataWriter: IDisposable
    {
        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);
        private readonly Encoding encoding;
        private bool needsCLRF = false;
        private readonly Stream requestStream;
        private readonly string boundary;
        private bool disposed;
        private bool hasContent;
        private bool hasFooter;
        public int size;

        public FormDataWriter(Stream requestStream, string boundary, Encoding encoding = null)
        {
            this.requestStream = requestStream;
            this.boundary = boundary;
            this.encoding = encoding ?? DefaultEncoding;
        }

        public void Dispose()
        {
            if (!this.hasFooter)
            {
                this.WriteFinalBlock();
            }
        }

        public int GetWrittenSize()
        {
            return this.size;
        }

        public void ResetSize()
        {
            this.size = 0;
        }

        public void WriteFinalBlock()
        {
            this.ThrowIfAlreadyHasFooter();
            if (this.hasContent)
            {
                // Add the end of the request.  Start with a newline  
                string footer = "\r\n--" + boundary + "--\r\n";
                var bytes = encoding.GetBytes(footer);
                requestStream.Write(bytes, 0, bytes.Length);
                this.size += bytes.Length;
                this.hasFooter = true;
            }
        }

        private void WriteCLRFIfNeeded()
        {
            if (needsCLRF)
            {
                var bytes = encoding.GetBytes("\r\n");
                requestStream.Write(bytes, 0, bytes.Length);
                this.size += bytes.Length;
            }
                

            needsCLRF = true;
        }

        private void ThrowIfAlreadyHasFooter()
        {
            if (this.hasFooter)
            {
                throw new InvalidOperationException("request already has footer");
            }
        }

        public void WriteValue(string key, string value)
        {
            this.WriteValue(key, (object)value);
        }

        public void WriteValue(string key, long value)
        {
            this.WriteValue(key, (object)value);
        }

        public void WriteValue(string key, bool value)
        {
            this.WriteValue(key, (object)value);
        }

        public void WriteValue(string key, object value)
        {
            this.ThrowIfAlreadyHasFooter();
            this.WriteCLRFIfNeeded();

            string postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
                                    boundary,
                                    key,
                                    value);
            var bytes = encoding.GetBytes(postData);
            requestStream.Write(bytes, 0, bytes.Length);
            this.hasContent = true;
            this.size += bytes.Length;
        }

        public void WriteFileHeader(string key, string fileName, string contentType = null)
        {
            this.ThrowIfAlreadyHasFooter();
            this.WriteCLRFIfNeeded();

            // Add just the first part of this param, since we will write the file data directly to the Stream  
            string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n",
                boundary,
                key,
                fileName ?? key,
                contentType ?? "application/octet-stream");

            var bytes = encoding.GetBytes(header);
            requestStream.Write(bytes, 0, bytes.Length);
            this.hasContent = true;
            this.size += bytes.Length;
        }

        public void WriteFile(string key, string fileName, Stream fileToUpload, string contentType)
        {
            this.WriteFileHeader(key, fileName, contentType);

            // Write the file data directly to the Stream, rather than serializing it to a string.  
            fileToUpload.CopyTo(requestStream);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            requestStream.Write(buffer, offset, count);
            this.hasContent = true;
            this.size += count;
        }
    }
}
