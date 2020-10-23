using System;
using System.IO;
using System.Text;
#if NETSTANDARD2_0
#endif

namespace Coral.Atoll.Utils
{
    public static class AtomicFileWriteHelper
    {
        // file atomic write https://blogs.msdn.microsoft.com/adioltean/2005/12/28/how-to-do-atomic-writes-in-a-file/
        public static void WriteToFile(string filePath, Stream stream, AtomicWriteTempFileOptions options = null)
        {
            options = options ?? new AtomicWriteTempFileOptions();
            string tempFilePath = (options.Destination == TempFileDestination.Random
                ? Path.GetTempFileName()
                : filePath)
                    + options.TempFileSuffix;

            // delete any existing
            if (File.Exists(tempFilePath))
                FileSystemHelper.DeleteFile(tempFilePath);

            using (FileStream tempFile = File.OpenWrite(tempFilePath))
            {
                stream.CopyTo(tempFile);
                tempFile.Flush(true);
            }

            try
            {
                // delete any existing
                if (File.Exists(filePath))
                    FileSystemHelper.DeleteFile(filePath);

                FileSystemHelper.MoveFile(tempFilePath, filePath);
            }
            catch (Exception ex)
            {
                try
                {
                    FileSystemHelper.DeleteFile(tempFilePath);
                }
                catch (Exception)
                {
                    //throw;
                }

                throw;
            }
        }

        public static void WriteToFile(string filePath, string content, Encoding encoding = null, AtomicWriteTempFileOptions options = null)
        {
            options = options ?? new AtomicWriteTempFileOptions();
            string tempFilePath = (options.Destination == TempFileDestination.Random
                ? Path.GetTempFileName()
                : filePath)
                    + options.TempFileSuffix;

            // delete any existing
            if (File.Exists(tempFilePath))
                FileSystemHelper.DeleteFile(tempFilePath);

            using (FileStream tempFile = File.Open(tempFilePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (var nonCLosingStream = new NonClosingStreamWrapper(tempFile))
                using (var sw = encoding != null ? new StreamWriter(nonCLosingStream, encoding) : new StreamWriter(nonCLosingStream))
                {
                    sw.Write(content);
                }

                tempFile.Flush(true);
            }

            try
            {
                // delete any existing
                if (File.Exists(filePath))
                    FileSystemHelper.DeleteFile(filePath);

                FileSystemHelper.MoveFile(tempFilePath, filePath);
            }
            catch (Exception ex)
            {
                try
                {
                    FileSystemHelper.SafeDeleteFile(tempFilePath);
                }
                catch (Exception)
                {
                    //throw;
                }

                throw;
            }
        }
    }
}
