using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Coral.Atoll.Utils
{
    /// <summary>
    /// Контракт лога.
    /// </summary>
    public interface ILog
    {

        /// <summary>
        /// Получить признак, что трассировочные сообщения включены.
        /// </summary>
        bool IsTraceEnabled { get; }

        /// <summary>
        /// Выполнить запись информационного сообщения.
        /// </summary>
        void LogMessage(string message);

        /// <summary>
        /// Выполнить запись трассировочного сообщения.
        /// </summary>
        void LogTraceMessage(string message);

        /// <summary>
        /// Выполнить запись трассировочного сообщения.
        /// </summary>
        void LogTraceMessage(string message, Exception ex);

        /// <summary>
        /// Выполнить запись предупредительного сообщения.
        /// </summary>
        void LogWarning(string message);

        /// <summary>
        /// Выполнить запись предупредительного сообщения.
        /// </summary>
        void LogWarning(string message, Exception ex);

        /// <summary>
        /// Выполнить запись сообщения об ошибке.
        /// </summary>
        void LogError(string message);

        /// <summary>
        /// Выполнить запись сообщения об ошибке.
        /// </summary>
        void LogError(string message, Exception ex);

    }
    
    /// <summary>
    /// Реализация пустого лога.
    /// </summary>
    public sealed class EmptyLog : ILog
    {

        /// <summary>
        /// Синглетон-экземпляр пустого лога.
        /// </summary>
        public static readonly EmptyLog Instance = new EmptyLog();

        /// <summary>
        /// Приватный конструктор.
        /// </summary>
        private EmptyLog() { }

        bool ILog.IsTraceEnabled { get { return false; } }

        void ILog.LogMessage(string message) { }

        void ILog.LogTraceMessage(string message) { }

        void ILog.LogTraceMessage(string message, Exception ex) { }

        void ILog.LogWarning(string message) { }

        void ILog.LogWarning(string message, Exception ex) { }

        void ILog.LogError(string message) { }

        void ILog.LogError(string message, Exception ex) { }

    }

    public static class FileSystemHelper
    {
        private static int DefaultAdditionalTryCount = 5;
        private static int DefaultWaitTryTimeout = 200;

        /// <summary>
        /// Выполнить безопасное перемещение файла.
        /// </summary>
        public static bool SafeMoveFile(string fromPath, string toPath, CancellationToken token = default(CancellationToken), ILog log = null)
        {
            log = log ?? EmptyLog.Instance;
            // Выполняем первую попытку.
            try
            {
                File.Move(fromPath, toPath);
                return true;
            }
            catch
            {
                // ignored
            }

            Exception lastException = null;

            for (var i = 0; i < DefaultAdditionalTryCount; i++)
            {
                // Делаем паузу перед повторной попыткой.
                if (token.IsCancellationRequested || token.WaitHandle.WaitOne(DefaultWaitTryTimeout))
                {
                    #region Instrumentation

                    log.LogTraceMessage(string.Concat(
                        "File move from \"", fromPath,
                        "\" to \"", toPath,
                        "\" cancelled by cancellation token."));

                    #endregion

                    return false;
                }

                // Выполняем повторную попытку.
                try
                {
                    File.Move(fromPath, toPath);
                    return true;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            #region Instrumentation

            log.LogTraceMessage(
                   string.Concat(
                       "Failed to move file \"", fromPath,
                       "\" to \"", toPath,
                       "\" after ", DefaultAdditionalTryCount + 1, " attempts."),
                   lastException);

            #endregion

            return false;
        }

        /// <summary>
        /// Выполнить безопасное удаление удаление файла.
        /// </summary>
        public static bool SafeDeleteFile(string path, CancellationToken token = default(CancellationToken), ILog log = null)
        {
            log = log ?? EmptyLog.Instance;

            // Выполняем первую попытку.
            try
            {
                if (!File.Exists(path))
                    return true;

                File.Delete(path);
                return true;
            }
            catch
            {
                // ignored
            }

            Exception lastException = null;

            for (var i = 0; i < DefaultAdditionalTryCount; i++)
            {
                // Делаем паузу перед повторной попыткой.
                if (token.IsCancellationRequested || token.WaitHandle.WaitOne(DefaultWaitTryTimeout))
                {
                    #region Instrumentation

                    log.LogTraceMessage(string.Concat(
                        "File delete at \"", path,
                        "\" cancelled by cancellation token."));

                    #endregion

                    return false;
                }

                // Проверяем, что файл еще существует.
                if (!File.Exists(path)) return true;

                // Снимаем все атрибуты с файла перед повторной попыткой удаления.
                try
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                }
                catch
                {
                    // ignored
                }

                // Выполняем повторную попытку.
                try
                {
                    File.Delete(path);

                    if (!File.Exists(path))
                        return true;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            #region Instrumentation

            log.LogTraceMessage(
                string.Concat(
                    "Failed to delete file at \"", path,
                    "\" after ", DefaultAdditionalTryCount + 1, " attempts."),
                lastException);

            #endregion

            return false;
        }
        
        /// <summary>
        /// Выполнить безопасное удаление папки.
        /// </summary>
        public static bool SafeDeleteDirectory(string path, CancellationToken token = default(CancellationToken), ILog log = null)
        {
            log = log ?? EmptyLog.Instance;

            // Выполняем первую попытку.
            try
            {
                if (!Directory.Exists(path))
                    return true;

                Directory.Delete(path, true);
                if (!Directory.Exists(path)) return true;
            }
            catch
            {
                // ignored
            }

            Exception lastException = null;

            for (var i = 0; i < DefaultAdditionalTryCount; i++)
            {
                // Делаем паузу перед повторной попыткой.
                if (token.IsCancellationRequested || token.WaitHandle.WaitOne(DefaultWaitTryTimeout))
                {
                    #region Instrumentation

                    log.LogTraceMessage(string.Concat(
                        "Directory delete at \"", path,
                        "\" cancelled by cancellation token."));

                    #endregion

                    return false;
                }

                // Проверяем, что папка еще существует.
                if (!Directory.Exists(path)) return true;

                // Пытаемся снять все атрибуты с файлов в удаляемой папке.
                try
                {
                    foreach (var filePath in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                    {
                        if (token.IsCancellationRequested) return false;

                        try
                        {
                            File.SetAttributes(filePath, FileAttributes.Normal);
                        }
                        catch
                        {
                            // ignored
                        }

                        try
                        {
                            File.Delete(filePath);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
                catch
                {
                    // ignored
                }

                // Выполняем повторную попытку.
                try
                {
                    Directory.Delete(path, true);
                    if (!Directory.Exists(path)) return true;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            #region Instrumentation

            log.LogTraceMessage(
                string.Concat(
                    "Failed to delete directory at \"", path,
                    "\" after ", DefaultAdditionalTryCount + 1, " attempts."),
                lastException);

            #endregion

            return false;
        }

        /// <summary>
        /// Выполнить безопасное создание родительской папки.
        /// </summary>
        public static bool SafeEnsureParentDirectory(string path, CancellationToken token = default(CancellationToken), ILog log = null)
        {
            return SafeCreateDirectory(Path.GetDirectoryName(path), token, log);
        }

        /// <summary>
        /// Выполнить безопасное создание папки.
        /// </summary>
        public static bool SafeCreateDirectory(string path, CancellationToken token = default(CancellationToken), ILog log = null)
        {
            // Выполняем первую попытку.
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch
            {
                // ignored
            }

            Exception lastException = null;

            for (var i = 0; i < DefaultAdditionalTryCount; i++)
            {
                // Делаем паузу перед повторной попыткой.
                if (token.IsCancellationRequested || token.WaitHandle.WaitOne(DefaultWaitTryTimeout))
                {
                    #region Instrumentation

                    log.LogTraceMessage(string.Concat(
                        "Directory creation at \"", path,
                        "\" cancelled by cancellation token."));

                    #endregion

                    return false;
                }

                // Проверяем, что директория еще не существует.
                if (Directory.Exists(path)) return true;

                // Выполняем повторную попытку.
                try
                {
                    Directory.CreateDirectory(path);
                    return true;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            #region Instrumentation

            log.LogTraceMessage(
                string.Concat(
                    "Failed to create directory at \"", path,
                    "\" after ", DefaultAdditionalTryCount + 1, " attempts."),
                lastException);

            #endregion

            return false;
        }

        public static bool SafeEmptyDirectory(string tempDir, CancellationToken token = default(CancellationToken), ILog log = null)
        {
            log = log ?? EmptyLog.Instance;

            if (!Directory.Exists(tempDir))
            {
                return true;
            }

            var di = new DirectoryInfo(tempDir);
            foreach (FileInfo file in di.EnumerateFiles())
            {
                if (!SafeDeleteFile(file.FullName, token, log))
                {
                    return false;
                }
            }

            foreach (DirectoryInfo dir in di.EnumerateDirectories())
            {
                if (!SafeDeleteDirectory(dir.FullName, token, log))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryExecuteInLoop(Action action, string operationName, CancellationToken token, ILog log)
        {
            // проверим перед исполнением
            if (token.IsCancellationRequested)
            {
                #region Instrumentation

                log.LogTraceMessage(string.Concat(operationName, " cancelled by cancellation token."));

                #endregion

                return false;
            }

            try
            {
                action();
                return true;
            }
            catch (Exception e)
            {

            }

            Exception lastException = null;
            for (int i = 0; i < DefaultAdditionalTryCount; i++)
            {
                // Делаем паузу перед повторной попыткой.
                if (token.IsCancellationRequested || token.WaitHandle.WaitOne(DefaultWaitTryTimeout))
                {
                    #region Instrumentation

                    log.LogTraceMessage(string.Concat(operationName, " cancelled by cancellation token."));

                    #endregion

                    return false;
                }

                // Выполняем повторную попытку.
                try
                {
                    action();
                    return true;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }


            log.LogError(
                string.Concat(operationName,
                    " failed after ", DefaultAdditionalTryCount + 1, " attempts."),
                lastException);

            return false;
        }

        /// <summary>
        /// Выполнить записывание в файл в несколько попыток.
        /// </summary>
        public static void WriteAllText(string filePath, string content, Encoding encoding = null, CancellationToken token = default(CancellationToken))
        {
            ExecuteInLoop(() =>
            {
                if (encoding == null)
                {
                    File.WriteAllText(filePath, content);
                }
                else
                {
                    File.WriteAllText(filePath, content, encoding);
                }
            }, token);
        }

        /// <summary>
        /// Выполнить записывание в файл в несколько попыток.
        /// </summary>
        public static string ReadAllText(string filePath, Encoding encoding = null, CancellationToken token = default(CancellationToken))
        {
            return ExecuteInLoop(() =>
            {
                if (encoding == null)
                {
                    return File.ReadAllText(filePath);
                }
                else
                {
                    return File.ReadAllText(filePath, encoding);
                }
            }, token);
        }

        public static void CopyFile(string source, string dest, bool overwrite = false, CancellationToken token = default(CancellationToken))
        {
            ExecuteInLoop(() => File.Copy(source, dest, overwrite), token);
        }

        public static void MoveFile(string source, string dest, CancellationToken token = default(CancellationToken))
        {
            ExecuteInLoop(() => File.Move(source, dest), token);
        }

        public static void DeleteFile(string path, CancellationToken token = default(CancellationToken))
        {
            ExecuteInLoop(() => {
                // Проверяем, что файл еще существует.
                if (!File.Exists(path))
                    return;

                // Снимаем все атрибуты с файла перед повторной попыткой удаления.
                try
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                }
                catch
                {
                    // ignored
                }

                // Выполняем повторную попытку.
                File.Delete(path);
            }, token);
        }

        private static void ExecuteInLoop(Action action, CancellationToken token)
        {
            // проверим перед исполнением
            token.ThrowIfCancellationRequested();

            try
            {
                action();
                return;
            }
            catch (Exception e)
            {

            }

            Exception lastException = null;
            for (int i = 0; i < DefaultAdditionalTryCount; i++)
            {
                // Делаем паузу перед повторной попыткой.
                if (token.IsCancellationRequested || token.WaitHandle.WaitOne(DefaultWaitTryTimeout))
                {
                    token.ThrowIfCancellationRequested();
                    return;
                }

                // Выполняем повторную попытку.
                try
                {
                    action();
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            if (lastException != null)
            {
                throw lastException;
            }
        }

        private static T ExecuteInLoop<T>(Func<T> action, CancellationToken token)
        {
            // проверим перед исполнением
            token.ThrowIfCancellationRequested();

            try
            {
                return action();
            }
            catch (Exception e)
            {

            }

            Exception lastException = null;
            for (int i = 0; i < DefaultAdditionalTryCount; i++)
            {
                // Делаем паузу перед повторной попыткой.
                if (token.IsCancellationRequested || token.WaitHandle.WaitOne(DefaultWaitTryTimeout))
                {
                    token.ThrowIfCancellationRequested();
                    return default(T);
                }

                // Выполняем повторную попытку.
                try
                {
                    return action();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            if (lastException != null)
            {
                throw lastException;
            }

            throw new InvalidOperationException("incorrect execution");
        }
    }
}
