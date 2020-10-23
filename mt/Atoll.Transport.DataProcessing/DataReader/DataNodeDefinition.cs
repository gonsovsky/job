using Atoll.Transport;
using System;
using System.Collections.Generic;

namespace Atoll.Transport.DataProcessing
{
    /// <summary>
    /// Описатель узла приёма событий DHU.
    /// </summary>
    public sealed class DataNodeDefinition : IEquatable<DataNodeDefinition>
    {

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="serviceUri">Адрес веб-сервиса выборки событий.</param>
        public DataNodeDefinition(Uri serviceUri)
        {
            this.ServiceUri = serviceUri;
        }

        /// <summary>
        /// Адрес веб-сервиса выборки событий.
        /// </summary>
        public Uri ServiceUri { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as DataNodeDefinition);
        }

        public bool Equals(DataNodeDefinition other)
        {
            return other != null &&
                   EqualityComparer<Uri>.Default.Equals(ServiceUri, other.ServiceUri);
        }

        public override int GetHashCode()
        {
            return 1818454126 + EqualityComparer<Uri>.Default.GetHashCode(ServiceUri);
        }

        //public string MongoDbName => TransportConstants.MongoDatabaseName;

    }
}
