#if NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Coral.Atoll.Utils
{

    public class WriteToStreamContent : HttpContent
    {
        private readonly Func<Stream, HttpContent, Task> writeActionAsync;
        private readonly Action<Stream, HttpContent> writeAction;

        public WriteToStreamContent(Func<Stream, HttpContent, Task> writeActionAsync)
        {
            this.writeActionAsync = writeActionAsync;
        }

        public WriteToStreamContent(Action<Stream, HttpContent> writeAction)
        {
            this.writeAction = writeAction;
        }

        protected override async Task SerializeToStreamAsync(Stream stream,
            TransportContext context)
        {
            if (this.writeActionAsync != null)
            {
                await this.writeActionAsync(stream, this);
            }
            else
            {
                this.writeAction(stream, this);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            return base.CreateContentReadStreamAsync();
        }
    }
}

#endif
