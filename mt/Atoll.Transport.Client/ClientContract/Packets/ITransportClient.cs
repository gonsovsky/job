using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Atoll.Transport.Client.Contract
{
    /// <summary>
    /// сервис для пакетной передачи данных на сервер
    /// </summary>
    public interface ITransportClient
    {
        /// <summary>
        /// создать сессию записи пакетных данных
        /// </summary>
        /// <param name="providerKey">ключ провайдера данных</param>
        ITransportSession CreateSession(string providerKey);

        ITransportDataWriter CreateWriter(string providerKey, CommitOptions commitOptions = CommitOptions.DeletePrevious);
    }
}
