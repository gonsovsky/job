#if NETSTANDARD2_0
#endif

namespace ClassLibrary1.Transport
{
    //public interface ITransportPacketWriter: IDisposable
    //{
    //    void Write(byte[] bytes);
    //}


    //public class TransportPacketWriter: ITransportPacketWriter
    //{
    //    private readonly ITransportSession session;
    //    private PacketSizeLimits limits;
    //    private ITransportPacket current;

    //    public void Dispose()
    //    {
    //    }

    //    private void AllocatePacket()
    //    {
    //        // если есть предыдущий, то его мы добавляем в сессию
    //        if (this.current != null)
    //        {
    //            this.session.Add(current);
    //        }

    //        this.current = this.session.CreatePacket();
    //    }

    //    private void InitPacket()
    //    {
    //        if (this.current == null)
    //        {
    //            this.current = this.session.CreatePacket();
    //        }
    //    }

    //    public void Write(byte[] bytes)
    //    {
    //        // инициализация
    //        this.InitPacket();

    //        // разбиваем на части
    //        if (bytes.Length > this.limits.Max)
    //        {
    //            var size = this.limits.Max;
    //            int partCount = (bytes.Length + size - 1) / size;
    //            byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
    //            try
    //            {
    //                for (int i = 0; i < partCount; i++)
    //                {
    //                    var offset = i * size;
    //                    if (i == partCount - 1)
    //                    {
    //                        Array.Copy(bytes, offset, buffer, 0, bytes.Length - offset);
    //                    }
    //                    else
    //                    {
    //                        Array.Copy(bytes, offset, buffer, 0, size);
    //                    }

    //                    this.Write(buffer);
    //                }

    //                return;
    //            }
    //            finally
    //            {
    //                ArrayPool<byte>.Shared.Return(buffer);
    //            }
    //        }

    //        // нужно создавать новый пакет
    //        if (this.current.Length + bytes.Length > this.limits.Max)
    //        {
    //            this.AllocatePacket();
    //        }

    //        if (this.current.Length > this.limits.Min)
    //        {
    //            this.WriteToCurrentOrThrow(bytes);
    //        }
    //        else
    //        {
    //            this.WriteToCurrentOrThrow(bytes);
    //        }
    //    }

    //    private void WriteToCurrentOrThrow(byte[] bytes)
    //    {
    //        if (!this.current.TryWrite(bytes))
    //        {
    //            // непредвиденная ошибка...
    //            throw new InvalidOperationException("ошибка при записи");
    //        }
    //    }
    //}

    //public interface IPacketStorage
    //{
    //    void SaveAtomic(IEnumerable<ITransportPacket> packets);

    //    IEnumerable<ITransportPacket> GetTransportMessages();
    //}


    /// <summary>
    /// данные об агенте для передачи транспортного сообщения
    /// </summary>
    public class TransportAgentInfo
    {
        public string Domain { get; set; }
        public string ComputerName { get; set; }
        public string DbToken { get; set; }
        public string ConfigToken { get; set; }
        public int ConfigVersion { get; set; }
        //
        public string OrganizationUnit { get; set; }
    }
}
