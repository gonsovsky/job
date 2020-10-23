using System;

namespace Atoll.Transport.DataHub
{
    public class MongoClusterPacketsStoreSettings
    {
        public TimeSpan? SearchDbTimeout { get; private set; }
        public TimeSpan? LostDbTimeout { get; private set; }

        public MongoClusterPacketsStoreSettings(TimeSpan? searchDbTimeout, TimeSpan? lostDbTimeout)
        {
            this.SearchDbTimeout = searchDbTimeout ?? TimeSpan.FromMilliseconds(300);
            this.LostDbTimeout = lostDbTimeout ?? TimeSpan.FromSeconds(7);
        }
    }

}
