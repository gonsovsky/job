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
#if NETSTANDARD2_0
using System.Net.Http;
using System.Net.Http.Headers;
#endif
using System.Threading.Tasks;
using Atoll.Transport.Client.Contract;
using Coral.Atoll.Utils;
using Atoll.Transport.Client.Bundle.Dto;
using Atoll.Transport.Contract;

namespace Atoll.Transport.Client.Bundle
{


    public class TransportSenderWorker : ITransportSenderWorker
    {


        private readonly IPacketManager packetManager;
#if NETSTANDARD2_0
        private readonly HttpClient httpClient;
#endif
        private readonly string url;
        private readonly ITransportSettingsProvider settingsProvider;
        private readonly ISendStateStore sendStateStore;
        private readonly ITransportAgentInfoService agentInfoService;
        private readonly IConfigurationStoreService configurationStore;
        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);
        private Encoding encoding;
        private readonly Func<HashAlgorithm> hashAlgorithmFunc;


        public TransportSenderWorker(IPacketManager packetManager,
            ITransportAgentInfoService agentInfoService,
            IConfigurationStoreService configurationStore,
            string url,
            TransportSettings settings,
            ISendStateStore sendStateStore)
            : this(packetManager, agentInfoService, configurationStore, url, new ConstantTransportSettingsProvider(settings), sendStateStore)
        {
        }

        public TransportSenderWorker(IPacketManager packetManager, 
            ITransportAgentInfoService agentInfoService, 
            IConfigurationStoreService configurationStore,
            string url,
            ITransportSettingsProvider settingsProvider,
            ISendStateStore sendStateStore)
        {
#if NETSTANDARD2_0
            this.httpClient = new HttpClient();
#endif
            this.packetManager = packetManager;
            this.agentInfoService = agentInfoService;
            this.configurationStore = configurationStore;
            this.url = url;
            this.settingsProvider = settingsProvider;
            this.sendStateStore = sendStateStore;
            this.hashAlgorithmFunc = (() => null);
        }

        private static Task Delay(TimeSpan timeSpan)
        {
            var tcs = new TaskCompletionSource<bool>();
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += (obj, args) =>
            {
                tcs.TrySetResult(true);
            };
            timer.Interval = timeSpan.TotalMilliseconds;
            timer.AutoReset = false;
            timer.Start();
            return tcs.Task;
        }

        public SendMessageHeaders GetSendMessageHeaders(SendIterationContext context)
        {
            var info = agentInfoService.Get();

            var headers = new SendMessageHeaders
            {
                DomainName = info.Domain,
                ComputerName = info.ComputerName,
                DbToken = info.DbToken,
                ConfigToken = info.ConfigToken,
                ConfigVersion = info.ConfigVersion,
                OrganizationUnit = info.OrganizationUnit,
                Attempt = context.Attempt,
                Format = TransportConstants.RequestFormDataFormat,
                Hints = new[] { MessageHints.OrderedFormData, MessageHints.ConfigurationResponseAsHttp399 },
            };

            return headers;
        }

        private int processing = 0;

        private bool NeedUpdateConfigs(SendIterationContext ctx)
        {
            var state = this.sendStateStore.Get();
            // если нужно докачать конфигурации
            var needUpdateConfigs = state.LastConfigurationsUpdate + ctx.TransportSettings.ConfigurationUpdateTimeout < DateTime.UtcNow;
            return needUpdateConfigs;
        }

        private bool NeedSendPacketsByTimeout(SendIterationContext ctx)
        {
            var state = this.sendStateStore.Get();
            // если нужно отправить пакеты
            var needSendPackets = state.FirstPacketsSizeEvaluation + ctx.TransportSettings.CollectMinPacketSizeTimeout < DateTime.UtcNow;
            return needSendPackets;
        }

        private SendIterationContext CreateSendIterationConxtext(IList<ITransportPacketInfo> currentPacketInfos)
        {
            var settings = this.settingsProvider.GetSettings();
            var ctx = new SendIterationContext
            {
                SendPackets = true,
                RequestConfigurations = true,
                TransportSettings = settings,
            };

            return ctx;
        }

        public void Process()
        {
            if (Interlocked.CompareExchange(ref this.processing, 1, 0) == 1)
            {
                return;
            }

            try
            {
                // TODO так отсылка сообщений идёт неравномерно для разных провайдеров
                var packetInfos = packetManager
                        .GetTransportProviderInfos()
                        .SelectMany(x => x.GetPackets())
                        .OrderBy(x => x.Identity.OrderValue)
                        .ToList();

                var configurationsToDownloadInfos = this.configurationStore.GetStateItems().Select(ConfigurationRequestExtensions.ToRequestItem).ToList();                

                // TODO проверять мин. размер и таймаут (можно ещё проверить что полный размер можно разбить на куски к которым применяется PacketSizeLimits)
                var size = packetInfos.Sum(x => x.Length);
                SendIterationContext context = this.CreateSendIterationConxtext(packetInfos);

                var needSendPackets = size > context.TransportSettings.PacketSizeLimits.Min || this.NeedSendPacketsByTimeout(context);
                // если нужно cкачать конфигурации
                var needUpdateConfigs = this.NeedUpdateConfigs(context);
                if (size > 0 && !needSendPackets)
                {
                    // отмечаем что рассчитан размер пакетов но им не требуется отправка
                    this.sendStateStore.CheckedPacketsSize();
                }

                if (!needSendPackets && !needUpdateConfigs)
                {
                    Interlocked.Exchange(ref this.processing, 0);
                    return;
                }

                SendResult sendResult = null;
                do
                {
                    // отправляем и сохраняем данные по ответу
                    sendResult = this.SendIteration(packetInfos, configurationsToDownloadInfos, context);

                    if (sendResult.TimeoutToNextTry.HasValue)
                    {
                        context.Attempt++;
                        context.FirstFailTimeUtc = context.FirstFailTimeUtc ?? DateTime.UtcNow;
                        Delay(sendResult.TimeoutToNextTry.Value).Wait();
                    }

                    // отправлять старые данные не нужно, дождёмся следующей отправки
                    if (sendResult.ServerDbChanged)
                    {
                        // TODO следует сократить интервал до следующей отправки?
                        return;
                    }

                    // TODO переписать лучше
                    packetInfos = packetInfos
                        .Where(x => !sendResult.IgnoredPackets.Contains(x) && !sendResult.TransferedPackets.Any(tp => tp.Result == PacketProcessingResult.Saved && tp.SendStats.TransferCompleted && tp.PacketInfo.Identity == x.Identity))
                        .OrderBy(x => x.Identity.OrderValue)
                        .ToList();

                    configurationsToDownloadInfos = this.configurationStore.GetStateItems().Select(ConfigurationRequestExtensions.ToRequestItem).ToList();

                    context = this.CreateSendIterationConxtext(packetInfos);

                    // нужно ли обновить конфиг
                    needUpdateConfigs = this.NeedUpdateConfigs(context);

                } while (packetInfos.Any() && (configurationsToDownloadInfos.Any(x => !x.IsCompleted) || needUpdateConfigs));

                // после отправки пробуем обнулить счётчик для упорядочивания пакетов
                this.packetManager.ReInitOrderCounter();
            }
            finally
            {
                Interlocked.Exchange(ref this.processing, 0);
            }
        }

        protected virtual string GetUri(SendMessageHeaders headers)
        {
            return QueryHelpers.AddQueryString(url, headers.ToDictionary());
        }

        protected virtual TransportSendStats WriteMessageToBody(string formDataBoundary, Stream requestStream, IList<ITransportPacketInfo> packetInfos, IList<ConfigurationRequestDataItem> configurationsToDownloadInfos, SendIterationContext context, Func<bool> cancelSendFunc)
        {
            var packetsSendStats = new Dictionary<ITransportPacketInfo, SendStats>();
            var completedPackets = new List<ITransportPacketInfo>();
            using (var nonClosingStream = new NonClosingStreamWrapper(requestStream))
            using (var sr = new StreamWriter(nonClosingStream, this.encoding ?? DefaultEncoding))
            using (var formDataWriter = new FormDataWriter(nonClosingStream, formDataBoundary, this.encoding ?? DefaultEncoding))
            {
                var messageCapacityReached = false;

                if (context.RequestConfigurations)
                {
                    // записываем данные по конфигурациям
                    for (int i = 0; i < configurationsToDownloadInfos.Count; i++)
                    {
                        if (cancelSendFunc())
                        {
                            break;
                        }

                        var configurationsToDownloadInfo = configurationsToDownloadInfos[i];

                        // == confs[0].
                        var itemPrefix = string.Concat(TransportConstants.FormDataConfigurationProp, "[", i, "].");

                        // == confs[0].ProviderKey
                        formDataWriter.WriteValue(string.Concat(itemPrefix, nameof(ConfigurationRequestDataItem.ProviderKey)), configurationsToDownloadInfo.ProviderKey);
                        formDataWriter.WriteValue(string.Concat(itemPrefix, nameof(ConfigurationRequestDataItem.Token)), configurationsToDownloadInfo.Token);
                        if (configurationsToDownloadInfo.StartPosition != null)
                        {
                            formDataWriter.WriteValue(string.Concat(itemPrefix, nameof(ConfigurationRequestDataItem.StartPosition)), configurationsToDownloadInfo.StartPosition);
                        }
                        formDataWriter.WriteValue(string.Concat(itemPrefix, nameof(ConfigurationRequestDataItem.IsCompleted)), configurationsToDownloadInfo.IsCompleted);
                    }
                }

                if (context.SendPackets)
                {
                    //formDataWriter.ResetSize();

                    bool hasWrittenPackagePartToRequest = false;
                    // записываем данные по пакетам
                    for (int i = 0; i < packetInfos.Count; i++)
                    {
                        if (cancelSendFunc())
                        {
                            break;
                        }

                        var packetInfo = packetInfos[i];

                        var sendStats = this.packetManager.ReadSendStats(packetInfo.ProviderKey, packetInfo.Identity);
                        if (sendStats?.TransferCompleted == true)
                        {
                            completedPackets.Add(packetInfo);
                            continue;
                        }

                        int packetBytesSended = sendStats?.TransferedBytes ?? 0;
                        // буфер ~16 kb (довольно много можно уместить)
                        int bufferSize = 4 * 4096;
                        bool packetTransferCompleted = false;
                        string packetBlockHashStr;
                        // == packets[0].
                        var itemPrefix = string.Concat(TransportConstants.FormDataPacketsProp, "[", i, "].");
                        // записываем данные пакета
                        using (var packetStream = packetInfo.GetReadOnlyStreamOrDefault())
                        {
                            // пакет был удалён
                            if (packetStream == null)
                            {
                                completedPackets.Add(packetInfo);
                                continue;
                            }

                            // записываем метаданные о пакете
                            // == packets[0].ProviderKey
                            formDataWriter.WriteValue(string.Concat(itemPrefix, nameof(PacketFormDataItem.ProviderKey)), packetInfo.ProviderKey);
                            formDataWriter.WriteValue(string.Concat(itemPrefix, nameof(PacketFormDataItem.PacketId)), packetInfo.Identity.PacketId);
                            formDataWriter.WriteValue(string.Concat(itemPrefix, nameof(PacketFormDataItem.StartPosition)), packetBytesSended);

                            packetStream.Seek(packetBytesSended, SeekOrigin.Begin);

                            using (var hashAlgorithm = this.hashAlgorithmFunc())
                            {
                                var packetIdStr = packetInfo.Identity.PacketId.ToString();
                                formDataWriter.WriteFileHeader(string.Concat(itemPrefix, nameof(PacketFormDataItem.FileKey)), packetIdStr);

                                byte[] buffer = new byte[bufferSize];
                                int read = 0;
                                bool writtenToEnd = false;
                                while (!messageCapacityReached && (read = packetStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    // подумать над реализацией которая будет делать меньшую фрагментацию пакетови не будет записывать "лишние"
                                    if (formDataWriter.GetWrittenSize() + read > context.TransportSettings.PacketSizeLimits.Max)
                                    {
                                        // записываем только до максимального размера
                                        var toWrite = context.TransportSettings.PacketSizeLimits.Max - formDataWriter.GetWrittenSize();
                                        
                                        if (toWrite < 0)
                                        {
                                            var originalReadFromPacket = read;
                                            if (hasWrittenPackagePartToRequest)
                                            {
                                                read = Math.Min(context.TransportSettings.PacketSizeLimits.Min, Math.Min(read, 4096));
                                            }
                                            else
                                            {
                                                read = read < context.TransportSettings.PacketSizeLimits.Max ? read : context.TransportSettings.PacketSizeLimits.Max;
                                            }

                                            writtenToEnd = originalReadFromPacket <= read && packetStream.ReadByte() == -1;
                                        }
                                        else
                                        {
                                            read = toWrite;
                                        }
                                        
                                        messageCapacityReached = true;
                                    }

                                    formDataWriter.Write(buffer, 0, read);
                                    hasWrittenPackagePartToRequest = true;

                                    if (hashAlgorithm != null)
                                    {
                                        // hash contents
                                        hashAlgorithm.TransformBlock(buffer, 0, read, null, 0);
                                    }

                                    packetBytesSended = packetBytesSended + read;
                                }

                                if (read <= 0 || writtenToEnd)
                                {
                                    packetTransferCompleted = true;
                                }

                                if (hashAlgorithm != null)
                                {
                                    hashAlgorithm.TransformFinalBlock(new byte[0], 0, 0);
                                    packetBlockHashStr = hashAlgorithm.GetHashString();
                                }
                            }
                        }

                        formDataWriter.WriteValue(string.Concat(itemPrefix, nameof(PacketFormDataItem.EndPosition)), packetBytesSended);

                        //// записываем хеш данные блока пакета
                        //formDataWriter.WriteValue(nameof(PacketFormDataItem.Hash) + suffix, packetBlockHashStr);
                        formDataWriter.WriteValue(string.Concat(itemPrefix, nameof(PacketFormDataItem.IsFinal)), packetTransferCompleted);

                        // данные о предыдущей части
                        if (sendStats?.PreviousPartIdentity != null)
                        {
                            formDataWriter.WriteValue(string.Concat(itemPrefix, nameof(PacketFormDataItem.PreviousPartId)), sendStats.PreviousPartIdentity.Id);
                            formDataWriter.WriteValue(string.Concat(itemPrefix, nameof(PacketFormDataItem.PreviousPartStorageToken)), sendStats.PreviousPartIdentity.StorageToken);
                        }

                        packetsSendStats.Add(packetInfo, new SendStats
                        {
                            TransferedBytes = packetBytesSended,
                            TransferCompleted = packetTransferCompleted,
                        });

                        if (messageCapacityReached)
                        {
                            break;
                        }
                    }
                }  
            }

            return new TransportSendStats
            {
                IgnoredPackets = completedPackets,
                SendedPacketsStats = packetsSendStats,
                ConfigurationsInfos = configurationsToDownloadInfos,
            };
        }

        protected virtual SendResult SendIteration(IList<ITransportPacketInfo> packetInfos, IList<ConfigurationRequestDataItem> configurationsToDownloadInfos, SendIterationContext context)
        {
            var url = this.GetUri(this.GetSendMessageHeaders(context));

            string formDataBoundary = string.Format("----------{0:N}", Guid.NewGuid());
            string contentType = "multipart/form-data; boundary=" + formDataBoundary;

            // для net40
#if !NETSTANDARD2_0
            try
            {
                var req = WebRequest.Create(url) as HttpWebRequest;
                //req.KeepAlive = true;
                //req.KeepAlive = false;
                req.AllowWriteStreamBuffering = false;
                req.SendChunked = true;
                req.ServicePoint.Expect100Continue = true;
                req.Method = "POST";
                req.ContentType = contentType;
                //req.ContinueTimeout = xxx;

                TransportSendStats sendededStats = null;
                using (Stream requestStream = req.GetRequestStream())
                {
                    sendededStats = this.WriteMessageToBody(formDataBoundary, requestStream, packetInfos, configurationsToDownloadInfos, context, () => req.HaveResponse);

                    // для тестирования...
                    //var length = 1 * 1024 * 1024 * 1024;
                    //var batch = 1 * 1024 * 1024;
                    //for (int i = 0; i < length; i += batch)
                    //{
                    //    var batchBuffer = new List<byte>();
                    //    for (int j = 0; j < batch; j++)
                    //    {
                    //        batchBuffer.Add(0);
                    //    }
                    //    if (req.HaveResponse)
                    //    {
                    //        break;
                    //    }
                    //    requestStream.Write(batchBuffer.ToArray(), 0, batchBuffer.Count);
                    //}
                }

                try
                {
                    using (var response = req.GetResponse())
                    using (var resStream = response.GetResponseStream())
                    using (var streamReader = new StreamReader(resStream))
                    {
                        // обрабатывает ответ
                        return this.SaveRequestResults(resStream, sendededStats, context);
                    }
                }
                catch (WebException ex)
                {
                    var wRespStatusCode = ((HttpWebResponse)ex.Response).StatusCode;
                    // кастомная обработка конфигурации (для проблемы когда тело запроса с агента отправляется, хотя в этом нет смысла - лишние данные ходят по сети, это можно исправить)
                    if ((int)wRespStatusCode == 399)
                    {
                        using (var resStream = ((HttpWebResponse)ex.Response).GetResponseStream())
                        using (var streamReader = new StreamReader(resStream))
                        {
                            // обрабатывает ответ
                            return this.SaveRequestResults(resStream, sendededStats, context);
                        }
                    }
                    if ((int)wRespStatusCode == 429)
                    {
                        var timeout = int.TryParse(ex.Response.Headers.GetValues("Retry-After").FirstOrDefault(), out var retryTm)
                            ? retryTm
                            : Convert.ToInt64(context.TransportSettings.ErrorRetryTimeout.TotalMilliseconds);

                        return SendResult.Retry(timeout);
                    }

                    // при общих ошибках
                    return SendResult.Retry(context.TransportSettings.ServerErrorRetryTimeout);
                }
            }
            catch (Exception)
            {
                // при общих ошибках
                return SendResult.Retry(context.TransportSettings.ErrorRetryTimeout);
            }
#else
            try
            {
                TransportSendStats sendedStats = null;
                var reqContent = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new WriteToStreamContent((requestStream, ctx) =>
                    {
                        sendedStats = this.WriteMessageToBody(formDataBoundary, requestStream, packetInfos, configurationsToDownloadInfos, context, () => false);
                        
                        // для тестирования...
                        //var length = 1 * 1024 * 1024 * 1024;
                        //var batch = 1 * 1024 * 1024;
                        //for (int i = 0; i < length; i += batch)
                        //{
                        //    var batchBuffer = new List<byte>();
                        //    for (int j = 0; j < batch; j++)
                        //    {
                        //        batchBuffer.Add(0);
                        //    }
                        //    //if (req.HaveResponse)
                        //    //{
                        //    //    break;
                        //    //}
                        //    requestStream.Write(batchBuffer.ToArray(), 0, batchBuffer.Count);
                        //}
                    }),
                };

                // workaround net core 2.2 и тех кто использует библиотеку System.Net.Http под netstandard (с версии 4.2.1.0)
                // https://github.com/dotnet/corefx/blob/v2.1.5/src/System.Net.Http/src/System/Net/Http/SocketsHttpHandler/HttpConnection.cs#L537  (версия 2.1.5 указана для примера)
                // нужно для того чтобы в случаях "отказов" не происходило чтение всего тела запроса, а читались только основные параметры
                reqContent.Headers.ExpectContinue = true;
                reqContent.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                //reqContent.Version = new Version(1, 1);
                var result = this.httpClient.SendAsync(reqContent, HttpCompletionOption.ResponseHeadersRead).Result;

                if (result.IsSuccessStatusCode)
                {
                    using (var stream = result.Content.ReadAsStreamAsync().Result)
                    {
                        // обрабатывает ответ
                        return this.SaveRequestResults(stream, sendedStats, context);
                    };
                }
                else
                {
                    // кастомная обработка конфигурации (для проблемы когда тело запроса с агента отправляется, хотя в этом нет смысла - лишние данные ходят по сети, это можно исправить)
                    if ((int)result.StatusCode == 399)
                    {
                        using (var stream = result.Content.ReadAsStreamAsync().Result)
                        {
                            // обрабатывает ответ
                            return this.SaveRequestResults(stream, sendedStats, context);
                        };
                    }
                    if ((int)result.StatusCode == 429)
                    {
                        var timeout = int.TryParse(result.Headers.GetValues("Retry-After").FirstOrDefault(), out var retryTm)
                                        ? retryTm
                                        : Convert.ToInt64(context.TransportSettings.ErrorRetryTimeout.TotalMilliseconds);

                        return SendResult.Retry(timeout);
                    }

                    return SendResult.Retry(context.TransportSettings.ServerErrorRetryTimeout);
                }
            }
            catch (Exception)
            {
                // при общих ошибках
                return SendResult.Retry(context.TransportSettings.ErrorRetryTimeout);
            }
#endif
        }

        protected virtual SendResult SaveRequestResults(Stream responseStream, TransportSendStats sendStats, SendIterationContext context)
        {
            bool needRetry = false;
            bool serverDbChanged = false;
            TransportResponseStats responseStats;
            using (var streamReader = new StreamReader(responseStream))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                var jsonSerializer = new JsonSerializer();
                // сохраним потоки данных конфигураций при десериализации
                // TODO Доработать configurationsConverter
                var configurationsConverter = new TransportResponseConfigurationsConverter(this.configurationStore);
                jsonSerializer
                    .Converters
                    .Add(configurationsConverter);

                var response = jsonSerializer.Deserialize<TransportResponse>(jsonReader);

                if (response.ErrorMessage != null)
                {
                    SendResult.Retry(context.TransportSettings.ErrorRetryTimeout);
                }

                // изменеилась статическая конфигурация
                if (response.StaticConfigData != null)
                {
                    this.agentInfoService.SetStaticConfig(new TransportStaticConfig
                    {
                        ConfigToken = response.StaticConfigData.ConfigToken,
                        ConfigVersion = response.StaticConfigData.ConfigVersion,
                    });

                    return SendResult.Retry(TransportConstants.DefaultUpdateStaticConfigTimeout);
                }
                else
                {
                    // ConfigurationsByteArraysToFilesConverter используется при сохранении конфигураций
                    this.sendStateStore.ConfigurationsUpdated();

                    // изменилась бд системы (например сменили сервера)
                    if (response.DbTokenData != null)
                    {
                        serverDbChanged = true;

                        this.agentInfoService.SetDbToken(new TransportDbTokenData
                        {
                            DbToken = response.DbTokenData.DbToken
                        });

                        //return SendResult.Retry(TransportConstants.DefaultUpdateDbTokenTimeout);
                    }

                    responseStats = new TransportResponseStats()
                    {
                        SendedPacketsStats = sendStats.SendedPacketsStats,
                        // изменилась бд системы
                        TransferedPacketsProcessingResults = response.DbTokenData != null ? new List<TransferedPacketStats>() : sendStats.SendedPacketsStats.Select(x =>
                        {
                            var transferedPacketResponse = response.TransferedPackets.First(p => p.PacketId == x.Key.Identity.PacketId && p.ProviderKey == x.Key.ProviderKey);
                            // 
                            x.Value.PreviousPartIdentity = new PacketPartIdentity(transferedPacketResponse.StorageToken, transferedPacketResponse.Id);
                            return new TransferedPacketStats
                            {
                                PacketInfo = x.Key,
                                SendStats = x.Value,
                                Result = transferedPacketResponse.Result,
                            };
                        }).ToList(),
                    };
                }
            }

            // сохраняем данные
            var packets = responseStats.TransferedPacketsProcessingResults;
            if (serverDbChanged)
            {
                this.packetManager.RemoveAll();
            }
            else
            {
                foreach (var item in packets)
                {
                    var packet = item.PacketInfo;
                    if (item.Result == PacketProcessingResult.Saved)
                    {
                        this.packetManager.SaveSendStats(packet.ProviderKey, packet.Identity, item.SendStats);
                    }
                    else if (item.Result == PacketProcessingResult.Error)
                    {
                        needRetry = true;
                    }
                    else if (item.Result == PacketProcessingResult.Resend)
                    {
                        // сбрасываем статистику
                        this.packetManager.SaveSendStats(packet.ProviderKey, packet.Identity, new SendStats());
                    }
                    // игнорим эти результаты (если потом появится обработчик на сервере или обновиться агент до нормальной версии и в нём будет другая обработка, предполагаю что следующий набор данных будет доставлен успешно)
                    //else if (item.Result == PacketProcessingResult.Unknown)
                    //{
                    //}
                    //else if (item.Result == PacketProcessingResult.NoProcessor)
                    //{
                    //}
                    else
                    {
                        this.packetManager.SaveSendStats(packet.ProviderKey, packet.Identity, item.SendStats);
                    }
                }
            }

            return needRetry
                ? SendResult.Retry(context.TransportSettings.ErrorRetryTimeout)
                : SendResult.Success(serverDbChanged, packets, sendStats.IgnoredPackets);
        }
    }
}
