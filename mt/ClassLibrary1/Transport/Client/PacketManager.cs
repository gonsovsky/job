using ClassLibrary1.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace ClassLibrary1.Transport
{
    public class PacketManagerSettings
    {
        public string PacketsDirectory { get; set; }
        public string PacketsTempDirectory { get; set; }

        public PacketManagerSettings(string packetsDirectory, string packetsTempDirectory)
        {
            this.PacketsDirectory = packetsDirectory;
            this.PacketsTempDirectory = packetsTempDirectory;
        }
    }

    /// <summary>
    /// <see cref="IPacketManager"/>
    /// </summary>
    public class PacketManager : IPacketManager
    {
        private string packetsDirectory;
        private string packetsTempDirectory;
        private string packetExt;
        private string packetSearchMask;
        private string packetStatsExt;

        public PacketManager(PacketManagerSettings settings)
        {
            this.packetsDirectory = settings.PacketsDirectory;
            this.packetsTempDirectory = settings.PacketsTempDirectory;
            Directory.CreateDirectory(this.packetsDirectory);
            Directory.CreateDirectory(this.packetsTempDirectory);

            this.packetExt = ".pkt";
            this.packetSearchMask = "*" + this.packetExt;

            this.packetStatsExt = ".stats";
        }

        /// <summary>
        /// использую для запрета одновременной записи/чтения пакетов (например чтени при отправке и удаление файлов при коммите который перезаписывает данные)
        /// </summary>
        private ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim();

        public void Commit(string providerKey, IEnumerable<ITransportPacket> packets, CommitOptions options)
        {
            this.RunInWriteLock(() =>
            {
                if (options == CommitOptions.DeletePrevious)
                {
                    var providerStoreDir = this.GetProviderStoreDir(providerKey);
                    if (!FileSystemHelper.SafeEmptyDirectory(providerStoreDir))
                    {
                        throw new InvalidOperationException("Can't empty directory");
                    }
                }

                foreach (var packet in packets)
                {
                    packet.Save(providerKey, this);
                }
            });
        }

        public virtual void SavePacket(string providerKey, string packetId, Stream stream)
        {
            var filePath = Path.Combine(this.GetProviderStoreDir(providerKey), packetId + this.packetExt);
            AtomicFileWriteHelper.WriteToFile(filePath, stream);
        }

        private string GetProviderTempDir(string providerKey)
        {
            return Path.Combine(this.packetsTempDirectory, providerKey);
        }

        private string GetProviderStoreDir(string providerKey)
        {
            return Path.Combine(this.packetsDirectory, providerKey);
        }

        //private long counter;
        // TODO нужен ли отдельный упорядоченный! уникальный идентификатор (Guid comb алгоритм генерирует упорядоченые guid-ы)
        // TODO нужен упорядоченный идентификатор 
        protected virtual string GetPacketId(string providerKey)
        {
            //var counter = Interlocked.Increment(ref this.counter);
            // https://stackoverflow.com/questions/1752004/sequential-guid-generator
            // https://stackoverflow.com/questions/29674395/how-to-sort-sequential-guids-in-c
            // https://stackoverflow.com/questions/12252551/generate-a-sequential-guid-by-myself
            // упорядоченные guid-ы
            return SeqIdGenerator.GenerateGuidComb().ToString("D");
        }

        public PacketCreationResult Create(string providerKey)
        {
            Stream stream = null;
            try
            {
                var packetId = this.GetPacketId(providerKey);
                var providerDir = this.GetProviderStoreDir(providerKey);
                var providerTempDir = this.GetProviderTempDir(providerKey);
                Directory.CreateDirectory(providerDir);
                Directory.CreateDirectory(providerTempDir);

                var tempFilePath = Path.Combine(providerTempDir, packetId.ToString() + this.packetExt);
                var filePath = Path.Combine(this.GetProviderStoreDir(providerKey), packetId.ToString() + this.packetExt);
                stream = File.Open(tempFilePath, FileMode.Create, FileAccess.ReadWrite);

                return new PacketCreationResult
                {
                    Packet = new TransportPacket(packetId, stream, () =>
                    {
                        stream.Dispose();
                        if (!FileSystemHelper.SafeMoveFile(tempFilePath, filePath))
                        {
                            throw new InvalidOperationException("Can't move file");
                        }
                    }),
                    Resources = new[] { (IDisposable)stream }
                };
            }
            catch (Exception)
            {
                stream?.Dispose();
                throw;
            }
        }

        public IEnumerable<ITransportPacketInfo> GetTransportPacketInfos()
        {
            foreach (var providerDir in Directory.EnumerateDirectories(this.packetsDirectory))
            {
                var provider = new DirectoryInfo(providerDir).Name;

                // файлы пакетов
                foreach (var packetFile in Directory.EnumerateFiles(providerDir, this.packetSearchMask ?? "*."))
                {
                    var packetId = Path.GetFileNameWithoutExtension(packetFile);
                    var filePath = packetFile;
                    var size = Convert.ToInt32(this.RunInReadLock(() => {
                        try
                        {
                            return new FileInfo(filePath).Length;
                        }
                        catch (FileNotFoundException)
                        {
                            return 0;
                        }
                    }));

                    yield return new TransportPacketInfo(provider, packetId, size, () => {
                        this.readerWriterLock.EnterReadLock();
                        try
                        {
                            return new DisposeActionStreamWrapper(File.OpenRead(filePath), this.readerWriterLock.ExitReadLock);
                            //return File.OpenRead(filePath);
                        }
                        catch (FileNotFoundException e)
                        {
                            this.readerWriterLock.ExitReadLock();
                            return null;
                        }
                        catch
                        {
                            this.readerWriterLock.ExitReadLock();
                            throw;
                        }
                    });
                }
            }
        }

        private static AtomicWriteTempFileOptions sendStatsOptions = new AtomicWriteTempFileOptions
        {
            TempFileSuffix = ".tmp",
            Destination = TempFileDestination.NextToMainFile,
        };

        private readonly static Encoding sendStatsEncoding = Encoding.UTF8;
        public void SaveSendStats(string providerKey, string packetId, SendStats stats)
        {
            var packetFilePath = Path.Combine(this.GetProviderStoreDir(providerKey), packetId + this.packetExt);
            var packetStatsFilePath = packetFilePath + this.packetStatsExt;
            if (stats.TransferCompleted)
            {
                this.RunInWriteLock(() =>
                {
                    FileSystemHelper.DeleteFile(packetStatsFilePath);
                    FileSystemHelper.DeleteFile(packetFilePath);
                });
            }
            else
            {
                AtomicFileWriteHelper.WriteToFile(packetStatsFilePath, JsonConvert.SerializeObject(stats), sendStatsEncoding, sendStatsOptions);
            }
        }

        private static JsonSerializer jsonSerializer = new JsonSerializer();
        public SendStats ReadSendStats(string providerKey, string packetId)
        {
            var packetFilePath = Path.Combine(this.GetProviderStoreDir(providerKey), packetId + this.packetExt);
            var packetStatsFilePath = packetFilePath + this.packetStatsExt;
            var packetStatsTempFilePath = packetStatsFilePath + sendStatsOptions.TempFileSuffix;
            Func<string, SendStats> readFromFile = (string filePath) =>
             {
                 return this.RunInReadLock(() =>
                 {
                     using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
                     using (var sw = new StreamReader(stream, sendStatsEncoding))
                     using (var jsonReader = new JsonTextReader(sw))
                     {
                         try
                         {
                             return jsonSerializer.Deserialize<SendStats>(jsonReader);
                         }
                         catch (JsonReaderException)
                         {
                             // в случаем когда файл json некорректен, начнём пересылку по-новой
                             return null;
                         }
                     }
                 });
             };

            if (File.Exists(packetStatsFilePath))
            {
                return readFromFile(packetStatsFilePath);
            }

            if (File.Exists(packetStatsTempFilePath))
            {
                return readFromFile(packetStatsFilePath);
            }

            return null;
        }

        private void RunInReadLock(Action action)
        {
            this.readerWriterLock.EnterReadLock();
            try
            {
                action();
            }
            finally
            {
                this.readerWriterLock.ExitReadLock();
            }
        }

        private T RunInReadLock<T>(Func<T> func)
        {
            this.readerWriterLock.EnterReadLock();
            try
            {
                return func();
            }
            finally
            {
                this.readerWriterLock.ExitReadLock();
            }
        }


        private void RunInWriteLock(Action action)
        {
            this.readerWriterLock.EnterWriteLock();
            try
            {
                action();
            }
            finally
            {
                this.readerWriterLock.ExitWriteLock();
            }
        }

        public void RemoveAll()
        {
            this.RunInWriteLock(() =>
            {
                foreach (var providerDir in Directory.EnumerateDirectories(this.packetsDirectory))
                {
                    if (!FileSystemHelper.SafeEmptyDirectory(providerDir))
                    {
                        throw new InvalidOperationException("can not delete packet files");
                    }
                }
            });
        }
    }
}
