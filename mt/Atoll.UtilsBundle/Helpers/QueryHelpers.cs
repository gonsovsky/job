using System;
using System.Collections.Generic;
using System.Text;
#if NETSTANDARD2_0
using System.Net.Http;
#endif

namespace Coral.Atoll.Utils
{
    /// <summary>
    /// класс хелпер для uri и query
    /// </summary>
    public static class QueryHelpers
    {
        public static string AddQueryString(
            string uri,
            IEnumerable<KeyValuePair<string, string>> queryString)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (queryString == null)
            {
                throw new ArgumentNullException(nameof(queryString));
            }

            var anchorIndex = uri.IndexOf('#');
            var uriToBeAppended = uri;
            var anchorText = "";
            // If there is an anchor, then the query string must be inserted before its first occurrence.
            if (anchorIndex != -1)
            {
                anchorText = uri.Substring(anchorIndex);
                uriToBeAppended = uri.Substring(0, anchorIndex);
            }

            var queryIndex = uriToBeAppended.IndexOf('?');
            var hasQuery = queryIndex != -1;

            var sb = new StringBuilder();
            sb.Append(uriToBeAppended);
            foreach (var parameter in queryString)
            {
                if (parameter.Value == null)
                {
                    continue;
                }

                sb.Append(hasQuery ? '&' : '?');
                sb.Append(Uri.EscapeDataString(parameter.Key));
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(parameter.Value));
                hasQuery = true;
            }

            sb.Append(anchorText);
            return sb.ToString();
        }
    }
}
