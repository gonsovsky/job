using System;

namespace Atoll.Transport.Client.Contract
{


    /// <summary>
    /// Сервис получения данных об агенте для отправки сообщений
    /// </summary>
    public interface ITransportAgentInfoService
    {
        TransportAgentInfo Get();
        void SetDbToken(TransportDbTokenData data);
        void SetStaticConfig(TransportStaticConfig data);
    }
}
