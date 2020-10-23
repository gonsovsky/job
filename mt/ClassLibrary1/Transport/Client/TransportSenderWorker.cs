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

namespace ClassLibrary1.Transport
{

    public class SendResult
    {
        public bool IsSended { get; private set; }
        public TimeSpan? TimeoutToNextTry { get; private set; }

        public SendResult(bool isSuccess, TimeSpan? timeoutToNextTry)
        {
            this.IsSended = isSuccess;
            this.TimeoutToNextTry = timeoutToNextTry;
        }

        public static SendResult Success()
        {
            return new SendResult(true, null);
        }

        public static SendResult Retry(long nextTryMilliseconds)
        {
            return new SendResult(false, TimeSpan.FromMilliseconds(nextTryMilliseconds));
        }

        public static SendResult Retry(TimeSpan nextTry)
        {
            return new SendResult(false, nextTry);
        }
    }

    public class SendSettings
    {
        public TimeSpan ErrorRetryTimeout { get; set; }
        public SendMessageSizeLimits SizeLimits { get; set; }
    }

    public enum MessageHints
    {
        None = 0,
        OrderedFormData = 1,
    }

    /// <summary>
    /// загловки для сообщения отправляемого на сервер
    /// </summary>
    public class SendMessageHeaders
    {
        //
        public string Domain { get; set; }
        public string ComputerName { get; set; }
        public string DbToken { get; set; }
        public string ConfigToken { get; set; }
        public int ConfigVersion { get; set; }

        //
        public string OrganizationUnit { get; set; }

        //
        public int Attempt { get; set; }
        //public int Timeout { get; set; }

        /// <summary>
        /// формат который используется для передачи пакетов\файлов в запросе
        /// </summary>
        public string Format { get; set; }

        public MessageHints[] Hints { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            var paramsDict = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(this.Domain))
            {
                paramsDict.Add(nameof(this.Domain), this.Domain);
            }

            if (!string.IsNullOrEmpty(this.ComputerName))
            {
                paramsDict.Add(nameof(this.ComputerName), this.ComputerName);
            }

            if (!string.IsNullOrEmpty(this.DbToken))
            {
                paramsDict.Add(nameof(this.DbToken), this.DbToken);
            }

            if (!string.IsNullOrEmpty(this.ConfigToken))
            {
                paramsDict.Add(nameof(this.ConfigToken), this.ConfigToken);
            }

            var configVersion = this.ConfigVersion.ToString();
            if (!string.IsNullOrEmpty(configVersion))
            {
                paramsDict.Add(nameof(this.ConfigVersion), configVersion);
            }

            if (!string.IsNullOrEmpty(this.OrganizationUnit))
            {
                paramsDict.Add(nameof(this.OrganizationUnit), this.OrganizationUnit);
            }

            var attempt = this.Attempt.ToString();
            if (!string.IsNullOrEmpty(attempt))
            {
                paramsDict.Add(nameof(this.Attempt), attempt);
            }

            if (this.Hints != null && this.Hints.Any())
            {
                paramsDict.Add(nameof(this.Hints), GetHintsString(this.Hints));
            }

            return paramsDict;
        }

        public static SendMessageHeaders FromDictionary(IDictionary<string, string> dict)
        {
            var headers = new SendMessageHeaders();
            headers.Domain = dict.GetOrDefault(nameof(headers.Domain));
            headers.ComputerName = dict.GetOrDefault(nameof(headers.ComputerName));
            headers.DbToken = dict.GetOrDefault(nameof(headers.DbToken));
            headers.ConfigToken = dict.GetOrDefault(nameof(headers.ConfigToken));
            headers.OrganizationUnit = dict.GetOrDefault(nameof(headers.OrganizationUnit));
            headers.ConfigVersion = dict.GetOrDefault(nameof(headers.ConfigVersion), int.Parse);
            headers.Attempt = dict.GetOrDefault(nameof(headers.Attempt), int.Parse);
            headers.Hints = dict.GetOrDefault(nameof(headers.Attempt), GetHints);

            return headers;
        }

        private static string SplitDelimiter = ",";
        private static string[] SplitDelimiterArray = new string[] { SplitDelimiter };
        private static MessageHints[] GetHints(string hintsStr)
        {
            if (string.IsNullOrEmpty(hintsStr))
            {
                return new MessageHints[0];
            }

            return hintsStr.Split(SplitDelimiterArray, StringSplitOptions.RemoveEmptyEntries)
                .Select(x =>
                {
                    x = x.Trim();
                    return Enum.TryParse<MessageHints>(x, out var result)
                            ? result
                            : MessageHints.None;
                })
            .Where(x => x != MessageHints.None)
            .ToArray();
        }

        private static string GetHintsString(MessageHints[] hints)
        {
            return string.Join(SplitDelimiter, hints.Select(x => x.ToString()));
        }
    }

    ///// <summary>
    ///// сообщение отправляемое на сервер
    ///// </summary>
    //public class SendMessage
    //{
    //    public SendMessageHeaders Headers { get; set; }
    //    public List<ITransportSendBlock> SendBlocks { get; set; }
    //}

    //public class SendMessageContent
    //{
    //    private IDictionary<ITransportPacketInfo, SendStats> sendStats;
    //    private IEnumerable<ITransportPacketInfo> packetInfos;

    //    public SendMessageContent(IEnumerable<ITransportPacketInfo> packetInfos)
    //    {
    //        this.packetInfos = packetInfos;
    //    }

    //    protected virtual async Task WriteToRequestStream(Stream stream)
    //    {
    //        foreach (var packetInfo in this.packetInfos)
    //        {
    //            using (var packetStream = packetInfo.)
    //            {

    //            }
    //        }
    //    }
    //}


    //public class PacketInfoContext
    //{
    //    private readonly SendMessageSizeLimits sizeLimits;
    //    private BlockingCollection<ITransportPacketInfo> packetInfos;

    //    public PacketInfoContext(IList<ITransportPacketInfo> packetInfos, SendMessageSizeLimits sizeLimits)
    //    {
    //        this.packetInfos = new BlockingCollection<ITransportPacketInfo>(new ConcurrentQueue<ITransportPacketInfo>(packetInfos));
    //        this.sizeLimits = sizeLimits;
    //    }

    //    public async Task WriteToRequest(Stream reqStream)
    //    {
    //        try
    //        {
    //            long count = 0;
    //            while (packetInfos.TryTake(out var packetInfo))
    //            {
    //                using (var packetStream = packetInfo.GetReadOnlyStream())
    //                {

    //                }
    //            }
    //        }
    //        finally
    //        {

    //        }
    //    }
    //}

    public class TransferedPacketStats
    {
        public ITransportPacketInfo PacketInfo { get; set; }

        public SendStats SendStats { get; set; }

        public PacketProcessingResult Result { get; set; }

    }

    //public enum PacketTransferingResult
    //{
    //    Unknown = 0,
    //    Success = 1,
    //    NoProcessor = 2,
    //    Error = 3,
    //    Resend = 4,
    //}

    //public static class PacketProcessingResultHelper
    //{
    //    public static PacketTransferingResult FromResponseValue(int val)
    //    {
    //        switch (val)
    //        {
    //            case 0:
    //                return PacketTransferingResult.Unknown;
    //                break;
    //            case 1:

    //                return PacketTransferingResult.Success;
    //                break;
    //            case 2:
    //                return PacketTransferingResult.NoProcessor;
    //                break;
    //            case 3:
    //                return PacketTransferingResult.Error;
    //                break;
    //            default:
    //                return PacketTransferingResult.Unknown;
    //                break;
    //        }
    //    }
    //}

    public class TransportResponseStats
    {
        public IDictionary<ITransportPacketInfo, SendStats> SendedPacketsStats { get; set; }

        public IList<TransferedPacketStats> TransferedPacketsProcessingResults { get; set; }

        public TransportResponseStats()
        {
            this.SendedPacketsStats = new Dictionary<ITransportPacketInfo, SendStats>();
            this.TransferedPacketsProcessingResults = new List<TransferedPacketStats>();
        }
    }

    public class TransportSendStats
    {
        public IDictionary<ITransportPacketInfo, SendStats> SendedPacketsStats { get; set; }
        public IList<ConfigurationRequestDataItem> ConfigurationsInfos { get; set; }

        public TransportSendStats()
        {
            this.SendedPacketsStats = new Dictionary<ITransportPacketInfo, SendStats>();
        }
    }

    public class SendIterationContext
    {
        public bool SendPackets { get; set; }
        public bool RequestConfigurations{ get; set; }
        public int MessageSize { get; set; }

        public int Attempt { get; set; }
        public DateTime? FirstFailTimeUtc { get; set; }
    }

    /// <summary>
    /// Часть данных состояния в процессе работы отправителя
    /// </summary>
    public class SendState
    {

        public DateTime? LastConfigurationsUpdate { get; set; }

        public DateTime? FirstPacketsSizeEvaluation { get; set; }

        public SendState Clone()
        {
            return new SendState
            {
                LastConfigurationsUpdate = this.LastConfigurationsUpdate,
                FirstPacketsSizeEvaluation = this.FirstPacketsSizeEvaluation,
            };
        }
    }

    public interface ISendStateStore
    {
        SendState Get();
        void ConfigurationsUpdated();
        void CheckedPacketsSize();

    }

    public class SendStateStore : ISendStateStore
    {
        private SendState current = new SendState();

        public void CheckedPacketsSize()
        {
            if (current.FirstPacketsSizeEvaluation == null)
            {
                var newValue = current.Clone();
                newValue.FirstPacketsSizeEvaluation = DateTime.UtcNow;
                this.Save(newValue);
            }
        }

        public void ConfigurationsUpdated()
        {
            var newValue = current.Clone();
            newValue.LastConfigurationsUpdate = DateTime.UtcNow;
            this.Save(newValue);
        }

        public SendState Get()
        {
            return this.current;
        }

        public void Save(SendState sendState)
        {
            this.current = sendState;
        }
    }


    public class PacketSizeCalculatorUtil
    {
        public static int Calculate(SendMessageSizeLimits packetSizeLimits, int fullSize, TimeSpan? timeout = null)
        {
            // пока свожу к уравнению d * x + z * y = 11
            // z between min and max
            // d between min and max
            // TODO реализовать решение
            return packetSizeLimits.Min;
        }
    }


    public class TransportSenderWorker : ITransportSenderWorker
    {


        private readonly IPacketManager packetManager;
#if NETSTANDARD2_0
        private readonly HttpClient httpClient;
#endif
        private readonly string url;
        private readonly TransportSettings settings;
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
        {
#if NETSTANDARD2_0
            this.httpClient = new HttpClient();
#endif
            this.packetManager = packetManager;
            this.agentInfoService = agentInfoService;
            this.configurationStore = configurationStore;
            this.url = url;
            this.settings = settings;
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
                Domain = info.Domain,
                ComputerName = info.ComputerName,
                DbToken = info.DbToken,
                ConfigToken = info.ConfigToken,
                ConfigVersion = info.ConfigVersion,
                OrganizationUnit = info.OrganizationUnit,
                Attempt = context.Attempt,
                Format = TransportConstants.RequestFormDataFormat,
                Hints = new[] { MessageHints.OrderedFormData },
            };

            return headers;
        }

        private int processing = 0;

        private bool NeedUpdateConfigs()
        {
            var state = this.sendStateStore.Get();
            // если нужно докачать конфигурации
            var needUpdateConfigs = state.LastConfigurationsUpdate + this.settings.ConfigurationUpdateTimeout > DateTime.UtcNow;
            return needUpdateConfigs;
        }

        private bool NeedSendPacketsByTimeout()
        {
            var state = this.sendStateStore.Get();
            // если нужно отправить пакеты
            var needSendPackets = state.FirstPacketsSizeEvaluation + this.settings.CollectMinPacketSizeTimeout > DateTime.UtcNow;
            return needSendPackets;
        }

        private int GetMessageSize(IList<ITransportPacketInfo> packetInfos)
        {
            return PacketSizeCalculatorUtil.Calculate(this.settings.PacketSizeLimits, packetInfos.Sum(x => x.Length));
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
                var packetInfos = this.packetManager
                    .GetTransportPacketInfos()
                    .OrderBy(x => x.Id)
                    .ToList();

                // TODO implement
                var configurationsToDownloadInfos = this.configurationStore.GetRequestItems();

                // TODO проверять мин. размер и таймаут (можно ещё проверить что полный размер можно разбить на куски к которым применяется PacketSizeLimits)
                var size = packetInfos.Sum(x => x.Length);
                var needSendPackets = size > this.settings.PacketSizeLimits.Min || this.NeedSendPacketsByTimeout();
                // если нужно cкачать конфигурации
                var needUpdateConfigs = this.NeedUpdateConfigs();
                if (size > 0 && !needSendPackets)
                {
                    this.sendStateStore.CheckedPacketsSize();
                }

                if (!needSendPackets && !needUpdateConfigs)
                {
                    Interlocked.Exchange(ref this.processing, 0);
                    return;
                }

                SendResult sendResult = null;
                SendIterationContext context = new SendIterationContext
                {
                    SendPackets = true,
                    RequestConfigurations = true,
                    MessageSize = this.GetMessageSize(packetInfos),
                };

                do
                {
                    // отправляем и сохраняем данные по ответу
                    sendResult = this.SendIteration(packetInfos, configurationsToDownloadInfos, context);

                    if (sendResult.TimeoutToNextTry.HasValue)
                    {
                        context.Attempt++;
                        context.FirstFailTimeUtc = context.FirstFailTimeUtc ?? DateTime.UtcNow;
                        Delay(sendResult.TimeoutToNextTry.Value)
                            .Wait();
                    }

                    // TODO надо оптимизировать !!! packetInfos будут подтягиваться из провайдера, а они уже есть в памяти
                    packetInfos = packetManager
                        .GetTransportPacketInfos()
                        .OrderBy(x => x.Id)
                        .ToList();

                    configurationsToDownloadInfos = this.configurationStore.GetRequestItems();

                    // нужно ли обновить конфиг
                    needUpdateConfigs = this.NeedUpdateConfigs();

                    if (sendResult.IsSended)
                    {
                        context = new SendIterationContext
                        {
                            SendPackets = true,
                            RequestConfigurations = true,
                            MessageSize = this.GetMessageSize(packetInfos),
                        };
                    }

                } while (packetInfos.Any() && (configurationsToDownloadInfos.Any(x => !x.IsCompleted) || needUpdateConfigs));
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

        protected virtual TransportSendStats WriteMessageToBody(string formDataBoundary, Stream requestStream, IList<ITransportPacketInfo> packetInfos, IList<ConfigurationRequestDataItem> configurationsToDownloadInfos, SendIterationContext context)
        {
            var packetsSendStats = new Dictionary<ITransportPacketInfo, SendStats>();
            using (var nonClosingStream = new NonClosingStreamWrapper(requestStream))
            using (var sr = new StreamWriter(nonClosingStream, this.encoding ?? DefaultEncoding))
            using (var formDataWriter = new FormDataWriter(nonClosingStream, formDataBoundary, this.encoding ?? DefaultEncoding))
            {
                var maxMessageSizeReached = false;

                if (context.RequestConfigurations)
                {
                    // записываем данные по конфигурациям
                    for (int i = 0; i < configurationsToDownloadInfos.Count; i++)
                    {
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
                    formDataWriter.ResetSize();

                    // записываем данные по пакетам
                    for (int i = 0; i < packetInfos.Count; i++)
                    {
                        var packetInfo = packetInfos[i];

                        var sendStats = this.packetManager.ReadSendStats(packetInfo.ProviderKey, packetInfo.Id);
                        if (sendStats?.TransferCompleted == true)
                        {
                            continue;
                        }

                        int packetBytesSended = sendStats?.TransferedBytes ?? 0;
                        int bufferSize = 4096;
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
                                continue;
                            }

                            // записываем метаданные о пакете
                            // == packets[0].ProviderKey
                            formDataWriter.WriteValue(string.Concat(itemPrefix, nameof(PacketFormDataItem.ProviderKey)), packetInfo.ProviderKey);
                            formDataWriter.WriteValue(string.Concat(itemPrefix, nameof(PacketFormDataItem.PacketId)), packetInfo.Id);
                            formDataWriter.WriteValue(string.Concat(itemPrefix, nameof(PacketFormDataItem.StartPosition)), packetBytesSended);

                            packetStream.Seek(packetBytesSended, SeekOrigin.Begin);

                            using (var hashAlgorithm = this.hashAlgorithmFunc())
                            {
                                var packetIdStr = packetInfo.Id.ToString();
                                formDataWriter.WriteFileHeader(string.Concat(itemPrefix, nameof(PacketFormDataItem.FileKey)), packetIdStr);

                                byte[] buffer = new byte[bufferSize];
                                int read = 0;
                                while (!maxMessageSizeReached && (read = packetStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    if (formDataWriter.GetWrittenSize() + read > this.settings.PacketSizeLimits.Max)
                                    {
                                        // записываем только до максимального размера
                                        read = this.settings.PacketSizeLimits.Max - formDataWriter.GetWrittenSize();
                                        maxMessageSizeReached = true;
                                    }

                                    formDataWriter.Write(buffer, 0, read);

                                    if (hashAlgorithm != null)
                                    {
                                        // hash contents
                                        hashAlgorithm.TransformBlock(buffer, 0, read, null, 0);
                                    }

                                    packetBytesSended = packetBytesSended + read;
                                }

                                if (read <= 0)
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

                        packetsSendStats.Add(packetInfo, new SendStats
                        {
                            TransferedBytes = packetBytesSended,
                            TransferCompleted = packetTransferCompleted,
                        });

                        if (maxMessageSizeReached)
                        {
                            break;
                        }
                    }
                }  
            }

            return new TransportSendStats
            {
                SendedPacketsStats = packetsSendStats,
                ConfigurationsInfos = configurationsToDownloadInfos,
            };
        }

        protected virtual SendResult SendIteration(IList<ITransportPacketInfo> packetInfos, IList<ConfigurationRequestDataItem> configurationsToDownloadInfos, SendIterationContext context)
        {
            var url = this.GetUri(this.GetSendMessageHeaders(context));

            string formDataBoundary = string.Format("----------{0:N}", Guid.NewGuid());
            string contentType = "multipart/form-data; boundary=" + formDataBoundary;

#if !NETSTANDARD2_0
            try
            {
                var req = WebRequest.Create(url) as HttpWebRequest;
                //req.KeepAlive = true;
                //req.KeepAlive = false;
                req.AllowWriteStreamBuffering = false;
                req.Method = "POST";
                req.ContentType = contentType;
                
                TransportSendStats sendededStats = null;
                using (Stream requestStream = req.GetRequestStream())
                {
                    sendededStats = this.WriteMessageToBody(formDataBoundary, requestStream, packetInfos, configurationsToDownloadInfos, context);
                }

                try
                {
                    var response = req.GetResponse();
                    using (var resStream = response.GetResponseStream())
                    using (var streamReader = new StreamReader(resStream))
                    {
                        // обрабатывает ответ
                        return this.SaveRequestResults(resStream, sendededStats);
                    }
                }
                catch (WebException ex)
                {
                    var wRespStatusCode = ((HttpWebResponse)ex.Response).StatusCode;
                    if ((int)wRespStatusCode == 429)
                    {
                        var timeout = int.TryParse(ex.Response.Headers.GetValues("Retry-After").FirstOrDefault(), out var retryTm)
                            ? retryTm
                            : Convert.ToInt64(settings.ErrorRetryTimeout.TotalMilliseconds);

                        return SendResult.Retry(timeout);
                    }

                    // при общих ошибках
                    return SendResult.Retry(settings.ServerErrorRetryTimeout);
                }
            }
            catch (Exception)
            {
                // при общих ошибках
                return SendResult.Retry(settings.ErrorRetryTimeout);
            }
#else
            try
            {
                TransportSendStats sendededStats = null;
                var reqContent = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new WriteToStreamContent((requestStream, ctx) =>
                    {
                        sendededStats = this.WriteMessageToBody(formDataBoundary, requestStream, packetInfos, configurationsToDownloadInfos, context);
                    }),
                };

                // workaround for net core 2.2 and analogs
                // нужно для того чтобы в случаях "отказов" не происходило чтение всего тела запроса, а читались только основные параметры
                reqContent.Headers.ExpectContinue = true;
                reqContent.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                var result = this.httpClient.SendAsync(reqContent, HttpCompletionOption.ResponseContentRead).Result;

                if (result.IsSuccessStatusCode)
                {
                    using (var stream = result.Content.ReadAsStreamAsync().Result)
                    {
                        // обрабатывает ответ
                        return this.SaveRequestResults(stream, sendededStats);
                    };
                }
                else
                {
                    if ((int)result.StatusCode == 429)
                    {
                        var timeout = int.TryParse(result.Headers.GetValues("Retry-After").FirstOrDefault(), out var retryTm)
                                        ? retryTm
                                        : Convert.ToInt64(settings.ErrorRetryTimeout.TotalMilliseconds);

                        return SendResult.Retry(timeout);
                    }

                    return SendResult.Retry(settings.ServerErrorRetryTimeout);
                }
            }
            catch (Exception)
            {
                // при общих ошибках
                return SendResult.Retry(settings.ErrorRetryTimeout);
            }
#endif
        }

        protected virtual SendResult SaveRequestResults(Stream responseStream, TransportSendStats sendStats)
        {
            bool needRetry = false;
            TransportResponseStats responseStats;
            using (var streamReader = new StreamReader(responseStream))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                var jsonSerializer = new JsonSerializer();
                // сохраним потоки данных конфигураций при десериализации
                jsonSerializer
                    .Converters
                    .Add(new ConfigurationsByteArraysToFilesConverter(this.configurationStore));

                var response = jsonSerializer.Deserialize<TransportResponse>(jsonReader);

                this.sendStateStore.ConfigurationsUpdated();

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
                    // изменилась бд системы (например сменили сервера)
                    if (response.DbTokenData != null)
                    {
                        this.agentInfoService.SetDbToken(new TransportDbTokenData
                        {
                            DbToken = response.DbTokenData.DbToken
                        });

                        this.packetManager.RemoveAll();

                        //return SendResult.Retry(TransportConstants.DefaultUpdateDbTokenTimeout);
                    }

                    responseStats = new TransportResponseStats()
                    {
                        SendedPacketsStats = sendStats.SendedPacketsStats,
                        // изменилась бд системы
                        TransferedPacketsProcessingResults = response.DbTokenData != null ? new List<TransferedPacketStats>() : sendStats.SendedPacketsStats.Select(x =>
                        {
                            return new TransferedPacketStats
                            {
                                PacketInfo = x.Key,
                                SendStats = x.Value,
                                Result = response.TransferedPackets.First(p => p.PacketId == x.Key.Id && p.ProviderKey == x.Key.ProviderKey).Result,
                            };
                        }).ToList(),
                    };
                }
            }

            var packets = responseStats.TransferedPacketsProcessingResults.ToList();
            foreach (var item in packets)
            {
                var packet = item.PacketInfo;
                if (item.Result == PacketProcessingResult.Saved)
                {
                    this.packetManager.SaveSendStats(packet.ProviderKey, packet.Id, item.SendStats);
                }
                else if (item.Result == PacketProcessingResult.Error)
                {
                    needRetry = true;
                }
                else if (item.Result == PacketProcessingResult.Resend)
                {
                    // сбрасываем статистику
                    this.packetManager.SaveSendStats(packet.ProviderKey, packet.Id, new SendStats());
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
                    this.packetManager.SaveSendStats(packet.ProviderKey, packet.Id, item.SendStats);
                }
            }

            //var configurations = stats.Configurations.ToList();
            //foreach (var item in configurations)
            //{
            //}

            return needRetry
                ? SendResult.Retry(settings.ErrorRetryTimeout)
                : SendResult.Success();
        }
    }

    public class SendStats
    {
        public int TransferedBytes { get; set; }
        public bool TransferCompleted { get; set; }
    }
}
