using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace Atoll.Transport.ServerBundle
{
    public class MongoIndexCreator
    {
        public const string CompoundIndexName = "ix_range_fields_compound";

        public async Task InitIndexes(IMongoDatabase database, CancellationToken cancellationToken)
        {
            // Индексы для частей пакетов
            var chunks = database.GetCollection<PacketPart>(TransportConstants.PacketsPartsTable);

            // чтобы не было дублей
            var packetIdStartPostionKeys = Builders<PacketPart>.IndexKeys.Ascending(x => x.PacketId).Ascending(x => x.StartPosition);
            var ixPacketIdStartPostionName = "uix_packetid_startpos";
            var ixPacketIdStartPostionModel = new CreateIndexModel<PacketPart>(packetIdStartPostionKeys, new CreateIndexOptions
            {
                Name = ixPacketIdStartPostionName,
                Unique = true,
            });

            // индекс для резервирования записей в работу
            var transferedTimeKeys = Builders<PacketPart>.IndexKeys
                .Ascending(x => x.FinalPartTransferTime)
                //.Ascending(x => x.IsFinal)
                .Ascending(x => x.StartTime)
                .Ascending(x => x.ProcessingUnitId)
                .Ascending(x => x.EndTime);
            var ixTransferedTimeName = CompoundIndexName;
            var ixTransferedTimeModel = new CreateIndexModel<PacketPart>(transferedTimeKeys, new CreateIndexOptions
            {
                Name = ixTransferedTimeName,
            });

            // для удаления
            var endTimeCreatedTimeKeys = Builders<PacketPart>.IndexKeys
                .Ascending(x => x.EndTime)
                .Ascending(x=> x.CreatedTime);
            var ixEndTimeCreatedTimeName = "ix_endtime_createdtime";
            var ixEndTimeCreatedTimeModel = new CreateIndexModel<PacketPart>(endTimeCreatedTimeKeys, new CreateIndexOptions
            {
                Name = ixEndTimeCreatedTimeName,
            });

            await chunks.Indexes.CreateManyAsync(new[] { ixPacketIdStartPostionModel, ixTransferedTimeModel, ixEndTimeCreatedTimeModel }, null, cancellationToken).ConfigureAwait(false);
        }
    }

}
