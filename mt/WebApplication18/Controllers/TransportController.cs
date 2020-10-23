using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Atoll.Transport;
using Microsoft.AspNetCore.Mvc;
using WebApplication18.Configuration;
using WebApplication18.Transport;

namespace WebApplication18.Controllers
{
    /// <remarks>
    /// стоит переделать на middleware, т.к. будет работать чуточку быстрее
    /// </remarks>
    [Route("dhu/transport")]
    public class TransportController : Controller
    {
        private readonly IPacketsStore packetsStore;
        private readonly ITransportRequestService packageRequestParser;
        private readonly IAgentStaticConfigService agentConfigService;
        private readonly IDbTokenService dbTokenService;
        private readonly IThrottleQueueManager queueManager;
        private readonly IAgentConfigurationsService agentConfigurationsHandler;

        public TransportController(
            IPacketsStore packetsStore,
            ITransportRequestService packageRequestParser,
            IThrottleQueueManager queueManager,
            IAgentStaticConfigService agentConfigService,
            IDbTokenService dbTokenService,
            IAgentConfigurationsService agentConfigurationsHandler)
        {
            this.packetsStore = packetsStore;
            this.packageRequestParser = packageRequestParser;
            this.queueManager = queueManager;
            this.agentConfigService = agentConfigService;
            this.dbTokenService = dbTokenService;
            this.agentConfigurationsHandler = agentConfigurationsHandler;
        }

        private ConfigData GetCurrentConfigData(MessageHeaders headers)
        {
            // проверка конфигурации
            var configData = agentConfigService.GetConfigData() ?? throw new InvalidOperationException("Отсутствует конфигурация агента");

            if (headers.ConfigToken != configData.ConfigToken)
            {
                if (headers.ConfigVersion > configData.ConfigVersion)
                {
                    agentConfigService.ReloadConfig();
                    configData = agentConfigService.GetConfigData() ?? throw new InvalidOperationException("Отсутствует конфигурация агента");
                    if (headers.ConfigVersion > configData.ConfigVersion)
                    {
                        //понижение конфига ?
                    }
                    //if (headers.ConfigToken != configData.ConfigToken)
                    //{
                    //    lazyResponse.Value.SetConfigChanged(configData);
                    //}
                }
                //else
                //{
                //    lazyResponse.Value.SetConfigChanged(configData);
                //}
            }

            return configData;
        }

        private ProcessingContext GetProcessingContext(MessageHeaders headers, ConfigData configData)
        {
            // проверка токена бд
            var dbTokenData = dbTokenService.GetDbTokenData() ?? throw new InvalidOperationException("Отсутствует токен базы данных");

            if (headers.DbToken != dbTokenData.DbToken)
            {
                // стоит переделать?
                if (string.Compare(headers.DbToken, dbTokenData.DbToken) > -1)
                {
                    dbTokenService.ReloadToken();
                    dbTokenData = dbTokenService.GetDbTokenData() ?? throw new InvalidOperationException("Отсутствует токен базы данных");
                }

                //lazyResponse.Value.SetDbTokenData(dbTokenData);
            }

            return new ProcessingContext
            {
                CurrentConfigData = configData,
                CurrentDbTokenData = dbTokenData,
                MessageHeaders = headers,
            };
        }

        private TransportResponse CheckStaticConfigAndDbTokenResponseOrNull(ProcessingContext ctx)
        {
            var lazyResponse = new Lazy<TransportResponse>(() => new TransportResponse(), LazyThreadSafetyMode.None);
            var configData = ctx.CurrentConfigData;
            var headers = ctx.MessageHeaders;
            var dbTokenData = ctx.CurrentDbTokenData;

            if (headers.ConfigToken != configData.ConfigToken)
            {
                lazyResponse.Value.SetStaticConfigChanged(configData);
            }

            if (headers.DbToken != dbTokenData.DbToken)
            {
                lazyResponse.Value.SetDbTokenData(dbTokenData);
            }

            if (lazyResponse.IsValueCreated)
            {
                return lazyResponse.Value;
            }

            return null;
        }

        private async Task<IActionResult> ProcessMessage(ProcessingContext ctx)
        {
            var response = new TransportResponse();
            var headers = ctx.MessageHeaders;
            var dbTokenData = ctx.CurrentDbTokenData;

            IList<TransferedPacketResponse> responses = new List<TransferedPacketResponse>();
            // при изменении токена надо конфиги по-новой
            IReadOnlyCollection<ConfigurationRequestDataItem> configurationsOnAgent = new List<ConfigurationRequestDataItem>();
            if (headers.DbToken != dbTokenData.DbToken)
            {
                // TODO удалять пакеты для старого токена
                response.SetDbTokenData(dbTokenData);
            }
            else
            {
                // сохраняем пакеты
                var savePacketsResult = await this.packageRequestParser.ParseBodyAndSavePackets(headers, this.Request);
                configurationsOnAgent = savePacketsResult.ConfigurationsStats;
                response.SetPacketResponses(savePacketsResult.TransferedPackets);
            }

            // добавляем "динамическую" конфигурацию - конфиг аннотаций, развёртываний и тп
            var configurations = this.agentConfigurationsHandler
                                     .GetConfigurationResponses(headers, configurationsOnAgent);

            // для dispose-а потоков конфигураций по окончании запроса
            foreach (var conf in configurations)
            {
                this.HttpContext.Response.RegisterForDispose(conf.Stream);
            }

            response.SetConfigurations(configurations);

            return this.Ok(response);
        }

        [HttpPost("exchange")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Exchange()
        {
            // получаем информацию заголовков (по ним мы как правило можем определить нужно ли получать тело)
            var messageHeaders = packageRequestParser.GetHeaders(this.Request);

            // вначале получим и проверим статическую конфигурацию
            // TODO при перезагрузке конфига и токена не возникнут ли зависоны??
            var configData = this.GetCurrentConfigData(messageHeaders);

            // если поменялась "статическая" конфигурация, то не принимаем информацию, а просим агента сходить за новой и применить её
            // статическую конфигурацию обрабатываем отдельно, чтобы была возможность отключить часть функционала на агенте, без отрабатывания сообщений с агента
            if (messageHeaders.ConfigToken != configData.ConfigToken)
            {
                return this.Ok(new TransportResponse().SetStaticConfigChanged(configData));
            }

            // ид для ограничивающей очереди сообщений с агентов
            var queueId = messageHeaders.GetAgentId();
            // нужно прийти позже (или нужно добавить в обработку)
            if (!this.queueManager.TryAccept(queueId, new ThrottleParams
            {
                Attempt = messageHeaders.Attempt,
            }))
            {
                // http status code 429 - The user has sent too many requests in a given amount of time ("rate limiting").
                var retryAfterMs = 36 * 1000;
                this.Response.Headers.Add("Retry-After", retryAfterMs.ToString());                
                return this.StatusCode(429);
            }

            try
            {
                // TODO при перезагрузке токена не возникнут ли зависоны??
                var processingCtx = this.GetProcessingContext(messageHeaders, configData);

                // обрабатываем
                return await this.ProcessMessage(processingCtx);
            }
            finally
            {
                this.queueManager.Release(queueId);
            }
        }

        [Route("health-check")]
        [HttpGet]
        public async Task<IActionResult> HealthCheck()
        {
            if (await this.packetsStore.CheckIfHasReservationsAsync(this.HttpContext.RequestAborted) && await this.packetsStore.PingAsync(this.HttpContext.RequestAborted))
                return this.StatusCode((int)HttpStatusCode.OK);

            //DhuWebRecieverProfilingUnit.Unit.ReadyCheck((int)HttpStatusCode.ServiceUnavailable);
            return this.StatusCode((int)HttpStatusCode.ServiceUnavailable);
        }


        [Route("health-check-ui")]
        [HttpGet]
        public async Task<IActionResult> HealthCheckUi()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"<html><body><ul>");
            stringBuilder.AppendLine($"<li>Время на сервере DHU: { DateTime.UtcNow }.");

            if (await this.packetsStore.CheckIfHasReservationsAsync(this.HttpContext.RequestAborted))
            {
                stringBuilder.AppendLine("<li>Хранилище событий: зарезервировано.");
            }
            else
            {
                stringBuilder.AppendLine("<li>Хранилище событий: НЕ зарезервировано.");
            }

            stringBuilder.AppendLine
                (
                    await this.packetsStore.PingAsync(this.HttpContext.RequestAborted)
                        ? "<li>Доступ к базе данных: база данных доступна."
                        : "<li>Доступ к базе данных: база данных не доступна."
                );

            stringBuilder.AppendLine($"</ul></body></html>");

            return this.Content(stringBuilder.ToString(), "text/html", Encoding.UTF8);
        }
    }
}
