using System;
using System.Collections.Generic;
using System.Linq;
using Atoll.Transport.Client.Contract;
using Coral.Atoll.Utils;

namespace Atoll.Transport.Client.Bundle
{
    /// <summary>
    /// загловки для сообщения отправляемого на сервер
    /// </summary>
    /// <remarks></remarks>
    public class SendMessageHeaders
    {
        //
        public string DomainName { get; set; }
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

            if (!string.IsNullOrEmpty(this.DomainName))
            {
                paramsDict.Add(nameof(this.DomainName), this.DomainName);
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

        //public static SendMessageHeaders FromDictionary(IDictionary<string, string> dict)
        //{
        //    var headers = new SendMessageHeaders();
        //    headers.DomainName = dict.GetOrDefault(nameof(headers.DomainName));
        //    headers.ComputerName = dict.GetOrDefault(nameof(headers.ComputerName));
        //    headers.DbToken = dict.GetOrDefault(nameof(headers.DbToken));
        //    headers.ConfigToken = dict.GetOrDefault(nameof(headers.ConfigToken));
        //    headers.OrganizationUnit = dict.GetOrDefault(nameof(headers.OrganizationUnit));
        //    headers.ConfigVersion = dict.GetOrDefault(nameof(headers.ConfigVersion), int.Parse);
        //    headers.Attempt = dict.GetOrDefault(nameof(headers.Attempt), int.Parse);
        //    headers.Hints = dict.GetOrDefault(nameof(headers.Attempt), GetHints);

        //    return headers;
        //}

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
}
