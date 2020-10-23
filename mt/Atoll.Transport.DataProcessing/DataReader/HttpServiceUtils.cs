using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace Atoll.Transport.DataProcessing
{
    internal static class HttpServiceUtils
    {

        public static void ProcessWebException(WebException webx)
        {
            if (webx.Response != null)
            {
                var exStream = webx.Response.GetResponseStream();
                if (exStream != null)
                {
                    var readData = ReadResponseStreamBytes(exStream);
                    var responseSetring = Encoding.UTF8.GetString(readData);
                    Trace.TraceError(responseSetring);
                }
            }
        }

        public static byte[] ReadResponseStreamBytes(Stream stream)
        {
            byte[] readData;
            using (var memoryStream = new MemoryStream())
            {
                var readBuffer = new byte[64];
                int actuallyRead;
                while ((actuallyRead = stream.Read(readBuffer, 0, readBuffer.Length)) > 0)
                    memoryStream.Write(readBuffer, 0, actuallyRead);

                readData = memoryStream.ToArray();
            }
            return readData;
        }

    }
}
