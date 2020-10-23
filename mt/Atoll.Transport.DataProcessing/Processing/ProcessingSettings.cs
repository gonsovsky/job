using System;

namespace Atoll.Transport.DataProcessing
{
    public class ProcessingSettings
    {
        public DataNodeDefinition[] DataNodes { get; set; }
        public DataNodeDefinition[] PriorityNodes { get; set; }
        public string PcdConnectionString { get; set; }

        /// <summary>
        /// время на которое по средством Lease-инга резервируется отдельная запись для обработки
        /// </summary>
        public TimeSpan? JobLeaseLifeTime { get; set; }

        //public int ProcessingBatchSize { get; set; }

        public TimeSpan? DeleteProcessedRecordsTimeout { get; set; }
        public ReservationRangeLimits ReservationRangeLimits { get; set; }
        public int LoadedInMemoryBinaryPartsBatchSize { get; set; }
        public TimeSpan PacketsLifeTime { get; set; }
        //public int? MaxLoadedInMemoryBinaryPartsMemorySize { get; set; }

        public ProcessingSettings()
        {
            this.DataNodes = Array.Empty<DataNodeDefinition>();
            this.PriorityNodes = Array.Empty<DataNodeDefinition>();
            this.ReservationRangeLimits = new ReservationRangeLimits
            {
                Min = 200,
                Max = 220
            };
            this.LoadedInMemoryBinaryPartsBatchSize = 20;
            this.PacketsLifeTime = TimeSpan.FromDays(7);
            //this.ProcessingBatchSize = 20;
        }
    }
}
