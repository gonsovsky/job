using System;
using System.Collections.Generic;
using System.IO;

namespace Atoll.Transport.DataProcessing
{
    //public sealed class ReadByEnumerableStream : Stream
    //{
    //    private readonly IEnumerable<byte[]> bytesEnumerable;
    //    private IEnumerator<byte[]> enumerator;
    //    private byte[] current;
    //    private int currentPosition = -1;
    //    private int position = -1;
    //    private bool startedEnumerating = false;
    //    private bool endedEnumerating = false;

    //    public ReadByEnumerableStream(IEnumerable<byte[]> bytesEnumerable)
    //    {
    //        this.bytesEnumerable = bytesEnumerable;
    //        this.enumerator = this.bytesEnumerable.GetEnumerator();
    //    }

    //    public override bool CanRead => true;

    //    public override bool CanSeek => false;

    //    public override bool CanWrite => false;

    //    public override long Length => throw new NotSupportedException();

    //    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    //    public override void Flush()
    //    {
    //    }

    //    public override void Close()
    //    {
    //        enumerator?.Dispose();
    //        this.endedEnumerating = true;
    //    }

    //    private bool ReadNext()
    //    {
    //        if (this.endedEnumerating)
    //        {
    //            return false;
    //        }

    //        this.startedEnumerating = true;

    //        if (this.enumerator.MoveNext())
    //        {
    //            this.current = this.enumerator.Current;
    //            this.currentPosition = 0;
    //            return true;
    //        }
    //        else
    //        {
    //            this.endedEnumerating = true;
    //            this.current = null;
    //            this.currentPosition = -1;
    //            return false;
    //        }
    //    }

    //    public override int Read(byte[] buffer, int offset, int count)
    //    {
    //        if (!this.startedEnumerating)
    //        {
    //            if (!this.ReadNext())
    //            {
    //                return 0;
    //            }
    //        }

    //        int readed = 0;
    //        bool enumerating = !this.endedEnumerating;
    //        int currentCount = count;
    //        while (enumerating)
    //        {
    //            var n = this.current.Length - this.currentPosition;
    //            if (n > currentCount)
    //                n = currentCount;

    //            if (n <= 0)
    //            {

    //            }
    //            else
    //            {
    //                Buffer.BlockCopy(this.current, this.currentPosition, buffer, offset, n);
    //                this.currentPosition += n;
    //                this.position += n;
    //                currentCount -= n;
    //                readed += n;
    //            }

    //            enumerating = this.ReadNext();
    //        }

    //        return readed;
    //    }

    //    public override long Seek(long offset, SeekOrigin origin)
    //    {
    //        throw new NotSupportedException();
    //    }

    //    public override void SetLength(long value)
    //    {
    //        throw new NotSupportedException();
    //    }

    //    public override void Write(byte[] buffer, int offset, int count)
    //    {
    //        throw new NotSupportedException();
    //    }
    //}
}
