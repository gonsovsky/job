//using Microsoft.Extensions.Primitives;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Primitives;
using System.Net.Http.Headers;

namespace Atoll.Transport.DataHub
{
    public static class HeaderUtilities
    {
        public static string RemoveQuotes(string input)
        {
            if (IsQuoted(input))
            {
                input = input.Substring(1, input.Length - 2);
            }
            return input;
        }

        public static bool IsQuoted(string input)
        {
            return !string.IsNullOrEmpty(input) && input.Length >= 2 && input[0] == '"' && input[input.Length - 1] == '"';
        }

        public static StringSegment RemoveQuotes(StringSegment input)
        {
            if (IsQuoted(input))
            {
                input = input.Subsegment(1, input.Length - 2);
            }
            return input;
        }

        public static bool IsQuoted(StringSegment input)
        {
            return !StringSegment.IsNullOrEmpty(input) && input.Length >= 2 && input[0] == '"' && input[input.Length - 1] == '"';
        }

        private const string BoundaryString = "boundary";
        // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
        // The spec says 70 characters is a reasonable limit.
        public static string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit)
        {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Parameters.FirstOrDefault(x => x.Name == BoundaryString)?.Value);
            if (string.IsNullOrEmpty(boundary))
            {
                throw new InvalidDataException("Missing content-type boundary.");
            }

            if (boundary.Length > lengthLimit)
            {
                throw new InvalidDataException($"Multipart boundary length limit {lengthLimit} exceeded.");
            }

            return boundary.ToString();
        }

        public static string GetBoundary(Microsoft.Net.Http.Headers.MediaTypeHeaderValue contentType, int lengthLimit)
        {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Parameters.FirstOrDefault(x => x.Name == BoundaryString)?.Value ?? default(StringSegment));

            if (StringSegment.IsNullOrEmpty(boundary))
            {
                throw new InvalidDataException("Missing content-type boundary.");
            }

            if (boundary.Length > lengthLimit)
            {
                throw new InvalidDataException($"Multipart boundary length limit {lengthLimit} exceeded.");
            }

            return boundary.ToString();
        }
    }

}
