using System;
using System.Collections.Generic;

namespace Atoll.Transport.ServerBundle
{

    public class AcquiredDbParameters : IEquatable<AcquiredDbParameters>
    {
        public string ConnStringOrUrl { get; set; }
        public string DatabaseName { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as AcquiredDbParameters);
        }

        public bool Equals(AcquiredDbParameters other)
        {
            return other != null &&
                   ConnStringOrUrl == other.ConnStringOrUrl &&
                   DatabaseName == other.DatabaseName;
        }

        public override int GetHashCode()
        {
            var hashCode = 1458974763;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ConnStringOrUrl);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DatabaseName);
            return hashCode;
        }

        //public TimeSpan LeaseCheckTimeout { get; set; }
        //public TimeSpan LeaseLostTimeout { get; set; }
        //public TimeSpan UpdateServerTimeInterval { get; set; }
    }

}
