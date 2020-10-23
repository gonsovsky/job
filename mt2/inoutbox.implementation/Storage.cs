using InOutBox.Contracts;

namespace InOutBox.Implementation
{
    public class Storage: IStorage
    {
        public IOutBox GetOutBox(string queue, string storageFolder, string conStr, string dbFile)
        {
            return new OutBox(queue, storageFolder, conStr, dbFile);
        }
    }
}
