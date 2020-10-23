using Atoll.Transport.ServerBundle;
using Atoll.UtilsBundle.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Atoll.Transport.DataProcessing
{

    /// <summary>
    /// Пул читателей DHU.
    /// </summary>
    sealed class DataNodeReadersPool: IDisposable
    {

        public DataNodeReadersPool(DataNodeDefinition[] dhuNodeDefinitions, IEnumerable<DataNodeDefinition> prioritedNodes, IDbServicesFactory timeServiceFactory)
        {
            if (dhuNodeDefinitions != null)
            {
                this.nodeReaders = dhuNodeDefinitions
                    .Select(ndf => new DataNodeReader(ndf, timeServiceFactory))
                    .ToArray();
            }
            else
            {
                this.nodeReaders = new DataNodeReader[0];
            }

            this.prioritedNodes = prioritedNodes;
        }

        private readonly DataNodeReader[] nodeReaders;
        private readonly IEnumerable<DataNodeDefinition> prioritedNodes;
        private readonly object syncLock = new object();
        private CancellationTokenSource initCts = null;
        private Task initTask = null;
        private bool isDisposed = false;

        public string[] GetStorageIds()
        {
            return this.nodeReaders
                .Select(x => x.StorageIdentifier)
                .Where(x => x != null)
                .ToArray();
        }

        //public bool TryGetReaderForUri(Uri url, out DataNodeReader nodeReader)
        //{
        //    nodeReader = this.nodeReaders.FirstOrDefault(x => x.ServiceUri == url && x.StorageIdentifier != null);
        //    return nodeReader != null;
        //}
        public bool TryGetReader(DataNodeDefinition defenition, out DataNodeReader nodeReader)
        {
            nodeReader = this.nodeReaders.FirstOrDefault(x => x.ServiceUri == defenition.ServiceUri && x.StorageIdentifier != null);
            return nodeReader != null;
        }

        public bool TryGetReaderForStorageId(string storageIdentifier, out DataNodeReader nodeReader)
        {
            nodeReader = this.nodeReaders.FirstOrDefault(x => x.StorageIdentifier == storageIdentifier);
            return nodeReader != null;
        }


        //private cancelation
        /// <summary>
        /// Инициализация Dhu-ридеров.
        /// </summary>
        /// <remarks>
        /// Блокируется до тех пор, пока хотя бы один из ридеров не инициализируется.
        /// </remarks>
        public void InitReaders(CancellationToken token)
        {
            if (this.nodeReaders.Length == 0)
                return;

            if (this.isDisposed)
            {
                throw new ObjectDisposedException("readersPool");
            }

            bool startedInit = false;
            lock (this.syncLock)
            {
                startedInit = this.initCts != null;
            }

            if (startedInit)
            {
                this.initTask.Wait(this.initCts.Token);
                return;
            }
            
            lock (this.syncLock)
            {
                if (this.isDisposed)
                {
                    throw new ObjectDisposedException("readersPool");
                }

                if (this.initCts == null)
                {
                    this.initCts = CancellationTokenSource.CreateLinkedTokenSource(token);

                    var cancellationToken = this.initCts.Token;

                    var initPriorited = this.nodeReaders
                         .Where(x => this.prioritedNodes.Contains(x.nodeDefinition))
                         .Select(x => x.InitializeAsync(cancellationToken))
                         .ToArray();

                    var initNonPriorited = this.nodeReaders
                        .Where(x => !this.prioritedNodes.Contains(x.nodeDefinition))
                        .Select(x => x.InitializeAsync(cancellationToken))
                        .ToArray();

                    //var initTasks = this.nodeReaders
                    //    .Select(x => x.InitializeAsync(cancellationToken))
                    //    .ToArray();
                    //Task.WaitAny(initTasks, cancellationToken);

                    Task initializePriorited = null;
                    if (initPriorited.Any())
                    {
                        initializePriorited = Task.WhenAny(initPriorited.Union(new[] { Task.Delay(TimeSpan.FromSeconds(5)) }));
                    }
                    else
                    {
                        initializePriorited = null;
                    }

                    Task initializeNonPriorited = null;
                    if (initNonPriorited.Any())
                    {
                        initializeNonPriorited = Task.WhenAny(initNonPriorited);
                    }
                    else
                    {
                        initializeNonPriorited = Task.CompletedTask;
                    }

                    this.initTask = initializePriorited != null
                        ? initializePriorited.ContinueWith(t =>
                        {
                            return initializeNonPriorited;
                        }, cancellationToken)
                    : initializeNonPriorited;
                }
            }

            this.initTask.Wait(this.initCts.Token);
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            lock (this.syncLock)
            {
                if (this.isDisposed)
                {
                    return;
                }

                this.initCts.SafeCancel();
                this.initTask?.Wait(TimeSpan.FromSeconds(5));
                this.initCts.SafeDispose();

                var nodeReaders = this.nodeReaders;
                foreach (var nodeReader in nodeReaders)
                {
                    nodeReader.Dispose();
                }

                this.isDisposed = true;
            }
        }

        // Change streams are available for replica sets and sharded clusters
        // https://docs.mongodb.com/manual/changeStreams/

        //public Task WhenAnyChange(CancellationToken cancellationToken)
        //{
        //    var nodeReaders = this.nodeReaders;
        //    return Task.WhenAny(nodeReaders.Select(x => x.WaitForChange(cancellationToken)).ToArray());
        //}
    }
}
