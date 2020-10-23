namespace InOutBox.Contracts
{
    public interface IStorage
    {
        IOutBox GetOutBox(string queue, string storageFolder, string conStr, string dbFile);
    }
}
