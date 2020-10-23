using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Atoll.Transport.Client.Contract;
using Coral.Atoll.Utils;
using System.Collections.Concurrent;
using System.Linq;
using Atoll.UtilsBundle.Helpers;

namespace Atoll.Transport.Client.Bundle
{
    /// <summary>
    /// <see cref="IPacketManager"/>
    /// </summary>
    public class PacketManager : IPacketManager, IDisposable
    {
        private string packetsDirectory;
        private string packetsTempDirectory;
        private string packetExt;
        private string packetSearchMask;
        private string packetStatsExt;
        private ConcurrentDictionary<string, bool> createdProviders = new ConcurrentDictionary<string, bool>();
        private readonly static Func<string, bool, bool> updateProviderDictFactory = (x, y) => false;

        private readonly static AtomicWriteTempFileOptions sendStatsOptions = new AtomicWriteTempFileOptions
        {
            TempFileSuffix = ".tmp",
            Destination = TempFileDestination.NextToMainFile,
        };

        private readonly static JsonSerializer jsonSerializer = new JsonSerializer();
        private readonly static Encoding sendStatsEncoding = Encoding.UTF8;
        private static char packetFileIdOrderDelimeter = '.';

        /// <summary>
        /// использую для запрета одновременной записи/чтения пакетов (например чтени при отправке и удалении файлов при коммите который перезаписывает данные)
        /// изначально пакеты записываются во временную директорию (по окончанию они перемещаются) и там не требуются какие-то блокировки
        /// </summary>
        private Dictionary<string, ReaderWriterLockSlim> providerToReadWriteLocks = new Dictionary<string, ReaderWriterLockSlim>();
        private readonly object providerToReadWriteLocksSync = new object();
        /// <summary>
        /// для блокировок по всем провайдерам
        /// </summary>
        private readonly ReaderWriterLockSlim masterLock = new ReaderWriterLockSlim();

        private int orderCounter;

        private object disposeLock = new object();
        private bool isDisposed;

        public PacketManager(PacketManagerSettings settings)
        {
            this.packetsDirectory = settings.PacketsDirectory;
            this.packetsTempDirectory = settings.PacketsTempDirectory;
            Directory.CreateDirectory(this.packetsDirectory);
            Directory.CreateDirectory(this.packetsTempDirectory);

            this.packetExt = ".pkt";
            this.packetSearchMask = "*" + this.packetExt;

            this.packetStatsExt = ".stats";

            this.ReInitOrderCounter();
        }

        public void ReInitOrderCounter()
        {
            this.RunInMasterLock(() =>
            {
                var maxOrderValue = Directory.EnumerateFiles(this.packetsDirectory, "*", SearchOption.AllDirectories).Select(x => this.GetPacketIdFromFilePath(x)).Max(x => (int?)x.OrderValue) ?? 0;
                this.orderCounter = maxOrderValue;
            });         
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            lock (this.disposeLock)
            {
                if (this.isDisposed)
                {
                    return;
                }

                this.masterLock.SafeDispose();
                lock (this.providerToReadWriteLocksSync)
                {
                    foreach (var readWriteLock in this.providerToReadWriteLocks.Values)
                    {
                        readWriteLock.SafeDispose();
                    }
                    this.providerToReadWriteLocks = null;
                }

                this.isDisposed = true;
            }
        }

        private ReaderWriterLockSlim GetProviderLock(string providerKey)
        {
            lock (this.providerToReadWriteLocksSync)
            {
                if (!this.providerToReadWriteLocks.TryGetValue(providerKey, out var readerWriterLock))
                {
                    readerWriterLock = new ReaderWriterLockSlim();
                    this.providerToReadWriteLocks.Add(providerKey, readerWriterLock);
                }
                return readerWriterLock;
            }
        }

        public void Commit(string providerKey, IEnumerable<ITransportPacket> packets, CommitOptions options)
        {
            this.RunInWriteLock(providerKey, () =>
            {
                if (options == CommitOptions.DeletePrevious)
                {
                    var providerStoreDir = this.GetProviderStoreDir(providerKey);
                    // удаляем все кроме тех которые в отправке (проще доотправить ещё файл, чем решать проблему стоит ли его удалять, когда следует отправить новые данные и как это описать в коде)
                    if (Directory.Exists(providerStoreDir))
                    {
                        bool canNotDelete = false;
                        foreach (var file in Directory.EnumerateFiles(providerStoreDir, this.packetSearchMask))
                        {
                            var packetId = this.GetPacketIdFromFilePath(file);
                            var statsExists = this.ExistsSendStats(providerKey, packetId);
                            if (!statsExists && !FileSystemHelper.SafeDeleteFile(file))
                            {
                                canNotDelete = true;                            
                            }                         
                        }

                        if (canNotDelete)
                        {
                            throw new InvalidOperationException("Can't empty directory");
                        }
                    }

                    //if (!FileSystemHelper.SafeEmptyDirectory(providerStoreDir))
                    //{
                    //    throw new InvalidOperationException("Can't empty directory");
                    //}
                }

                foreach (var packet in packets)
                {
                    packet.Save(providerKey, this);
                }
            });
        }

        private string GetProviderTempDir(string providerKey)
        {
            return Path.Combine(this.packetsTempDirectory, providerKey);
        }

        private string GetProviderStoreDir(string providerKey)
        {
            return Path.Combine(this.packetsDirectory, providerKey);
        }

        protected virtual PacketIdentity GetPacketId(string providerKey)
        {
            //var counter = Interlocked.Increment(ref this.counter);
            // https://stackoverflow.com/questions/1752004/sequential-guid-generator
            // https://stackoverflow.com/questions/29674395/how-to-sort-sequential-guids-in-c
            // https://stackoverflow.com/questions/12252551/generate-a-sequential-guid-by-myself
            return new PacketIdentity(Guid.NewGuid().ToString("D"), Interlocked.Increment(ref this.orderCounter));
        }

        public PacketCreationResult Create(string providerKey)
        {
            Stream stream = null;
            try
            {
                var packetId = this.GetPacketId(providerKey);
                var providerDir = this.GetProviderStoreDir(providerKey);
                var providerTempDir = this.GetProviderTempDir(providerKey);

                if (!createdProviders.ContainsKey(providerKey))
                {
                    if (!FileSystemHelper.SafeCreateDirectory(providerTempDir))
                    {
                        throw new InvalidOperationException("Can't create provider temp dir");
                    }
                    if (!FileSystemHelper.SafeCreateDirectory(providerDir))
                    {
                        throw new InvalidOperationException("Can't create provider dir");
                    }
                    createdProviders.AddOrUpdate(providerKey, false, updateProviderDictFactory);
                }

                var tempFilePath = Path.Combine(providerTempDir, string.Concat(packetId.PacketId.ToString(), packetFileIdOrderDelimeter, packetId.OrderValue, this.packetExt));
                var filePath = this.GetPacketFilePath(providerKey, packetId);

                try
                {
                    stream = File.Open(tempFilePath, FileMode.Create, FileAccess.ReadWrite);
                }
                catch (DirectoryNotFoundException)
                {
                    // на случай когда папка удалена в процессе работы
                    stream?.Dispose();
                    if (!FileSystemHelper.SafeCreateDirectory(providerTempDir))
                    {
                        throw new InvalidOperationException("Can't create provider temp dir");
                    }
                    stream = File.Open(tempFilePath, FileMode.Create, FileAccess.ReadWrite);
                }

                return new PacketCreationResult
                {
                    Packet = new TransportPacket(packetId, stream, () =>
                    {
                        stream.Dispose();
                        if (!FileSystemHelper.SafeMoveFile(tempFilePath, filePath))
                        {
                            if (!FileSystemHelper.SafeCreateDirectory(providerDir) || !FileSystemHelper.SafeMoveFile(tempFilePath, filePath))
                            {
                                throw new InvalidOperationException("Can't move file");
                            }
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

        public IEnumerable<ITransportProviderInfo> GetTransportProviderInfos()
        {
            foreach (var providerDir in Directory.EnumerateDirectories(this.packetsDirectory))
            {
                var provider = new DirectoryInfo(providerDir).Name;

                yield return new DelegateTransportProviderInfo(provider, () => this.GetTransportPacketsForProvider(provider, providerDir));
            }
        }
    
        private PacketIdentity GetPacketIdFromFilePath(string filePath)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            var splits = nameWithoutExt.Split(packetFileIdOrderDelimeter);
            if (splits.Length == 2 && int.TryParse(splits[1], out var order))
            {
                return new PacketIdentity(splits[0], order);
            }
            return new PacketIdentity(nameWithoutExt, 0);
        }

        private IEnumerable<ITransportPacketInfo> GetTransportPacketsForProvider(string providerKey, string providerDir, int? limit = null)
        {
            // файлы пакетов
            IEnumerable<string> files = Directory.EnumerateFiles(providerDir, this.packetSearchMask);
            if (limit != null)
            {
                files = files.Take(limit.Value);
            }

            foreach (var packetFile in files)
            {
                var packetId = this.GetPacketIdFromFilePath(packetFile);
                var filePath = packetFile;
                var size = Convert.ToInt32(this.RunInReadLock(providerKey, () => {
                    try
                    {
                        return new FileInfo(filePath).Length;
                    }
                    catch (FileNotFoundException)
                    {
                        return 0;
                    }
                }));

                yield return new TransportPacketInfo(providerKey, packetId, size, () => {
                    var providerLock = this.GetProviderLock(providerKey);
                    this.masterLock.EnterReadLock();
                    try
                    {
                        var exitLocks = new Action(() =>
                        {
                            try
                            {
                                this.masterLock.ExitReadLock();
                            }
                            finally
                            {
                                providerLock.ExitReadLock();
                            }
                        });

                        providerLock.EnterReadLock();

                        try
                        {
                            return new DisposeActionStreamWrapper(File.OpenRead(filePath), exitLocks);
                            //return File.OpenRead(filePath);
                        }
                        catch (FileNotFoundException e)
                        {
                            exitLocks();
                            return null;
                        }
                        catch
                        {
                            exitLocks();
                            throw;
                        }
                    }
                    catch (Exception)
                    {
                        this.masterLock.ExitReadLock();
                        throw;
                    }
                    
                });
            }
        }

        public void SaveSendStats(string providerKey, PacketIdentity packetId, SendStats stats)
        {
            var packetFilePath = this.GetPacketFilePath(providerKey, packetId);
            var packetStatsFilePath = packetFilePath + this.packetStatsExt;
            this.RunInWriteLock(providerKey, () =>
            {
                if (stats.TransferCompleted)
                {
                    FileSystemHelper.DeleteFile(packetStatsFilePath);
                    FileSystemHelper.DeleteFile(packetFilePath);
                }
                else
                {
                    AtomicFileWriteHelper.WriteToFile(packetStatsFilePath, JsonConvert.SerializeObject(stats), sendStatsEncoding, sendStatsOptions);
                }
            });        
        }

        public SendStats ReadSendStats(string providerKey, PacketIdentity packetId)
        {
            return this.ReadSendStatsInternal(providerKey, packetId, true);
        }

        private SendStats ReadSendStatsFromFile(string filePath)
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
        }

        private SendStats ReadSendStatsFromFileWithOrWithoutLocks(string providerKey, string filePath, bool useLock)
        {
            if (useLock)
            {
                return this.RunInReadLock(providerKey, () => this.ReadSendStatsFromFile(filePath));
            }
            return this.ReadSendStatsFromFile(filePath);
        }

        private string GetPacketFilePath(string providerKey, PacketIdentity packetId)
        {
            return Path.Combine(this.GetProviderStoreDir(providerKey), string.Concat(packetId.PacketId.ToString(), packetFileIdOrderDelimeter, packetId.OrderValue, this.packetExt));
        }

        private bool ExistsSendStats(string providerKey, PacketIdentity packetId)
        {
            var packetFilePath = this.GetPacketFilePath(providerKey, packetId);
            var packetStatsFilePath = packetFilePath + this.packetStatsExt;
            var packetStatsTempFilePath = packetStatsFilePath + sendStatsOptions.TempFileSuffix;
            return File.Exists(packetStatsFilePath) || File.Exists(packetStatsTempFilePath);
        }

        private SendStats ReadSendStatsInternal(string providerKey, PacketIdentity packetId, bool useLock)
        {
            var packetFilePath = this.GetPacketFilePath(providerKey, packetId);
            var packetStatsFilePath = packetFilePath + this.packetStatsExt;
            var packetStatsTempFilePath = packetStatsFilePath + sendStatsOptions.TempFileSuffix;

            if (File.Exists(packetStatsFilePath))
            {
                return this.ReadSendStatsFromFileWithOrWithoutLocks(providerKey, packetStatsFilePath, useLock);
            }

            if (File.Exists(packetStatsTempFilePath))
            {
                return this.ReadSendStatsFromFileWithOrWithoutLocks(providerKey, packetStatsTempFilePath, useLock);
            }

            return null;
        }

        private void RunInReadLock(string providerKey, Action action)
        {
            this.masterLock.EnterReadLock();
            try
            {
                var providerLock = this.GetProviderLock(providerKey);
                providerLock.EnterReadLock();
                try
                {
                    action();
                }
                finally
                {
                    providerLock.ExitReadLock();
                }
            }
            finally
            {
                this.masterLock.ExitReadLock();
            }
        }

        private T RunInReadLock<T>(string providerKey, Func<T> func)
        {
            this.masterLock.EnterReadLock();
            try
            {
                var providerLock = this.GetProviderLock(providerKey);
                providerLock.EnterReadLock();
                try
                {
                    return func();
                }
                finally
                {
                    providerLock.ExitReadLock();
                }
            }
            finally
            {
                this.masterLock.ExitReadLock();
            }
        }

        private void RunInWriteLock(string providerKey, Action action)
        {
            this.masterLock.EnterReadLock();
            try
            {
                var providerLock = this.GetProviderLock(providerKey);
                providerLock.EnterWriteLock();
                try
                {
                    action();
                }
                finally
                {
                    providerLock.ExitWriteLock();
                }
            }
            finally
            {
                this.masterLock.ExitReadLock();
            }
        }

        private void RunInMasterLock(Action action)
        {
            this.masterLock.EnterWriteLock();
            try
            {
                action();
            }
            finally
            {
                this.masterLock.ExitWriteLock();
            }
        }

        public void RemoveAll()
        {
            this.RunInMasterLock(() =>
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
