using System;
using System.IO;

namespace Coral.Atoll.Utils
{
    public sealed class ReadOnlyStream : Stream
    {
        public ReadOnlyStream(Stream stream)
        {
            this.stream = stream;
            this.peeked = new byte[16];
            this.peekedLength = 0;
        }

        public override void Flush()
        {
            //throw new NotSupportedException();
        }

        public override void Close()
        {
            this.stream.Close();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.peekedLength == 0)
            {
                return this.stream.Read(buffer, offset, count);
            }

            for (var i = 0; i < count; i++)
            {
                buffer[offset + i] = this.Pop();
            }
            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return this.stream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return this.stream.Length; }
        }

        public override long Position
        {
            get { return this.stream.Position; }
            set { this.stream.Position = value; }
        }

        public int Peek(int depth)
        {
            if (this.peeked.Length <= depth)
            {
                var temp = new byte[depth + 16];
                for (var i = 0; i < this.peeked.Length; i++)
                {
                    temp[i] = this.peeked[i];
                }
                this.peeked = temp;
            }

            if (depth >= this.peekedLength)
            {
                var offset = this.peekedLength;
                var length = (depth - this.peekedLength) + 1;
                var lengthRead = stream.Read(peeked, offset, length);

                if (lengthRead < 1)
                {
                    return -1;
                }

                this.peekedLength = depth + 1;
            }

            return this.peeked[depth];
        }

        private byte Pop()
        {
            var result = this.peeked[0];
            this.peekedLength--;
            for (var i = 0; i < peekedLength; i++)
            {
                this.peeked[i] = this.peeked[i + 1];
            }

            return result;
        }

        private int peekedLength;
        private byte[] peeked;
        private readonly Stream stream;
    }
}
