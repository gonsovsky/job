#if NETSTANDARD2_0
#endif

namespace ClassLibrary1.Transport
{
    ///// <summary>
    ///// передаваемый блок данных в сообщении
    ///// для передачи, мы разбиваем пакет на части и отправляем его на сервер по частям
    ///// данный интерфейс это как раз передааваемая часть пакета
    ///// </summary>
    //public interface ITransportSendBlock
    //{
    //    long Id { get; }
    //    string ProviderKey { get; }
    //    int Length { get; }
    //    Stream GetReadOnlyStream();
    //}

    ///// <summary>
    ///// <see cref="ITransportSendBlock"/>
    ///// </summary>
    //public class TransportSendBlock : ITransportSendBlock
    //{
    //    public long Id { get; }
    //    public string ProviderKey { get; }
    //    public int Length { get; }

    //    private readonly Func<Stream> streamFactory;

    //    public TransportSendBlock(string providerKey, long id, int length, Func<Stream> streamFactory)
    //    {
    //        this.Id = id;
    //        this.ProviderKey = providerKey;
    //        this.streamFactory = streamFactory;
    //        this.Length = length;
    //    }

    //    public Stream GetReadOnlyStream()
    //    {
    //        return new ReadOnlyStream(this.streamFactory());
    //    }
    //}

    /// <summary>
    /// пакет(/файл) данных который будет доставлен на сервер и обработан как единое целое
    /// </summary>
    public interface ITransportPacket
    {
        string Id { get; }
        long Length { get; }
        void Write(byte[] bytes);

        /// <summary>
        /// сохраняет пакет
        /// </summary>
        /// <remarks>после сохранения в пакет как правило нельзя записывать данные</remarks>
        void Save(string providerKey, IPacketManager packetManager);
    }
}
